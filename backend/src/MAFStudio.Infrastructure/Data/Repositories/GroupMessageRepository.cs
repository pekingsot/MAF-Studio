using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Infrastructure.Data;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class GroupMessageRepository : IGroupMessageRepository
{
    private readonly IDapperContext _context;

    public GroupMessageRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<List<GroupMessage>> GetByCollaborationIdAsync(long collaborationId, int limit = 50, long? beforeId = null)
    {
        using var connection = _context.CreateConnection();

        string sql;
        object param;

        if (beforeId.HasValue)
        {
            sql = @"
                SELECT * FROM group_messages
                WHERE collaboration_id = @CollaborationId AND id < @BeforeId
                ORDER BY id DESC
                LIMIT @Limit";
            param = new { CollaborationId = collaborationId, BeforeId = beforeId.Value, Limit = limit };
        }
        else
        {
            sql = @"
                SELECT * FROM group_messages
                WHERE collaboration_id = @CollaborationId
                ORDER BY id DESC
                LIMIT @Limit";
            param = new { CollaborationId = collaborationId, Limit = limit };
        }

        var result = await connection.QueryAsync<GroupMessage>(sql, param);
        return result.OrderBy(m => m.Id).ToList();
    }

    public async Task<GroupMessage> CreateAsync(GroupMessage message)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            INSERT INTO group_messages (
                collaboration_id, message_type, sender_type,
                from_agent_id, to_agent_id, from_agent_name, from_agent_role,
                from_agent_type, from_agent_avatar, model_name, llm_config_name,
                content, is_mentioned, created_at
            ) VALUES (
                @CollaborationId, @MessageType, @SenderType,
                @FromAgentId, @ToAgentId, @FromAgentName, @FromAgentRole,
                @FromAgentType, @FromAgentAvatar, @ModelName, @LlmConfigName,
                @Content, @IsMentioned, @CreatedAt
            )
            RETURNING id";

        var id = await connection.ExecuteScalarAsync<long>(sql, message);
        message.Id = id;
        return message;
    }
}
