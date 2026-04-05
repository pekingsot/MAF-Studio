using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class WorkflowSessionRepository : IWorkflowSessionRepository
{
    private readonly IDapperContext _context;

    public WorkflowSessionRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<WorkflowSession?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM workflow_sessions WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<WorkflowSession>(sql, new { Id = id });
    }

    public async Task<WorkflowSession?> GetActiveByCollaborationIdAsync(long collaborationId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM workflow_sessions WHERE collaboration_id = @CollaborationId AND status = 'running'";
        return await connection.QueryFirstOrDefaultAsync<WorkflowSession>(sql, new { CollaborationId = collaborationId });
    }

    public async Task<List<WorkflowSession>> GetByCollaborationIdAsync(long collaborationId, int limit = 20)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM workflow_sessions WHERE collaboration_id = @CollaborationId ORDER BY started_at DESC LIMIT @Limit";
        var result = await connection.QueryAsync<WorkflowSession>(sql, new { CollaborationId = collaborationId, Limit = limit });
        return result.ToList();
    }

    public async Task<List<WorkflowSession>> GetByTaskIdAsync(long taskId, int limit = 20)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM workflow_sessions WHERE task_id = @TaskId ORDER BY started_at DESC LIMIT @Limit";
        var result = await connection.QueryAsync<WorkflowSession>(sql, new { TaskId = taskId, Limit = limit });
        return result.ToList();
    }

    public async Task<WorkflowSession> CreateAsync(WorkflowSession session)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            INSERT INTO workflow_sessions (
                collaboration_id, task_id, workflow_type, orchestration_mode, 
                status, topic, metadata, total_rounds, total_messages,
                conclusion, error_message, started_at, completed_at, created_at
            )
            VALUES (
                @CollaborationId, @TaskId, @WorkflowType, @OrchestrationMode,
                @Status, @Topic, @Metadata, @TotalRounds, @TotalMessages,
                @Conclusion, @ErrorMessage, @StartedAt, @CompletedAt, @CreatedAt
            )
            RETURNING *";
        return await connection.QueryFirstAsync<WorkflowSession>(sql, session);
    }

    public async Task<WorkflowSession> UpdateAsync(WorkflowSession session)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE workflow_sessions SET 
                status = @Status,
                total_rounds = @TotalRounds,
                total_messages = @TotalMessages,
                conclusion = @Conclusion,
                error_message = @ErrorMessage,
                completed_at = @CompletedAt
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<WorkflowSession>(sql, session);
    }

    public async Task<bool> EndSessionAsync(long id, string? conclusion = null, string? errorMessage = null)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE workflow_sessions SET 
                status = CASE WHEN @ErrorMessage IS NULL THEN 'completed' ELSE 'failed' END,
                completed_at = @CompletedAt,
                conclusion = @Conclusion,
                error_message = @ErrorMessage
            WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { 
            Id = id, 
            CompletedAt = DateTime.UtcNow, 
            Conclusion = conclusion,
            ErrorMessage = errorMessage
        });
        return rows > 0;
    }

    public async Task<bool> IncrementMessageCountAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "UPDATE workflow_sessions SET total_messages = total_messages + 1 WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}

public class MessageRepository : IMessageRepository
{
    private readonly IDapperContext _context;

    public MessageRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<Message?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM messages WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Message>(sql, new { Id = id });
    }

    public async Task<List<Message>> GetBySessionIdAsync(long sessionId, int limit = 1000)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            SELECT * FROM messages 
            WHERE session_id = @SessionId 
            ORDER BY COALESCE(round_number, step_number, 0), created_at 
            LIMIT @Limit";
        var result = await connection.QueryAsync<Message>(sql, new { SessionId = sessionId, Limit = limit });
        return result.ToList();
    }

    public async Task<List<Message>> GetByCollaborationIdAsync(long collaborationId, int limit = 100)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM messages WHERE collaboration_id = @CollaborationId ORDER BY created_at DESC LIMIT @Limit";
        var result = await connection.QueryAsync<Message>(sql, new { CollaborationId = collaborationId, Limit = limit });
        return result.ToList();
    }

    public async Task<List<Message>> GetByTaskIdAsync(long taskId, int limit = 100)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM messages WHERE task_id = @TaskId ORDER BY created_at DESC LIMIT @Limit";
        var result = await connection.QueryAsync<Message>(sql, new { TaskId = taskId, Limit = limit });
        return result.ToList();
    }

    public async Task<Message> CreateAsync(Message message)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            INSERT INTO messages (
                session_id, collaboration_id, task_id, message_type,
                round_number, step_number, from_agent_id, from_agent_name, from_agent_role,
                to_agent_id, content, thinking_process, selected_next_speaker, selection_reason,
                metadata, created_at
            )
            VALUES (
                @SessionId, @CollaborationId, @TaskId, @MessageType,
                @RoundNumber, @StepNumber, @FromAgentId, @FromAgentName, @FromAgentRole,
                @ToAgentId, @Content, @ThinkingProcess, @SelectedNextSpeaker, @SelectionReason,
                @Metadata, @CreatedAt
            )
            RETURNING *";
        return await connection.QueryFirstAsync<Message>(sql, message);
    }

    public async Task<int> GetMessageCountBySessionAsync(long sessionId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT COUNT(*) FROM messages WHERE session_id = @SessionId";
        return await connection.ExecuteScalarAsync<int>(sql, new { SessionId = sessionId });
    }
}
