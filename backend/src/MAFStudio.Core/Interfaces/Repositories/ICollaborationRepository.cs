using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ICollaborationRepository
{
    Task<Collaboration?> GetByIdAsync(Guid id);
    Task<List<Collaboration>> GetByUserIdAsync(string userId);
    Task<Collaboration> CreateAsync(Collaboration collaboration);
    Task<Collaboration> UpdateAsync(Collaboration collaboration);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> AddAgentAsync(Guid collaborationId, Guid agentId, string? role);
    Task<bool> RemoveAgentAsync(Guid collaborationId, Guid agentId);
    Task<List<CollaborationAgent>> GetAgentsAsync(Guid collaborationId);
}
