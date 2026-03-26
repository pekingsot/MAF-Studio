using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Utils;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class AgentRepository : IAgentRepository
{
    private readonly IDapperContext _context;

    public AgentRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<Agent?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            SELECT a.*, l.id, l.name, l.provider, l.api_key, l.endpoint, l.default_model, l.extra_config, l.user_id, l.created_at, l.updated_at
            FROM agents a
            LEFT JOIN llm_configs l ON a.llm_config_id = l.id
            WHERE a.id = @Id";
        
        var result = await connection.QueryAsync<Agent, LlmConfig?, Agent>(
            sql,
            (agent, llmConfig) =>
            {
                agent.LlmConfig = llmConfig;
                return agent;
            },
            new { Id = id },
            splitOn: "id"
        );
        
        return result.FirstOrDefault();
    }

    public async Task<List<Agent>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agents ORDER BY name";
        var result = await connection.QueryAsync<Agent>(sql);
        return result.ToList();
    }

    public async Task<List<Agent>> GetByUserIdAsync(string userId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agents WHERE user_id = @UserId ORDER BY name";
        var result = await connection.QueryAsync<Agent>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task<Agent> CreateAsync(Agent agent)
    {
        using var connection = _context.CreateConnection();
        agent.GenerateId();
        agent.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO agents (id, name, description, type, configuration, avatar, user_id, status, llm_config_id, llm_model_config_id, created_at, updated_at)
            VALUES (@Id, @Name, @Description, @Type, @Configuration, @Avatar, @UserId, @Status, @LlmConfigId, @LlmModelConfigId, @CreatedAt, @UpdatedAt)
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
                configuration = @Configuration,
                avatar = @Avatar,
                llm_config_id = @LlmConfigId,
                llm_model_config_id = @LlmModelConfigId,
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
