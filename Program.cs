using App.Data;
using Azure;
using Azure.AI.Agents.Persistent;
using Azure.AI.Projects;
using Azure.Identity;
using localdotnet.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// EF Core DbContext with MySQL (Pomelo)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

// Raw SQL service (MySqlConnector, used directly for agent tool endpoints)
builder.Services.AddSingleton<SqlService>();

// Azure AI Persistent Agents client
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config["PROJECT_ENDPOINT"] ?? Environment.GetEnvironmentVariable("PROJECT_ENDPOINT");

    if (string.IsNullOrWhiteSpace(endpoint))
        throw new InvalidOperationException("Set PROJECT_ENDPOINT in config or environment variables.");

    var projectClient = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());
    return projectClient.GetPersistentAgentsClient();
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

// Health check (moved off "/" so the chat UI can live there)
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
})
.WithName("HealthCheck");

// Chat endpoint
app.MapPost("/chat", async (ChatRequest request, PersistentAgentsClient agentsClient, IConfiguration config, ILogger<Program> logger) =>
{
    var agentId = config["AGENT_ID"] ?? Environment.GetEnvironmentVariable("AGENT_ID");
    if (string.IsNullOrEmpty(agentId))
        return Results.BadRequest("AGENT_ID not configured.");

    PersistentAgentThread thread = !string.IsNullOrWhiteSpace(request.ThreadId)
        ? agentsClient.Threads.GetThread(request.ThreadId)
        : agentsClient.Threads.CreateThread();

    agentsClient.Messages.CreateMessage(thread.Id, MessageRole.User, request.Message);

    ThreadRun run = agentsClient.Runs.CreateRun(thread.Id, agentId);

    do
    {
        await Task.Delay(500);
        run = agentsClient.Runs.GetRun(thread.Id, run.Id);
    }
    while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);

    if (run.Status != RunStatus.Completed)
    {
        logger.LogError("Agent run failed. Status={Status}, Code={Code}, Error={Message}",
            run.Status,
            run.LastError?.Code,
            run.LastError?.Message);

        return Results.Problem(
            $"Agent run failed with status: {run.Status}. Error: {run.LastError?.Message}");
    }

    if (run.Status != RunStatus.Completed)
        return Results.Problem($"Agent run failed with status: {run.Status}. Error: {run.LastError?.Message}");

    Pageable<PersistentThreadMessage> messages = agentsClient.Messages.GetMessages(
        threadId: thread.Id,
        order: ListSortOrder.Ascending);

    PersistentThreadMessage? lastAgentMessage = null;
    foreach (var msg in messages)
    {
        if (msg.Role == MessageRole.Agent)
            lastAgentMessage = msg;
    }

    string responseText = string.Empty;
    if (lastAgentMessage != null)
    {
        foreach (MessageContent contentItem in lastAgentMessage.ContentItems)
        {
            if (contentItem is MessageTextContent textItem)
                responseText += textItem.Text;
        }
    }

    return Results.Ok(new ChatResponse { Response = responseText, ThreadId = thread.Id });
});

// Tool endpoints
app.MapPost("/tools/execute-sql", async (SqlQueryRequest request, SqlService sql, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Incoming /tools/execute-sql - Query: {SqlQuery}", request.Sql);
        logger.LogInformation("Log payload: {@Log}", request.Log);

        var result = await sql.ExecuteSqlQueryAsync(request.Sql);

        if (!result.StartsWith("DB ERROR:", StringComparison.OrdinalIgnoreCase))
        {
            await sql.InsertAgentActionLogAsync(request.Log);
        }

        logger.LogInformation("Result: {SqlResult}", result);
        return Results.Ok(new SqlQueryResponse(result));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in /tools/execute-sql");
        return Results.BadRequest(new SqlQueryResponse($"Error: {ex.Message}"));
    }
});

/*app.MapPost("/tools/get-table-ddl", async (TableDdlRequest request, SqlService sql, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Incoming /tools/get-table-ddl - Table: {TableName}", request.TableName);
        logger.LogInformation("Survey: {Survey}", request.Survey);
        var ddl = await sql.GetTableDdlAsync(request.TableName);
        logger.LogInformation("DDL: {TableDdl}", ddl);
        return Results.Ok(new TableDdlResponse(ddl));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in /tools/get-table-ddl");
        return Results.BadRequest(new TableDdlResponse($"Error: {ex.Message}"));
    }
});*/

app.MapPost("/tools/get-permissions-by-name",
    async (GetPermissionsRequest request,
           SqlService sql,
           ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Log payload: {@Log}", request.Log);

        var result = await sql.GetPermissionsByFullNameAsync(request.FullName, false); // ÄNDRA FRÅN BOOL TILL TEXT HÄR!!!

        if (!result.StartsWith("DB ERROR:", StringComparison.OrdinalIgnoreCase))
        {
            await sql.InsertAgentActionLogAsync(request.Log);
        }

        logger.LogInformation("Permissions result: {Result}", result);
        return Results.Ok(new GetPermissionsResponse(result));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in /tools/get-permissions-by-name");
        return Results.BadRequest(new GetPermissionsResponse($"Error: {ex.Message}"));
    }
});

// Serve chat UI (must be last — catch-all for root)
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