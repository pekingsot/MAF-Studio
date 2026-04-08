using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ICollaborationRepository
{
    Task<Collaboration?> GetByIdAsync(long id);
    Task<List<Collaboration>> GetByUserIdAsync(long userId);
    Task<Collaboration> CreateAsync(Collaboration collaboration);
    Task<Collaboration> UpdateAsync(Collaboration collaboration);
    Task<bool> DeleteAsync(long id);
    Task<bool> AddAgentAsync(long collaborationId, long agentId, string? role, string? customPrompt);
    Task<bool> RemoveAgentAsync(long collaborationId, long agentId);
    Task<bool> UpdateAgentRoleAsync(long collaborationId, long agentId, string role, string? customPrompt);
    Task<List<CollaborationAgent>> GetAgentsAsync(long collaborationId);
}
