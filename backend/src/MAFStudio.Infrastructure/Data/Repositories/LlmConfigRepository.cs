using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class LlmConfigRepository : ILlmConfigRepository
{
    private readonly IDapperContext _context;
    private readonly ILlmModelConfigRepository _modelConfigRepository;

    public LlmConfigRepository(IDapperContext context, ILlmModelConfigRepository modelConfigRepository)
    {
        _context = context;
        _modelConfigRepository = modelConfigRepository;
    }

    public async Task<LlmConfig?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM llm_configs WHERE id = @Id";
        var config = await connection.QueryFirstOrDefaultAsync<LlmConfig>(sql, new { Id = id });
        
        if (config != null)
        {
            config.Models = await _modelConfigRepository.GetByLlmConfigIdAsync(config.Id);
        }
        
        return config;
    }

    public async Task<List<LlmConfig>> GetByUserIdAsync(string userId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM llm_configs WHERE user_id = @UserId ORDER BY created_at DESC";
        var result = await connection.QueryAsync<LlmConfig>(sql, new { UserId = userId });
        var configs = result.ToList();
        
        foreach (var config in configs)
        {
            config.Models = await _modelConfigRepository.GetByLlmConfigIdAsync(config.Id);
        }
        
        return configs;
    }

    public async Task<List<LlmConfig>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM llm_configs ORDER BY name";
        var result = await connection.QueryAsync<LlmConfig>(sql);
        var configs = result.ToList();
        
        foreach (var config in configs)
        {
            config.Models = await _modelConfigRepository.GetByLlmConfigIdAsync(config.Id);
        }
        
        return configs;
    }

    public async Task<LlmConfig> CreateAsync(LlmConfig config)
    {
        using var connection = _context.CreateConnection();
        config.GenerateId();
        config.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO llm_configs (id, name, provider, api_key, endpoint, default_model, user_id, is_default, is_enabled, created_at, updated_at)
            VALUES (@Id, @Name, @Provider, @ApiKey, @Endpoint, @DefaultModel, @UserId, @IsDefault, @IsEnabled, @CreatedAt, @UpdatedAt)
            RETURNING *";
        return await connection.QueryFirstAsync<LlmConfig>(sql, config);
    }

    public async Task<LlmConfig> UpdateAsync(LlmConfig config)
    {
        using var connection = _context.CreateConnection();
        config.MarkAsUpdated();
        const string sql = @"
            UPDATE llm_configs SET 
                name = @Name,
                provider = @Provider,
                api_key = @ApiKey,
                endpoint = @Endpoint,
                default_model = @DefaultModel,
                is_default = @IsDefault,
                is_enabled = @IsEnabled,
                updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<LlmConfig>(sql, config);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM llm_configs WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task SetDefaultAsync(long id, string userId)
    {
        using var connection = _context.CreateConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(
                "UPDATE llm_configs SET is_default = false WHERE user_id = @UserId",
                new { UserId = userId },
                transaction);
            
            await connection.ExecuteAsync(
                "UPDATE llm_configs SET is_default = true WHERE id = @Id",
                new { Id = id },
                transaction);
            
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
