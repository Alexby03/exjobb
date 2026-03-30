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
        // In MySQL, TABLE_SCHEMA is the database name.
        // COLUMN_TYPE already includes length/precision (e.g. varchar(255), decimal(10,2)),
        // so no CASE expression needed unlike T-SQL.
        const string ddlQuery = @"
            SELECT CONCAT(
                'CREATE TABLE `', @TableName, '` (\n',
                GROUP_CONCAT(
                    CONCAT(
                        '    `', COLUMN_NAME, '` ',
                        COLUMN_TYPE,
                        IF(IS_NULLABLE = 'NO', ' NOT NULL', ' NULL')
                    )
                    ORDER BY ORDINAL_POSITION
                    SEPARATOR ',\n'
                ),
                '\n);'
            ) AS CreateTableDDL
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @SchemaName
              AND TABLE_NAME   = @TableName
            GROUP BY TABLE_NAME;
        ";

        await using var conn = await ConnectAsync();
        await using var command = new MySqlCommand(ddlQuery, conn);
        command.Parameters.AddWithValue("@SchemaName", conn.Database);
        command.Parameters.AddWithValue("@TableName", tableName);

        try
        {
            var result = await command.ExecuteScalarAsync();
            return result is string ddl && !string.IsNullOrWhiteSpace(ddl)
                ? ddl
                : $"Could not generate DDL for {tableName}.";
        }
        catch (Exception ex)
        {
            return $"DB ERROR in get_table_ddl: {ex.Message}";
        }
    }
}