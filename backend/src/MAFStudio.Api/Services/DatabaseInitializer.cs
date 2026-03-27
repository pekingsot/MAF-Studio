using Dapper;
using MAFStudio.Infrastructure.Data;
using Npgsql;

namespace MAFStudio.Api.Services;

public interface IDatabaseInitializer
{
    Task InitializeAsync();
}

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDapperContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IWebHostEnvironment _environment;

    public DatabaseInitializer(IDapperContext context, ILogger<DatabaseInitializer> logger, IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    public async Task InitializeAsync()
    {
        try
        {
            using var connection = _context.CreateOpenConnection();

            await EnsureMigrationHistoryTableAsync(connection);

            var executedScripts = await GetExecutedScriptsAsync(connection);

            var scriptsPath = Path.Combine(_environment.ContentRootPath, "..", "MAFStudio.Infrastructure", "Data", "Scripts");
            
            if (!Directory.Exists(scriptsPath))
            {
                _logger.LogWarning("SQL脚本目录未找到: {Path}", scriptsPath);
                return;
            }

            var sqlFiles = Directory.GetFiles(scriptsPath, "*.sql")
                .OrderBy(f => f)
                .ToList();

            if (sqlFiles.Count == 0)
            {
                _logger.LogWarning("未找到SQL脚本文件");
                return;
            }

            var newScriptsCount = 0;
            foreach (var sqlFile in sqlFiles)
            {
                var fileName = Path.GetFileName(sqlFile);
                
                if (executedScripts.Contains(fileName))
                {
                    _logger.LogDebug("脚本已执行过，跳过: {FileName}", fileName);
                    continue;
                }

                _logger.LogInformation("执行SQL脚本: {FileName}", fileName);
                
                var sql = await File.ReadAllTextAsync(sqlFile);
                await connection.ExecuteAsync(sql);
                
                await RecordMigrationAsync(connection, fileName);
                
                _logger.LogInformation("SQL脚本执行完成: {FileName}", fileName);
                newScriptsCount++;
            }
            
            _logger.LogInformation("数据库初始化完成，新增执行 {Count} 个脚本，跳过 {Skipped} 个已执行脚本", 
                newScriptsCount, sqlFiles.Count - newScriptsCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库初始化失败");
        }
    }

    private async Task EnsureMigrationHistoryTableAsync(NpgsqlConnection connection)
    {
        const string sql = @"
            CREATE TABLE IF NOT EXISTS __migration_history (
                id SERIAL PRIMARY KEY,
                script_name VARCHAR(255) NOT NULL UNIQUE,
                executed_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            )";
        await connection.ExecuteAsync(sql);
    }

    private async Task<HashSet<string>> GetExecutedScriptsAsync(NpgsqlConnection connection)
    {
        const string sql = "SELECT script_name FROM __migration_history";
        var scripts = await connection.QueryAsync<string>(sql);
        return new HashSet<string>(scripts, StringComparer.OrdinalIgnoreCase);
    }

    private async Task RecordMigrationAsync(NpgsqlConnection connection, string scriptName)
    {
        const string sql = "INSERT INTO __migration_history (script_name) VALUES (@ScriptName) ON CONFLICT DO NOTHING";
        await connection.ExecuteAsync(sql, new { ScriptName = scriptName });
    }
}
