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

    public async Task<(List<SystemLog> Data, int Total)> GetPagedAsync(
        string? level = null, string? category = null, string? keyword = null,
        DateTime? startTime = null, DateTime? endTime = null,
        int page = 1, int pageSize = 20)
    {
        using var connection = _context.CreateConnection();

        var whereClauses = new List<string> { "1=1" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(level))
        {
            whereClauses.Add("level = @Level");
            parameters.Add("Level", level);
        }

        if (!string.IsNullOrEmpty(category))
        {
            whereClauses.Add("category = @Category");
            parameters.Add("Category", category);
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            whereClauses.Add("(message ILIKE @Keyword OR category ILIKE @Keyword)");
            parameters.Add("Keyword", $"%{keyword}%");
        }

        if (startTime.HasValue)
        {
            whereClauses.Add("created_at >= @StartTime");
            parameters.Add("StartTime", startTime.Value);
        }

        if (endTime.HasValue)
        {
            whereClauses.Add("created_at <= @EndTime");
            parameters.Add("EndTime", endTime.Value);
        }

        var whereClause = string.Join(" AND ", whereClauses);

        var countSql = $"SELECT COUNT(*) FROM system_logs WHERE {whereClause}";
        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var dataSql = $"SELECT * FROM system_logs WHERE {whereClause} ORDER BY created_at DESC OFFSET @Offset LIMIT @PageSize";
        var data = (await connection.QueryAsync<SystemLog>(dataSql, parameters)).ToList();

        return (data, total);
    }

    public async Task<SystemLog> CreateAsync(SystemLog log)
    {
        using var connection = _context.CreateConnection();
        log.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO system_logs (level, category, message, exception, stack_trace, user_id, request_path, request_method, source, additional_data, created_at)
            VALUES (@Level, @Category, @Message, @Exception, @StackTrace, @UserId, @RequestPath, @RequestMethod, @Source, @AdditionalData, @CreatedAt)
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

    public async Task<int> DeleteBeforeAsync(DateTime cutoff)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM system_logs WHERE created_at < @Cutoff";
        return await connection.ExecuteAsync(sql, new { Cutoff = cutoff });
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT DISTINCT category FROM system_logs WHERE category IS NOT NULL ORDER BY category";
        var result = await connection.QueryAsync<string>(sql);
        return result.ToList();
    }

    public async Task<Dictionary<string, int>> GetLevelCountsAsync(int days = 7)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            SELECT level, COUNT(*) as count 
            FROM system_logs 
            WHERE created_at >= NOW() - INTERVAL '@Days days'
            GROUP BY level";
        var result = await connection.QueryAsync<(string Level, int Count)>(sql, new { Days = days });
        return result.ToDictionary(r => r.Level, r => r.Count);
    }

    public async Task<List<object>> GetDailyCountsAsync(int days = 7)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            SELECT DATE(created_at) as date, COUNT(*) as count 
            FROM system_logs 
            WHERE created_at >= NOW() - INTERVAL '@Days days'
            GROUP BY DATE(created_at) 
            ORDER BY date";
        var result = await connection.QueryAsync(sql, new { Days = days });
        return result.Cast<object>().ToList();
    }
}
