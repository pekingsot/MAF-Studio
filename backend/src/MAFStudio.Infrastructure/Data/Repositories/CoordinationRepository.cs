using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class CoordinationSessionRepository : ICoordinationSessionRepository
{
    private readonly IDapperContext _context;

    public CoordinationSessionRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<CoordinationSession?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT id, collaboration_id, task_id, orchestration_mode, status, topic, metadata, started_at as start_time, completed_at as end_time, total_rounds, total_messages, conclusion, created_at FROM workflow_sessions WHERE id = @Id AND workflow_type = 'GroupChat'";
        return await connection.QueryFirstOrDefaultAsync<CoordinationSession>(sql, new { Id = id });
    }

    public async Task<CoordinationSession?> GetActiveByCollaborationIdAsync(long collaborationId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT id, collaboration_id, task_id, orchestration_mode, status, topic, metadata, started_at as start_time, completed_at as end_time, total_rounds, total_messages, conclusion, created_at FROM workflow_sessions WHERE collaboration_id = @CollaborationId AND status = 'running' AND workflow_type = 'GroupChat'";
        return await connection.QueryFirstOrDefaultAsync<CoordinationSession>(sql, new { CollaborationId = collaborationId });
    }

    public async Task<List<CoordinationSession>> GetByCollaborationIdAsync(long collaborationId, int limit = 20)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT id, collaboration_id, task_id, orchestration_mode, status, topic, metadata, started_at as start_time, completed_at as end_time, total_rounds, total_messages, conclusion, created_at FROM workflow_sessions WHERE collaboration_id = @CollaborationId AND workflow_type = 'GroupChat' ORDER BY started_at DESC LIMIT @Limit";
        var result = await connection.QueryAsync<CoordinationSession>(sql, new { CollaborationId = collaborationId, Limit = limit });
        return result.ToList();
    }

    public async Task<List<CoordinationSession>> GetByTaskIdAsync(long taskId, int limit = 20)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT id, collaboration_id, task_id, orchestration_mode, status, topic, metadata, started_at as start_time, completed_at as end_time, total_rounds, total_messages, conclusion, created_at FROM workflow_sessions WHERE task_id = @TaskId AND workflow_type = 'GroupChat' ORDER BY started_at DESC LIMIT @Limit";
        var result = await connection.QueryAsync<CoordinationSession>(sql, new { TaskId = taskId, Limit = limit });
        return result.ToList();
    }

    public async Task<CoordinationSession> CreateAsync(CoordinationSession session)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            INSERT INTO workflow_sessions (collaboration_id, task_id, workflow_type, orchestration_mode, status, topic, metadata, started_at, created_at)
            VALUES (@CollaborationId, @TaskId, 'GroupChat', @OrchestrationMode, @Status, @Topic, @Metadata, @StartTime, @CreatedAt)
            RETURNING id, collaboration_id, task_id, orchestration_mode, status, topic, metadata, started_at as start_time, completed_at as end_time, total_rounds, total_messages, conclusion, created_at";
        return await connection.QueryFirstAsync<CoordinationSession>(sql, session);
    }

    public async Task<CoordinationSession> UpdateAsync(CoordinationSession session)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE workflow_sessions SET 
                status = @Status,
                completed_at = @EndTime,
                total_rounds = @TotalRounds,
                total_messages = @TotalMessages,
                conclusion = @Conclusion
            WHERE id = @Id
            RETURNING id, collaboration_id, task_id, orchestration_mode, status, topic, metadata, started_at as start_time, completed_at as end_time, total_rounds, total_messages, conclusion, created_at";
        return await connection.QueryFirstAsync<CoordinationSession>(sql, session);
    }

    public async Task<bool> EndSessionAsync(long id, string? conclusion = null)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE workflow_sessions SET 
                status = 'completed',
                completed_at = @EndTime,
                conclusion = @Conclusion
            WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id, EndTime = DateTime.UtcNow, Conclusion = conclusion });
        return rows > 0;
    }
}

public class CoordinationRoundRepository : ICoordinationRoundRepository
{
    private readonly IDapperContext _context;

    public CoordinationRoundRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<CoordinationRound?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT id, session_id, round_number, from_agent_id as speaker_agent_id, from_agent_name as speaker_name, from_agent_role as speaker_role, content as message_content, thinking_process, selected_next_speaker, selection_reason, created_at FROM messages WHERE id = @Id AND message_type = 'coordination'";
        return await connection.QueryFirstOrDefaultAsync<CoordinationRound>(sql, new { Id = id });
    }

    public async Task<List<CoordinationRound>> GetBySessionIdAsync(long sessionId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT id, session_id, round_number, from_agent_id as speaker_agent_id, from_agent_name as speaker_name, from_agent_role as speaker_role, content as message_content, thinking_process, selected_next_speaker, selection_reason, created_at FROM messages WHERE session_id = @SessionId AND message_type = 'coordination' ORDER BY round_number";
        var result = await connection.QueryAsync<CoordinationRound>(sql, new { SessionId = sessionId });
        return result.ToList();
    }

    public async Task<CoordinationRound> CreateAsync(CoordinationRound round)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            INSERT INTO messages (session_id, message_type, round_number, from_agent_id, from_agent_name, from_agent_role, content, thinking_process, selected_next_speaker, selection_reason, created_at)
            VALUES (@SessionId, 'coordination', @RoundNumber, @SpeakerAgentId, @SpeakerName, @SpeakerRole, @MessageContent, @ThinkingProcess, @SelectedNextSpeaker, @SelectionReason, @CreatedAt)
            RETURNING id, session_id, round_number, from_agent_id as speaker_agent_id, from_agent_name as speaker_name, from_agent_role as speaker_role, content as message_content, thinking_process, selected_next_speaker, selection_reason, created_at";
        return await connection.QueryFirstAsync<CoordinationRound>(sql, round);
    }

    public async Task<int> GetRoundCountAsync(long sessionId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT COUNT(*) FROM messages WHERE session_id = @SessionId AND message_type = 'coordination'";
        return await connection.ExecuteScalarAsync<int>(sql, new { SessionId = sessionId });
    }
}

public class CoordinationParticipantRepository : ICoordinationParticipantRepository
{
    private readonly IDapperContext _context;

    public CoordinationParticipantRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<CoordinationParticipant?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            SELECT id, session_id, agent_id, agent_name, agent_role, is_manager, speak_count, total_tokens, joined_at 
            FROM coordination_participants 
            WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<CoordinationParticipant>(sql, new { Id = id });
    }

    public async Task<List<CoordinationParticipant>> GetBySessionIdAsync(long sessionId)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            SELECT id, session_id, agent_id, agent_name, agent_role, is_manager, speak_count, total_tokens, joined_at 
            FROM coordination_participants 
            WHERE session_id = @SessionId 
            ORDER BY is_manager DESC, agent_name";
        var result = await connection.QueryAsync<CoordinationParticipant>(sql, new { SessionId = sessionId });
        return result.ToList();
    }

    public async Task<CoordinationParticipant?> GetBySessionAndAgentAsync(long sessionId, long agentId)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            SELECT id, session_id, agent_id, agent_name, agent_role, is_manager, speak_count, total_tokens, joined_at 
            FROM coordination_participants 
            WHERE session_id = @SessionId AND agent_id = @AgentId";
        return await connection.QueryFirstOrDefaultAsync<CoordinationParticipant>(sql, new { SessionId = sessionId, AgentId = agentId });
    }

    public async Task<CoordinationParticipant> CreateAsync(CoordinationParticipant participant)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            INSERT INTO coordination_participants (session_id, agent_id, agent_name, agent_role, is_manager)
            VALUES (@SessionId, @AgentId, @AgentName, @AgentRole, @IsManager)
            RETURNING id, session_id, agent_id, agent_name, agent_role, is_manager, speak_count, total_tokens, joined_at";
        return await connection.QueryFirstAsync<CoordinationParticipant>(sql, participant);
    }

    public async Task<bool> IncrementSpeakCountAsync(long sessionId, long agentId)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE coordination_participants SET speak_count = speak_count + 1
            WHERE session_id = @SessionId AND agent_id = @AgentId";
        var rows = await connection.ExecuteAsync(sql, new { SessionId = sessionId, AgentId = agentId });
        return rows > 0;
    }

    public async Task<bool> UpdateTokenCountAsync(long sessionId, long agentId, int tokens)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE coordination_participants SET total_tokens = total_tokens + @Tokens
            WHERE session_id = @SessionId AND agent_id = @AgentId";
        var rows = await connection.ExecuteAsync(sql, new { SessionId = sessionId, AgentId = agentId, Tokens = tokens });
        return rows > 0;
    }
}
