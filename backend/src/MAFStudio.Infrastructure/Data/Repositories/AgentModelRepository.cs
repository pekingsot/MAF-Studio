using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Infrastructure.Data.Repositories;

/// <summary>
/// 智能体模型配置仓储实现
/// </summary>
public class AgentModelRepository : IAgentModelRepository
{
    private readonly IDapperContext _context;

    public AgentModelRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<AgentModel?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agent_models WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<AgentModel>(sql, new { Id = id });
    }

    public async Task<List<AgentModel>> GetByAgentIdAsync(long agentId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agent_models WHERE agent_id = @AgentId ORDER BY priority, id";
        var result = await connection.QueryAsync<AgentModel>(sql, new { AgentId = agentId });
        return result.ToList();
    }

    public async Task<AgentModel?> GetPrimaryModelAsync(long agentId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agent_models WHERE agent_id = @AgentId AND is_primary = true AND is_enabled = true";
        return await connection.QueryFirstOrDefaultAsync<AgentModel>(sql, new { AgentId = agentId });
    }

    public async Task<List<AgentModel>> GetAvailableModelsAsync(long agentId)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            SELECT * FROM agent_models 
            WHERE agent_id = @AgentId AND is_enabled = true 
            ORDER BY priority, id";
        var result = await connection.QueryAsync<AgentModel>(sql, new { AgentId = agentId });
        return result.ToList();
    }

    public async Task<AgentModel> CreateAsync(AgentModel agentModel)
    {
        using var connection = _context.CreateConnection();
        agentModel.GenerateId();
        agentModel.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO agent_models (id, agent_id, llm_config_id, llm_model_config_id, priority, is_primary, is_enabled, created_at)
            VALUES (@Id, @AgentId, @LlmConfigId, @LlmModelConfigId, @Priority, @IsPrimary, @IsEnabled, @CreatedAt)
            RETURNING *";
        return await connection.QueryFirstAsync<AgentModel>(sql, agentModel);
    }

    public async Task<AgentModel> UpdateAsync(AgentModel agentModel)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE agent_models SET 
                llm_config_id = @LlmConfigId,
                llm_model_config_id = @LlmModelConfigId,
                priority = @Priority,
                is_primary = @IsPrimary,
                is_enabled = @IsEnabled
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<AgentModel>(sql, agentModel);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM agent_models WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<bool> DeleteByAgentIdAsync(long agentId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM agent_models WHERE agent_id = @AgentId";
        var rows = await connection.ExecuteAsync(sql, new { AgentId = agentId });
        return rows > 0;
    }

    public async Task SetPrimaryAsync(long agentId, long agentModelId)
    {
        using var connection = _context.CreateConnection();
        using var transaction = connection.BeginTransaction();
        
        try
        {
            const string clearSql = "UPDATE agent_models SET is_primary = false WHERE agent_id = @AgentId";
            await connection.ExecuteAsync(clearSql, new { AgentId = agentId }, transaction);
            
            const string setSql = "UPDATE agent_models SET is_primary = true WHERE id = @Id";
            await connection.ExecuteAsync(setSql, new { Id = agentModelId }, transaction);
            
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<List<AgentModel>> CreateBatchAsync(List<AgentModel> agentModels)
    {
        var results = new List<AgentModel>();
        using var connection = _context.CreateConnection();
        using var transaction = connection.BeginTransaction();
        
        try
        {
            const string sql = @"
                INSERT INTO agent_models (id, agent_id, llm_config_id, llm_model_config_id, priority, is_primary, is_enabled, created_at)
                VALUES (@Id, @AgentId, @LlmConfigId, @LlmModelConfigId, @Priority, @IsPrimary, @IsEnabled, @CreatedAt)
                RETURNING *";
            
            foreach (var model in agentModels)
            {
                model.GenerateId();
                model.CreatedAt = DateTime.UtcNow;
                var result = await connection.QueryFirstAsync<AgentModel>(sql, model, transaction);
                results.Add(result);
            }
            
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        
        return results;
    }
}
