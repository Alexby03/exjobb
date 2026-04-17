using System.Collections.Concurrent;
using OpenAI;
using App.Data;
using OpenAI.Chat;
using localdotnet.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

builder.Services.AddScoped<SqlService>();
builder.Services.AddTransient<Generator>();
builder.Services.AddTransient<Tester>();

builder.Services.AddSingleton<ChatClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config["AZURE_OPENAI_ENDPOINT"] ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
    var deploymentName = config["DEPLOYMENT_NAME"] ?? Environment.GetEnvironmentVariable("DEPLOYMENT_NAME");
    var apiKey = config["AZURE_OPENAI_KEY"] ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");

    if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(deploymentName))
        throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT and DEPLOYMENT_NAME in config or environment variables.");

    if (string.IsNullOrWhiteSpace(apiKey))
        throw new InvalidOperationException("Mistral serverless endpoint requires an API Key.");

    var clientOptions = new OpenAIClientOptions 
    { 
        Endpoint = new Uri(endpoint) 
    };
    
    var openAIClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey), clientOptions);

    return openAIClient.GetChatClient(deploymentName);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // dev features
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

var threadMemory = new ConcurrentDictionary<string, List<ChatMessage>>();

app.MapGet("/health", async (AppDbContext dbContext) =>
{
    try
    {
        await dbContext.Database.CanConnectAsync();
        return Results.Ok(new { status = "healthy", message = "Application and database are running" });
    }
    catch
    {
        return Results.StatusCode(503);
    }
}).WithName("HealthCheck");

app.MapPost("/tools/test-scenarios", (
    IServiceProvider serviceProvider,
    IConfiguration config,
    ILogger<Program> logger) =>
{
    logger.LogInformation("Received POST /tools/test-scenarios. Starting Task.Run...");

    _ = Task.Run(async () =>
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var tester = scope.ServiceProvider.GetRequiredService<Tester>();
            await tester.RunTestingLoop();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "A critical error occurred within the testing background thread.");
        }
    });

    return Results.Accepted(value: new
    {
        Message = "Testing of scenarios has started in the background, follow the progress in the server's console."
    });
});


app.MapPost("/chat", async (
    ChatRequest request,
    ChatClient chatClient, 
    IConfiguration config,
    ILogger<Program> logger) =>
{
    logger.LogInformation("POST /chat: incoming message: {Message}, threadId={ThreadId}", request.Message, request.ThreadId);

    string currentThreadId = string.IsNullOrWhiteSpace(request.ThreadId) ? Guid.NewGuid().ToString() : request.ThreadId;
    
    var history = threadMemory.GetOrAdd(currentThreadId, _ => new List<ChatMessage> 
    {
        new SystemChatMessage("You are a helpful AI assistant.") // filler completion
    });
    history.Add(new UserChatMessage(request.Message));

    try
    {
        logger.LogInformation("Sending request directly to LLM for thread {ThreadId}...", currentThreadId);
        
        ChatCompletion completion = await chatClient.CompleteChatAsync(history);
        
        string responseText = completion.Content[0].Text;

        history.Add(new AssistantChatMessage(responseText));

        logger.LogInformation("POST /chat completed for thread {ThreadId}. Response length={Length}", currentThreadId, responseText.Length);
        return Results.Ok(new ChatResponse { Response = responseText, ThreadId = currentThreadId });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Chat completion failed.");
        return Results.Problem($"LLM Error: {ex.Message}");
    }
});

app.MapPost("/tools/generate-scenarios", (
    IServiceProvider serviceProvider,
    IConfiguration config,
    ILogger<Program> logger) =>
{
    logger.LogInformation("Received POST /tools/generate-scenarios. Starting Task.Run...");

    _ = Task.Run(async () => 
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var generator = scope.ServiceProvider.GetRequiredService<Generator>();
            await generator.RunGenerationLoop(); // generator
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "A critical error occurred within the background thread.");
        }
    });

    return Results.Accepted(value: new { 
        Message = "Generation of scenarios has started in the background, follow the process in the server's console." 
    });
});

app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync("wwwroot/index.html");
});

app.Run();

// DTOs
public sealed record AgentActionLogDto(
    long UserPromptId,
    int ToolIndex,
    string ToolName,
    string Client,
    bool IsNL,
    string UserPrompt,
    bool IsBad,
    string ReasonLog
);
public sealed record ChatRequest(string Message, string? ThreadId = null);

public sealed record ChatResponse
{
    public string Response { get; init; } = string.Empty;
    public string ThreadId { get; init; } = string.Empty;
}

public sealed record SqlQueryRequest(
    string Sql,
    AgentActionLogDto Log
);
public sealed record SqlQueryResponse(string Result);
public sealed record TableDdlRequest(string TableName, string Survey);
public sealed record TableDdlResponse(string Ddl);

public sealed record GetPermissionsRequest(
    string FullName,
    AgentActionLogDto Log
);

public sealed record GetPermissionsResponse(string Result);

public record SaveScenarioRequest(
    string UserId,
    string PromptText,
    string Category,
    bool ExpectedIsAllowed,
    List<string> ExpectedTools,
    List<string> RequiredPermissions,
    string Rationale
);