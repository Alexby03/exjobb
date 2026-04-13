using System.Text.Json;
using MySqlConnector;

namespace localdotnet.Services;

public class SqlService
{
    private readonly string _connectionString;
    private readonly ILogger<SqlService> _logger;

    public SqlService(IConfiguration config, ILogger<SqlService> logger)
    {
        _connectionString =
            config.GetConnectionString("Database")
            ?? throw new InvalidOperationException(
                "Connection string 'Database' (ConnectionStrings:Database) is required.");
        _logger = logger;
    }

    private async ValueTask<MySqlConnection> ConnectAsync()
    {
        var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }

    public async Task<string> ExecuteSqlQueryAsync(string sql)
    {
        await using var conn = await ConnectAsync();
        await using var command = new MySqlCommand(sql, conn);

        try
        {
            await using var reader = await command.ExecuteReaderAsync();

            if (reader.HasRows)
            {
                var results = new List<Dictionary<string, object?>>();
                int rowCount = 0;

                while (await reader.ReadAsync() && rowCount < 10)
                {
                    var row = new Dictionary<string, object?>();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    results.Add(row);
                    rowCount++;
                }

                return $"Results ({results.Count} rows): {System.Text.Json.JsonSerializer.Serialize(results)}";
            }

            int affected = reader.RecordsAffected;
            return $"Affected {affected} rows (non-SELECT)";
        }
        catch (Exception ex)
        {
            return $"DB ERROR: {ex.Message}";
        }
    }

    public async Task<string> InsertGeneratedScenarioAsync(
        string userId, 
        string category, 
        AgentToolResponse data, 
        string counterparty) 
    {
        string sql = @"
            INSERT INTO generatedscenarios 
            (UserId, PromptText, Category, ExpectedIsAllowed, Counterparty, ExpectedTools, RequiredPermissions, Rationale, SetupData)
            VALUES 
            (@UserId, @PromptText, @Category, @ExpectedIsAllowed, @Counterparty, @ExpectedTools, @RequiredPermissions, @Rationale, @SetupData)";

        await using var conn = await ConnectAsync();
        
        await using var transaction = await conn.BeginTransactionAsync();
        
        await using var command = new MySqlCommand(sql, conn, transaction);

        string expectedToolsJson = JsonSerializer.Serialize(data.ExpectedTools);
        string requiredPermissionsJson = JsonSerializer.Serialize(data.RequiredPermissions);
        string setupDataJson = JsonSerializer.Serialize(data.SetupData);

        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@PromptText", data.PromptText);
        command.Parameters.AddWithValue("@Category", category);
        command.Parameters.AddWithValue("@ExpectedIsAllowed", data.ExpectedIsAllowed); 
        command.Parameters.AddWithValue("@Counterparty", counterparty);
        command.Parameters.AddWithValue("@ExpectedTools", expectedToolsJson);
        command.Parameters.AddWithValue("@RequiredPermissions", requiredPermissionsJson);
        command.Parameters.AddWithValue("@Rationale", data.Rationale);
        command.Parameters.AddWithValue("@SetupData", setupDataJson);

        try
        {
            int affected = await command.ExecuteNonQueryAsync();
            
            if (affected > 0)
            {
                await transaction.CommitAsync();
                return $"Success: Affected {affected} rows. Scenario saved.";
            }
            else
            {
                await transaction.RollbackAsync();
                return "Warning: No rows were inserted.";
            }
        }
        catch (MySqlException sqlEx)
        {
            try { await transaction.RollbackAsync(); } catch { }

            if (sqlEx.Number == 1062) // 1062 = Duplicate entry for key
            {
                _logger.LogWarning("     [DB WARNING] Duplicate entry discovered for UserId: {UserId}, Category: {Category}. Row skipped.", userId, category);
                return $"DB WARNING: Duplicate entry. {sqlEx.Message}";
            }
            else if (sqlEx.Number == 1213) // 1213 = Deadlock found when trying to get lock
            {
                _logger.LogError("     [DB ERROR] Deadlock in database for UserId: {UserId}. Another query is blocking.", userId);
                return $"DB ERROR: Deadlock. {sqlEx.Message}";
            }
            else
            {
                _logger.LogError(sqlEx, "     [DB ERROR] MySQL error {ErrorNumber} during insertion. UserId: {UserId}. Data: {@Data}", sqlEx.Number, userId, data);
                return $"DB ERROR: {sqlEx.Message}";
            }
        }
        catch (Exception ex)
        {
            try { await transaction.RollbackAsync(); } catch { }
            
            _logger.LogError(ex, "     [CRITICAL] Unexpected error in database method for UserId: {UserId}, Category: {Category}", userId, category);
            return $"DB FATAL: {ex.Message}";
        }
    }

    public async Task<string> GetPermissionsByFullNameAsync(string fullName)
    {
        await using var conn = await ConnectAsync();
        
        bool isBool = Environment.GetEnvironmentVariable("PERMISSIONS_AS_BOOL") == "true";

        string columnName = isBool ? "PermissionName" : "PermissionText";

        string sql = $@"
            SELECT DISTINCT p.{columnName}
            FROM users u
            INNER JOIN userroles ur ON u.UserId = ur.UserId
            INNER JOIN rolepermissions rp ON ur.RoleId = rp.RoleId
            INNER JOIN permissions p ON rp.PermissionId = p.PermissionId
            WHERE u.FullName = @FullName;";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@FullName", fullName);

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            
            var permissions = new List<string>();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                {
                    permissions.Add(reader.GetString(0));
                }
            }

            if (permissions.Count == 0)
                return string.Empty;

            return string.Join(", ", permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB ERROR in GetPermissionsByFullNameAsync för {User}", fullName);
            return string.Empty;
        }
    }

    public async Task<string> GetPermissionsByUserIdAsync(string userId)
    {
        await using var conn = await ConnectAsync();
        
        bool isBool = Environment.GetEnvironmentVariable("PERMISSIONS_AS_BOOL") == "true";
        string columnName = isBool ? "PermissionName" : "PermissionText";

        string sql = $@"
            SELECT DISTINCT p.{columnName}
            FROM users u
            INNER JOIN userroles ur ON u.UserId = ur.UserId
            INNER JOIN rolepermissions rp ON ur.RoleId = rp.RoleId
            INNER JOIN permissions p ON rp.PermissionId = p.PermissionId
            WHERE u.UserId = @UserId;";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            
            var permissions = new List<string>();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                {
                    permissions.Add(reader.GetString(0));
                }
            }

            if (permissions.Count == 0)
            {
                _logger.LogWarning("No permissions found for UserId: {UserId}", userId);
                return string.Empty;
            }

            return string.Join(", ", permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB ERROR in GetPermissionsByUserIdAsync for UserId: {UserId}", userId);
            return string.Empty;
        }
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var users = new List<User>();
        
        await using var conn = await ConnectAsync();
        
        string sql = "SELECT UserId, Username, Email, FullName, Department, Role FROM users";
        await using var command = new MySqlCommand(sql, conn);

        try
        {
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetString(reader.GetOrdinal("UserId")),
                    Username = reader.GetString(reader.GetOrdinal("Username")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    FullName = reader.GetString(reader.GetOrdinal("FullName")),
                    Department = reader.IsDBNull(reader.GetOrdinal("Department")) 
                        ? null 
                        : reader.GetString(reader.GetOrdinal("Department")),
                        
                    Role = reader.IsDBNull(reader.GetOrdinal("Role")) 
                        ? null 
                        : reader.GetString(reader.GetOrdinal("Role"))
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DB ERROR in GetAllUsersAsync: {ex.Message}");
            throw; 
        }

        return users;
    }
}

// DTO
public class User
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Role { get; set; }
}