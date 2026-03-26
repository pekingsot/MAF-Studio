using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;

namespace MAFStudio.Core.Interfaces.Services;

public interface ICollaborationService
{
    Task<List<Collaboration>> GetByUserIdAsync(string userId);
    Task<Collaboration?> GetByIdAsync(Guid id, string userId);
    Task<Collaboration> CreateAsync(string name, string? description, string? path, string? gitRepositoryUrl, string? gitBranch, string? gitUsername, string? gitEmail, string? gitAccessToken, string userId);
    Task<Collaboration> UpdateAsync(Collaboration collaboration);
    Task<bool> DeleteAsync(Guid id, string userId);
    Task<bool> AddAgentAsync(Guid collaborationId, Guid agentId, string? role, string userId);
    Task<bool> RemoveAgentAsync(Guid collaborationId, Guid agentId, string userId);
    Task<List<CollaborationAgent>> GetAgentsAsync(Guid collaborationId);
    Task<CollaborationTask> CreateTaskAsync(Guid collaborationId, string title, string? description, string userId);
    Task<CollaborationTask> UpdateTaskStatusAsync(Guid taskId, CollaborationTaskStatus status, string userId);
    Task<bool> DeleteTaskAsync(Guid taskId, string userId);
}
