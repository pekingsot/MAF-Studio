using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ICollaborationAgentRepository
{
    Task<List<CollaborationAgent>> GetByCollaborationIdAsync(long collaborationId);
    Task<List<CollaborationAgent>> GetByAgentIdAsync(long agentId);
    Task<CollaborationAgent?> GetByIdAsync(long id);
    Task<CollaborationAgent> CreateAsync(CollaborationAgent collaborationAgent);
    Task<bool> DeleteAsync(long id);
    Task<bool> DeleteByCollaborationAndAgentAsync(long collaborationId, long agentId);
}
