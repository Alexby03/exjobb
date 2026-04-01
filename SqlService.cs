using MySqlConnector;

namespace localdotnet.Services;

public class SqlService
{
    private readonly string _connectionString;

    public SqlService(IConfiguration config)
    {
        _connectionString =
            config.GetConnectionString("Database")
            ?? throw new InvalidOperationException(
                "Connection string 'Database' (ConnectionStrings:Database) is required.");
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

    public async Task<string> GetTableDdlAsync(string tableName)
    {
        await using var conn = await ConnectAsync();

        // Use parameter for database, but table name must be injected into the SHOW statement
        // because MySQL doesn't allow identifiers as parameters.
        var ddlQuery = $"SHOW CREATE TABLE `{conn.Database}`.`{tableName}`;";

        await using var command = new MySqlCommand(ddlQuery, conn);

        try
        {
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                // Column 0 = Table name, Column 1 = full CREATE TABLE statement
                var ddl = reader.GetString(1);
                return !string.IsNullOrWhiteSpace(ddl)
                    ? ddl
                    : $"Could not generate DDL for {tableName}.";
            }

            return $"Could not generate DDL for {tableName}.";
        }
        catch (Exception ex)
        {
            return $"DB ERROR in get_table_ddl: {ex.Message}";
        }
    }

    public async Task InsertAgentActionLogAsync(AgentActionLogDto dto)
    {
        const string insertSql = @"
            INSERT INTO agentactionlog
                (UserPromptId, ToolIndex, ToolName, Client, IsNL, UserPrompt, IsBad, ReasonLog)
            VALUES
                (@UserPromptId, @ToolIndex, @ToolName, @Client, @IsNL, @UserPrompt, @IsBad, @ReasonLog);
        ";

        await using var conn = await ConnectAsync();
        await using var cmd = new MySqlCommand(insertSql, conn);

        cmd.Parameters.AddWithValue("@UserPromptId", dto.UserPromptId);
        cmd.Parameters.AddWithValue("@ToolIndex", dto.ToolIndex);
        cmd.Parameters.AddWithValue("@ToolName", dto.ToolName);
        cmd.Parameters.AddWithValue("@Client", dto.Client);
        cmd.Parameters.AddWithValue("@IsNL", dto.IsNL);
        cmd.Parameters.AddWithValue("@UserPrompt", dto.UserPrompt);
        cmd.Parameters.AddWithValue("@IsBad", dto.IsBad);
        cmd.Parameters.AddWithValue("@ReasonLog", dto.ReasonLog);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<string> GetPermissionsByFullNameAsync(string fullName, bool isBool)
    {
        await using var conn = await ConnectAsync();

        string sql = isBool
            ? @"
                SELECT
                    p.PermissionName,
                    up.IsAllowed
                FROM userpermissions up
                INNER JOIN users u ON up.UserId = u.UserId
                INNER JOIN permissions p ON up.PermissionId = p.PermissionId
                WHERE u.FullName = @FullName;"
            : @"
                SELECT
                    p.PermissionText,
                    up.IsAllowed
                FROM userpermissions up
                INNER JOIN users u ON up.UserId = u.UserId
                INNER JOIN permissions p ON up.PermissionId = p.PermissionId
                WHERE u.FullName = @FullName;";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@FullName", fullName);

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows)
                return $"No permissions found for '{fullName}'.";

            var results = new List<Dictionary<string, object?>>();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();

                if (isBool)
                {
                    var name = reader.GetString(reader.GetOrdinal("PermissionName"));
                    bool allowed = reader.GetInt32(reader.GetOrdinal("IsAllowed")) == 1;

                    row["PermissionName"] = name;
                    row["IsAllowed"] = allowed;
                }
                else
                {
                    var text = reader.GetString(reader.GetOrdinal("PermissionText"));
                    bool allowed = reader.GetInt32(reader.GetOrdinal("IsAllowed")) == 1;

                    if (!allowed && text.StartsWith("can "))
                        text = "not " + text;

                    row["Permission"] = text;
                }

                results.Add(row);
            }

            return System.Text.Json.JsonSerializer.Serialize(results);
        }
        catch (Exception ex)
        {
            return $"DB ERROR in GetPermissionsByFullNameAsync: {ex.Message}";
        }
    }
}