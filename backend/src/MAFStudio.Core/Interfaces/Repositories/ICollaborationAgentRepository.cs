using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ICollaborationAgentRepository
{
    Task<List<CollaborationAgent>> GetByCollaborationIdAsync(Guid collaborationId);
    Task<List<CollaborationAgent>> GetByAgentIdAsync(Guid agentId);
    Task<CollaborationAgent?> GetByIdAsync(Guid id);
    Task<CollaborationAgent> CreateAsync(CollaborationAgent collaborationAgent);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> DeleteByCollaborationAndAgentAsync(Guid collaborationId, Guid agentId);
}
