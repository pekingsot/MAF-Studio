using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Utils;
using System.Text.Json;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class AgentRepository : IAgentRepository
{
    private readonly IDapperContext _context;
    private readonly ILlmModelConfigRepository _modelConfigRepository;

    public AgentRepository(IDapperContext context, ILlmModelConfigRepository modelConfigRepository)
    {
        _context = context;
        _modelConfigRepository = modelConfigRepository;
    }

    public async Task<Agent?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        
        const string agentSql = "SELECT * FROM agents WHERE id = @Id";
        var agent = await connection.QueryFirstOrDefaultAsync<Agent>(agentSql, new { Id = id });
        
        if (agent == null)
        {
            return null;
        }

        var allLlmConfigIds = new HashSet<long>();
        
        if (agent.LlmConfigId.HasValue)
        {
            allLlmConfigIds.Add(agent.LlmConfigId.Value);
        }
        
        if (!string.IsNullOrEmpty(agent.FallbackModels))
        {
            try
            {
                var fallbackConfigs = JsonSerializer.Deserialize<List<Core.DTOs.FallbackModelConfig>>(agent.FallbackModels);
                if (fallbackConfigs != null)
                {
                    foreach (var config in fallbackConfigs)
                    {
                        allLlmConfigIds.Add(config.LlmConfigId);
                    }
                }
            }
            catch
            {
            }
        }
        
        if (allLlmConfigIds.Count > 0)
        {
            const string llmConfigSql = "SELECT * FROM llm_configs WHERE id = ANY(@Ids)";
            var llmConfigs = (await connection.QueryAsync<LlmConfig>(llmConfigSql, new { Ids = allLlmConfigIds.ToArray() })).ToList();
            
            foreach (var llmConfig in llmConfigs)
            {
                llmConfig.Models = await _modelConfigRepository.GetByLlmConfigIdAsync(llmConfig.Id);
            }
            
            agent.AllLlmConfigs = llmConfigs;
            
            if (agent.LlmConfigId.HasValue)
            {
                agent.LlmConfig = llmConfigs.FirstOrDefault(c => c.Id == agent.LlmConfigId.Value);
            }
        }

        return agent;
    }

    public async Task<List<Agent>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agents ORDER BY name";
        var result = await connection.QueryAsync<Agent>(sql);
        var agents = result.ToList();
        
        await LoadAllLlmConfigsAsync(connection, agents);
        
        return agents;
    }

    public async Task<List<Agent>> GetByUserIdAsync(string userId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agents WHERE user_id = @UserId ORDER BY name";
        var result = await connection.QueryAsync<Agent>(sql, new { UserId = userId });
        var agents = result.ToList();
        
        await LoadAllLlmConfigsAsync(connection, agents);
        
        return agents;
    }
    
    private async Task LoadAllLlmConfigsAsync(System.Data.IDbConnection connection, List<Agent> agents)
    {
        var allLlmConfigIds = new HashSet<long>();
        
        foreach (var agent in agents)
        {
            if (agent.LlmConfigId.HasValue)
            {
                allLlmConfigIds.Add(agent.LlmConfigId.Value);
            }
            
            if (!string.IsNullOrEmpty(agent.FallbackModels))
            {
                try
                {
                    var fallbackConfigs = JsonSerializer.Deserialize<List<Core.DTOs.FallbackModelConfig>>(agent.FallbackModels);
                    if (fallbackConfigs != null)
                    {
                        foreach (var config in fallbackConfigs)
                        {
                            allLlmConfigIds.Add(config.LlmConfigId);
                        }
                    }
                }
                catch
                {
                }
            }
        }
        
        if (allLlmConfigIds.Count > 0)
        {
            const string llmConfigSql = "SELECT * FROM llm_configs WHERE id = ANY(@Ids)";
            var llmConfigs = (await connection.QueryAsync<LlmConfig>(llmConfigSql, new { Ids = allLlmConfigIds.ToArray() })).ToList();
            
            foreach (var llmConfig in llmConfigs)
            {
                llmConfig.Models = await _modelConfigRepository.GetByLlmConfigIdAsync(llmConfig.Id);
            }
            
            foreach (var agent in agents)
            {
                agent.AllLlmConfigs = llmConfigs;
                
                if (agent.LlmConfigId.HasValue)
                {
                    agent.LlmConfig = llmConfigs.FirstOrDefault(c => c.Id == agent.LlmConfigId.Value);
                }
            }
        }
    }

    public async Task<Agent> CreateAsync(Agent agent)
    {
        using var connection = _context.CreateConnection();
        agent.GenerateId();
        agent.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO agents (id, name, description, type, system_prompt, avatar, user_id, status, llm_config_id, llm_model_config_id, fallback_models, created_at, updated_at)
            VALUES (@Id, @Name, @Description, @Type, @SystemPrompt, @Avatar, @UserId, @Status, @LlmConfigId, @LlmModelConfigId, @FallbackModels, @CreatedAt, @UpdatedAt)
            RETURNING *";
        return await connection.QueryFirstAsync<Agent>(sql, agent);
    }

    public async Task<Agent> UpdateAsync(Agent agent)
    {
        using var connection = _context.CreateConnection();
        agent.MarkAsUpdated();
        const string sql = @"
            UPDATE agents SET 
                name = @Name,
                description = @Description,
                type = @Type,
                system_prompt = @SystemPrompt,
                avatar = @Avatar,
                llm_config_id = @LlmConfigId,
                llm_model_config_id = @LlmModelConfigId,
                fallback_models = @FallbackModels,
                updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<Agent>(sql, agent);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM agents WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<bool> UpdateStatusAsync(long id, AgentStatus status)
    {
        using var connection = _context.CreateConnection();
        const string sql = "UPDATE agents SET status = @Status, updated_at = @UpdatedAt WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id, Status = status, UpdatedAt = DateTime.UtcNow });
        return rows > 0;
    }
}
