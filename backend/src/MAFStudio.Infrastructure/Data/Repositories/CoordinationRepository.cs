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
        const string sql = "SELECT * FROM coordination_sessions WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<CoordinationSession>(sql, new { Id = id });
    }

    public async Task<CoordinationSession?> GetActiveByCollaborationIdAsync(long collaborationId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM coordination_sessions WHERE collaboration_id = @CollaborationId AND status = 'running'";
        return await connection.QueryFirstOrDefaultAsync<CoordinationSession>(sql, new { CollaborationId = collaborationId });
    }

    public async Task<List<CoordinationSession>> GetByCollaborationIdAsync(long collaborationId, int limit = 20)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM coordination_sessions WHERE collaboration_id = @CollaborationId ORDER BY start_time DESC LIMIT @Limit";
        var result = await connection.QueryAsync<CoordinationSession>(sql, new { CollaborationId = collaborationId, Limit = limit });
        return result.ToList();
    }

    public async Task<List<CoordinationSession>> GetByTaskIdAsync(long taskId, int limit = 20)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM coordination_sessions WHERE task_id = @TaskId ORDER BY start_time DESC LIMIT @Limit";
        var result = await connection.QueryAsync<CoordinationSession>(sql, new { TaskId = taskId, Limit = limit });
        return result.ToList();
    }

    public async Task<CoordinationSession> CreateAsync(CoordinationSession session)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            INSERT INTO coordination_sessions (collaboration_id, task_id, workflow_execution_id, orchestration_mode, status, topic, metadata, start_time)
            VALUES (@CollaborationId, @TaskId, @WorkflowExecutionId, @OrchestrationMode, @Status, @Topic, @Metadata, @StartTime)
            RETURNING *";
        return await connection.QueryFirstAsync<CoordinationSession>(sql, session);
    }

    public async Task<CoordinationSession> UpdateAsync(CoordinationSession session)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE coordination_sessions SET 
                status = @Status,
                end_time = @EndTime,
                total_rounds = @TotalRounds,
                total_messages = @TotalMessages,
                conclusion = @Conclusion
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<CoordinationSession>(sql, session);
    }

    public async Task<bool> EndSessionAsync(long id, string? conclusion = null)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE coordination_sessions SET 
                status = 'completed',
                end_time = @EndTime,
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
        const string sql = "SELECT * FROM coordination_rounds WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<CoordinationRound>(sql, new { Id = id });
    }

    public async Task<List<CoordinationRound>> GetBySessionIdAsync(long sessionId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM coordination_rounds WHERE session_id = @SessionId ORDER BY round_number";
        var result = await connection.QueryAsync<CoordinationRound>(sql, new { SessionId = sessionId });
        return result.ToList();
    }

    public async Task<CoordinationRound> CreateAsync(CoordinationRound round)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            INSERT INTO coordination_rounds (session_id, round_number, speaker_agent_id, speaker_name, speaker_role, message_content, message_id, thinking_process, selected_next_speaker, selection_reason)
            VALUES (@SessionId, @RoundNumber, @SpeakerAgentId, @SpeakerName, @SpeakerRole, @MessageContent, @MessageId, @ThinkingProcess, @SelectedNextSpeaker, @SelectionReason)
            RETURNING *";
        return await connection.QueryFirstAsync<CoordinationRound>(sql, round);
    }

    public async Task<int> GetRoundCountAsync(long sessionId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT COUNT(*) FROM coordination_rounds WHERE session_id = @SessionId";
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
        const string sql = "SELECT * FROM coordination_participants WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<CoordinationParticipant>(sql, new { Id = id });
    }

    public async Task<List<CoordinationParticipant>> GetBySessionIdAsync(long sessionId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM coordination_participants WHERE session_id = @SessionId ORDER BY is_manager DESC, agent_name";
        var result = await connection.QueryAsync<CoordinationParticipant>(sql, new { SessionId = sessionId });
        return result.ToList();
    }

    public async Task<CoordinationParticipant?> GetBySessionAndAgentAsync(long sessionId, long agentId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM coordination_participants WHERE session_id = @SessionId AND agent_id = @AgentId";
        return await connection.QueryFirstOrDefaultAsync<CoordinationParticipant>(sql, new { SessionId = sessionId, AgentId = agentId });
    }

    public async Task<CoordinationParticipant> CreateAsync(CoordinationParticipant participant)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            INSERT INTO coordination_participants (session_id, agent_id, agent_name, agent_role, is_manager)
            VALUES (@SessionId, @AgentId, @AgentName, @AgentRole, @IsManager)
            RETURNING *";
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
