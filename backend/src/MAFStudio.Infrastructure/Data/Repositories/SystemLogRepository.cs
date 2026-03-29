using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class SystemLogRepository : ISystemLogRepository
{
    private readonly IDapperContext _context;

    public SystemLogRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<SystemLog?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM system_logs WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<SystemLog>(sql, new { Id = id });
    }

    public async Task<List<SystemLog>> GetAsync(string? level = null, string? category = null, long? userId = null, int limit = 100)
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT * FROM system_logs WHERE 1=1";
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(level))
        {
            sql += " AND level = @Level";
            parameters.Add("Level", level);
        }

        if (!string.IsNullOrEmpty(category))
        {
            sql += " AND category = @Category";
            parameters.Add("Category", category);
        }

        if (userId.HasValue)
        {
            sql += " AND user_id = @UserId";
            parameters.Add("UserId", userId.Value);
        }

        sql += " ORDER BY created_at DESC LIMIT @Limit";
        parameters.Add("Limit", limit);

        var result = await connection.QueryAsync<SystemLog>(sql, parameters);
        return result.ToList();
    }

    public async Task<SystemLog> CreateAsync(SystemLog log)
    {
        using var connection = _context.CreateConnection();
        log.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO system_logs (level, category, message, exception, stack_trace, user_id, request_path, additional_data, created_at)
            VALUES (@Level, @Category, @Message, @Exception, @StackTrace, @UserId, @RequestPath, @AdditionalData, @CreatedAt)
            RETURNING *";
        return await connection.QueryFirstAsync<SystemLog>(sql, log);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM system_logs WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}
