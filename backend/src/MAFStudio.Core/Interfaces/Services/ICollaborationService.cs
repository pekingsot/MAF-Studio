using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Core.Interfaces.Services;

public interface ICollaborationService
{
    Task<List<Collaboration>> GetByUserIdAsync(long userId);
    Task<Collaboration?> GetByIdAsync(long id, long userId);
    Task<Collaboration?> GetByIdAsync(long id);
    Task<Collaboration> CreateAsync(string name, string? description, string? path, string? gitRepositoryUrl, string? gitBranch, string? gitUsername, string? gitEmail, string? gitAccessToken, long userId);
    Task<Collaboration> UpdateAsync(Collaboration collaboration);
    Task<bool> DeleteAsync(long id, long userId);
    Task<bool> AddAgentAsync(long collaborationId, long agentId, string? role, string? customPrompt, long userId);
    Task<bool> RemoveAgentAsync(long collaborationId, long agentId, long userId);
    Task<bool> UpdateAgentRoleAsync(long collaborationId, long agentId, string role, string? customPrompt, long userId);
    Task<List<CollaborationAgent>> GetAgentsAsync(long collaborationId);
    Task<List<CollaborationAgentWithDetails>> GetAgentsWithDetailsAsync(long collaborationId);
    Task<List<CollaborationTask>> GetTasksAsync(long collaborationId);
    Task<CollaborationTask?> GetTaskByIdAsync(long taskId);
    Task<CollaborationTask> CreateTaskAsync(long collaborationId, string title, string? description, long userId, string? gitUrl = null, string? gitBranch = null, string? gitToken = null, List<long>? agentIds = null);
    Task<CollaborationTask> UpdateTaskAsync(long taskId, string title, string? description, string? gitUrl = null, string? gitBranch = null, string? gitToken = null, List<long>? agentIds = null);
    Task<CollaborationTask> UpdateTaskStatusAsync(long taskId, CollaborationTaskStatus status, long userId);
    Task<bool> DeleteTaskAsync(long taskId, long userId);
}
