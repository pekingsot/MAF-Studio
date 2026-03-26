using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class AgentMessageRepository : IAgentMessageRepository
{
    private readonly IDapperContext _context;

    public AgentMessageRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<AgentMessage?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agent_messages WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<AgentMessage>(sql, new { Id = id });
    }

    public async Task<List<AgentMessage>> GetByCollaborationIdAsync(long collaborationId, int limit = 100)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            SELECT * FROM agent_messages 
            WHERE collaboration_id = @CollaborationId 
            ORDER BY created_at DESC 
            LIMIT @Limit";
        var result = await connection.QueryAsync<AgentMessage>(sql, new { CollaborationId = collaborationId, Limit = limit });
        return result.ToList();
    }

    public async Task<AgentMessage> CreateAsync(AgentMessage message)
    {
        using var connection = _context.CreateConnection();
        message.GenerateId();
        message.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO agent_messages (id, from_agent_id, to_agent_id, collaboration_id, content, sender_type, sender_name, user_id, created_at, is_streaming)
            VALUES (@Id, @FromAgentId, @ToAgentId, @CollaborationId, @Content, @SenderType, @SenderName, @UserId, @CreatedAt, @IsStreaming)
            RETURNING *";
        return await connection.QueryFirstAsync<AgentMessage>(sql, message);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM agent_messages WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}
