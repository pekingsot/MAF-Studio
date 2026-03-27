using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class OperationLogRepository : IOperationLogRepository
{
    private readonly IDapperContext _context;

    public OperationLogRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<OperationLog?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM operation_logs WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<OperationLog>(sql, new { Id = id });
    }

    public async Task<List<OperationLog>> GetByUserIdAsync(string userId, int limit = 100)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            SELECT * FROM operation_logs 
            WHERE user_id = @UserId 
            ORDER BY created_at DESC 
            LIMIT @Limit";
        var result = await connection.QueryAsync<OperationLog>(sql, new { UserId = userId, Limit = limit });
        return result.ToList();
    }

    public async Task<List<OperationLog>> GetAllAsync(int limit = 100)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM operation_logs ORDER BY created_at DESC LIMIT @Limit";
        var result = await connection.QueryAsync<OperationLog>(sql, new { Limit = limit });
        return result.ToList();
    }

    public async Task<OperationLog> CreateAsync(OperationLog log)
    {
        using var connection = _context.CreateConnection();
        log.GenerateId();
        log.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO operation_logs (
                id, user_id, action, resource_type, resource_id, description, details, 
                ip_address, user_agent, request_path, request_method, status_code, duration_ms, error_message, created_at
            ) VALUES (
                @Id, @UserId, @Action, @ResourceType, @ResourceId, @Description, @Details,
                @IpAddress, @UserAgent, @RequestPath, @RequestMethod, @StatusCode, @DurationMs, @ErrorMessage, @CreatedAt
            )
            RETURNING *";
        return await connection.QueryFirstAsync<OperationLog>(sql, log);
    }
}
