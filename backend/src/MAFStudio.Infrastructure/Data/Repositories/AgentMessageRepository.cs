using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Infrastructure.Data;

namespace MAFStudio.Infrastructure.Data.Repositories
{
    public class AgentMessageRepository : IAgentMessageRepository
    {
        private readonly IDapperContext _context;

        public AgentMessageRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<List<AgentMessage>> GetByCollaborationIdAsync(long collaborationId)
        {
            using var connection = _context.CreateConnection();
            var sql = @"
                SELECT 
                    id,
                    from_agent_id,
                    to_agent_id,
                    collaboration_id,
                    content,
                    sender_type,
                    sender_name,
                    user_id,
                    is_streaming,
                    created_at
                FROM agent_messages
                WHERE collaboration_id = @CollaborationId
                ORDER BY created_at ASC";
            
            var result = await connection.QueryAsync<AgentMessage>(sql, new { CollaborationId = collaborationId });
            return result.ToList();
        }

        public async Task<AgentMessage> CreateAsync(AgentMessage message)
        {
            using var connection = _context.CreateConnection();
            var sql = @"
                INSERT INTO agent_messages (
                    from_agent_id,
                    to_agent_id,
                    collaboration_id,
                    content,
                    sender_type,
                    sender_name,
                    is_streaming,
                    created_at
                ) VALUES (
                    @FromAgentId,
                    @ToAgentId,
                    @CollaborationId,
                    @Content,
                    @SenderType,
                    @SenderName,
                    @IsStreaming,
                    @CreatedAt
                )
                RETURNING id";
            
            var id = await connection.ExecuteScalarAsync<long>(sql, message);
            message.Id = id;
            return message;
        }
    }
}
