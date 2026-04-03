using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Utils;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class CollaborationRepository : ICollaborationRepository
{
    private readonly IDapperContext _context;

    public CollaborationRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<Collaboration?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM collaborations WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Collaboration>(sql, new { Id = id });
    }

    public async Task<List<Collaboration>> GetByUserIdAsync(long userId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM collaborations WHERE user_id = @UserId ORDER BY created_at DESC";
        var result = await connection.QueryAsync<Collaboration>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task<Collaboration> CreateAsync(Collaboration collaboration)
    {
        using var connection = _context.CreateConnection();
        collaboration.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO collaborations (name, description, path, status, user_id, git_repository_url, git_branch, git_username, git_email, git_access_token, created_at, updated_at)
            VALUES (@Name, @Description, @Path, @Status, @UserId, @GitRepositoryUrl, @GitBranch, @GitUsername, @GitEmail, @GitAccessToken, @CreatedAt, @UpdatedAt)
            RETURNING *";
        return await connection.QueryFirstAsync<Collaboration>(sql, collaboration);
    }

    public async Task<Collaboration> UpdateAsync(Collaboration collaboration)
    {
        using var connection = _context.CreateConnection();
        collaboration.MarkAsUpdated();
        const string sql = @"
            UPDATE collaborations SET 
                name = @Name,
                description = @Description,
                path = @Path,
                status = @Status,
                git_repository_url = @GitRepositoryUrl,
                git_branch = @GitBranch,
                git_username = @GitUsername,
                git_email = @GitEmail,
                git_access_token = @GitAccessToken,
                updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<Collaboration>(sql, collaboration);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM collaborations WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<bool> AddAgentAsync(long collaborationId, long agentId, string? role, string? customPrompt)
    {
        using var connection = _context.CreateConnection();
        var id = SnowflakeIdGenerator.Instance.NextId();
        const string sql = @"
            INSERT INTO collaboration_agents (id, collaboration_id, agent_id, role, custom_prompt, joined_at)
            VALUES (@Id, @CollaborationId, @AgentId, @Role, @CustomPrompt, @JoinedAt)";
        var rows = await connection.ExecuteAsync(sql, new 
        { 
            Id = id, 
            CollaborationId = collaborationId, 
            AgentId = agentId, 
            Role = role, 
            CustomPrompt = customPrompt,
            JoinedAt = DateTime.UtcNow 
        });
        return rows > 0;
    }

    public async Task<bool> RemoveAgentAsync(long collaborationId, long agentId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM collaboration_agents WHERE collaboration_id = @CollaborationId AND agent_id = @AgentId";
        var rows = await connection.ExecuteAsync(sql, new { CollaborationId = collaborationId, AgentId = agentId });
        return rows > 0;
    }

    public async Task<bool> UpdateAgentRoleAsync(long collaborationId, long agentId, string role, string? customPrompt)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE collaboration_agents 
            SET role = @Role, custom_prompt = @CustomPrompt
            WHERE collaboration_id = @CollaborationId AND agent_id = @AgentId";
        var rows = await connection.ExecuteAsync(sql, new 
        { 
            CollaborationId = collaborationId, 
            AgentId = agentId, 
            Role = role, 
            CustomPrompt = customPrompt 
        });
        return rows > 0;
    }

    public async Task<List<CollaborationAgent>> GetAgentsAsync(long collaborationId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM collaboration_agents WHERE collaboration_id = @CollaborationId";
        var result = await connection.QueryAsync<CollaborationAgent>(sql, new { CollaborationId = collaborationId });
        return result.ToList();
    }
}
