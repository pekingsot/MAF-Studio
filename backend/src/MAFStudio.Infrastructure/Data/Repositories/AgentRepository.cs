using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Utils;

namespace MAFStudio.Infrastructure.Data.Repositories;

/// <summary>
/// 智能体仓储实现
/// </summary>
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
        const string sql = "SELECT * FROM agents WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Agent>(sql, new { Id = id });
    }

    public async Task<List<Agent>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agents ORDER BY created_at DESC";
        var result = await connection.QueryAsync<Agent>(sql);
        return result.ToList();
    }

    public async Task<List<Agent>> GetByUserIdAsync(long userId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agents WHERE user_id = @UserId ORDER BY created_at DESC";
        var result = await connection.QueryAsync<Agent>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task<Agent> CreateAsync(Agent agent)
    {
        using var connection = _context.CreateConnection();
        agent.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO agents (name, description, type, type_name, system_prompt, avatar, user_id, status, 
                llm_configs, created_at, updated_at)
            VALUES (@Name, @Description, @Type, @TypeName, @SystemPrompt, @Avatar, @UserId, @Status, 
                @LlmConfigs, @CreatedAt, @UpdatedAt)
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
                type_name = @TypeName,
                system_prompt = @SystemPrompt,
                avatar = @Avatar,
                llm_configs = @LlmConfigs,
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
