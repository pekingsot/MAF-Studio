using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class LlmConfigRepository : ILlmConfigRepository
{
    private readonly IDapperContext _context;

    public LlmConfigRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<LlmConfig?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM llm_configs WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<LlmConfig>(sql, new { Id = id });
    }

    public async Task<List<LlmConfig>> GetByUserIdAsync(string userId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM llm_configs WHERE user_id = @UserId ORDER BY created_at DESC";
        var result = await connection.QueryAsync<LlmConfig>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task<List<LlmConfig>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM llm_configs ORDER BY name";
        var result = await connection.QueryAsync<LlmConfig>(sql);
        return result.ToList();
    }

    public async Task<LlmConfig> CreateAsync(LlmConfig config)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            INSERT INTO llm_configs (id, name, provider, api_key, endpoint, default_model, extra_config, user_id, created_at, updated_at)
            VALUES (@Id, @Name, @Provider, @ApiKey, @Endpoint, @DefaultModel, @ExtraConfig, @UserId, @CreatedAt, @UpdatedAt)
            RETURNING *";
        return await connection.QueryFirstAsync<LlmConfig>(sql, config);
    }

    public async Task<LlmConfig> UpdateAsync(LlmConfig config)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE llm_configs SET 
                name = @Name,
                provider = @Provider,
                api_key = @ApiKey,
                endpoint = @Endpoint,
                default_model = @DefaultModel,
                extra_config = @ExtraConfig,
                updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<LlmConfig>(sql, config);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM llm_configs WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}
