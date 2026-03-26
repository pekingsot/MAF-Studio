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

            using var connection = _context.CreateOpenConnection();

            foreach (var sqlFile in sqlFiles)
            {
                var fileName = Path.GetFileName(sqlFile);
                _logger.LogInformation("执行SQL脚本: {FileName}", fileName);
                
                var sql = await File.ReadAllTextAsync(sqlFile);
                await connection.ExecuteAsync(sql);
                
                _logger.LogInformation("SQL脚本执行完成: {FileName}", fileName);
            }
            
            _logger.LogInformation("数据库初始化完成，共执行 {Count} 个脚本", sqlFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库初始化失败");
        }
    }
}
