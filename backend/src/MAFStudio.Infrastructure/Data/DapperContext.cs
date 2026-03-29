using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MAFStudio.Infrastructure.Data;

public interface IDapperContext : IDisposable
{
    NpgsqlConnection CreateConnection();
    NpgsqlConnection CreateOpenConnection();
}

public class DapperContext : IDapperContext
{
    private readonly string _connectionString;
    private NpgsqlConnection? _connection;
    private readonly ILogger<DapperContext>? _logger;

    static DapperContext()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public DapperContext(IConfiguration configuration, ILogger<DapperContext>? logger = null)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
        _logger?.LogInformation("DapperContext 初始化，连接字符串: {ConnectionString}", _connectionString);
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public NpgsqlConnection CreateOpenConnection()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            _connection = CreateConnection();
            _connection.Open();
        }
        return _connection;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
