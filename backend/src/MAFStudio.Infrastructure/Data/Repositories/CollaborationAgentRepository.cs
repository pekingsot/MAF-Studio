using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Infrastructure.Data;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class CollaborationAgentRepository : ICollaborationAgentRepository
{
    private readonly IDapperContext _context;

    public CollaborationAgentRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<List<CollaborationAgent>> GetByCollaborationIdAsync(long collaborationId)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            SELECT * FROM collaboration_agents 
            WHERE collaboration_id = @CollaborationId 
            ORDER BY id";
        
        var result = await connection.QueryAsync<CollaborationAgent>(sql, new { CollaborationId = collaborationId });
        return result.ToList();
    }

    public async Task<List<CollaborationAgentWithDetails>> GetWithAgentDetailsByCollaborationIdAsync(long collaborationId)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            SELECT 
                ca.id,
                ca.collaboration_id,
                ca.agent_id,
                ca.role,
                ca.joined_at,
                a.name as agent_name,
                a.type as agent_type,
                a.status as agent_status,
                a.avatar as agent_avatar
            FROM collaboration_agents ca
            INNER JOIN agents a ON ca.agent_id = a.id
            WHERE ca.collaboration_id = @CollaborationId 
            ORDER BY ca.id";
        
        var result = await connection.QueryAsync<CollaborationAgentWithDetails>(sql, new { CollaborationId = collaborationId });
        return result.ToList();
    }

    public async Task<List<CollaborationAgent>> GetByAgentIdAsync(long agentId)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            SELECT * FROM collaboration_agents 
            WHERE agent_id = @AgentId 
            ORDER BY id";
        
        var result = await connection.QueryAsync<CollaborationAgent>(sql, new { AgentId = agentId });
        return result.ToList();
    }

    public async Task<CollaborationAgent?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT * FROM collaboration_agents WHERE id = @Id";
        
        return await connection.QueryFirstOrDefaultAsync<CollaborationAgent>(sql, new { Id = id });
    }

    public async Task<CollaborationAgent> CreateAsync(CollaborationAgent collaborationAgent)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            INSERT INTO collaboration_agents (collaboration_id, agent_id, created_at)
            VALUES (@CollaborationId, @AgentId, @CreatedAt)
            RETURNING id";
        
        var id = await connection.ExecuteScalarAsync<long>(sql, collaborationAgent);
        collaborationAgent.Id = id;
        return collaborationAgent;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        var sql = "DELETE FROM collaboration_agents WHERE id = @Id";
        
        var affected = await connection.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    public async Task<bool> DeleteByCollaborationAndAgentAsync(long collaborationId, long agentId)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            DELETE FROM collaboration_agents 
            WHERE collaboration_id = @CollaborationId AND agent_id = @AgentId";
        
        var affected = await connection.ExecuteAsync(sql, new { CollaborationId = collaborationId, AgentId = agentId });
        return affected > 0;
    }
}
