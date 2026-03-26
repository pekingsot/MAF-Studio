using Dapper;
using Microsoft.Extensions.Configuration;
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

    static DapperContext()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
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
