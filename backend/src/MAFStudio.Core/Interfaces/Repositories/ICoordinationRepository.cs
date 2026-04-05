using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ICoordinationSessionRepository
{
    Task<CoordinationSession?> GetByIdAsync(long id);
    Task<CoordinationSession?> GetActiveByCollaborationIdAsync(long collaborationId);
    Task<List<CoordinationSession>> GetByCollaborationIdAsync(long collaborationId, int limit = 20);
    Task<List<CoordinationSession>> GetByTaskIdAsync(long taskId, int limit = 20);
    Task<CoordinationSession> CreateAsync(CoordinationSession session);
    Task<CoordinationSession> UpdateAsync(CoordinationSession session);
    Task<bool> EndSessionAsync(long id, string? conclusion = null);
}

public interface ICoordinationRoundRepository
{
    Task<CoordinationRound?> GetByIdAsync(long id);
    Task<List<CoordinationRound>> GetBySessionIdAsync(long sessionId);
    Task<CoordinationRound> CreateAsync(CoordinationRound round);
    Task<int> GetRoundCountAsync(long sessionId);
}

public interface ICoordinationParticipantRepository
{
    Task<CoordinationParticipant?> GetByIdAsync(long id);
    Task<List<CoordinationParticipant>> GetBySessionIdAsync(long sessionId);
    Task<CoordinationParticipant?> GetBySessionAndAgentAsync(long sessionId, long agentId);
    Task<CoordinationParticipant> CreateAsync(CoordinationParticipant participant);
    Task<bool> IncrementSpeakCountAsync(long sessionId, long agentId);
    Task<bool> UpdateTokenCountAsync(long sessionId, long agentId, int tokens);
}
