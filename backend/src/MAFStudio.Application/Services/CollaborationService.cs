using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;

namespace MAFStudio.Application.Services;

public class CollaborationService : ICollaborationService
{
    private readonly ICollaborationRepository _collaborationRepository;
    private readonly ICollaborationTaskRepository _taskRepository;

    public CollaborationService(ICollaborationRepository collaborationRepository, ICollaborationTaskRepository taskRepository)
    {
        _collaborationRepository = collaborationRepository;
        _taskRepository = taskRepository;
    }

    public async Task<List<Collaboration>> GetByUserIdAsync(string userId)
    {
        return await _collaborationRepository.GetByUserIdAsync(userId);
    }

    public async Task<Collaboration?> GetByIdAsync(long id, string userId)
    {
        var collaboration = await _collaborationRepository.GetByIdAsync(id);
        if (collaboration == null || collaboration.UserId != userId)
        {
            return null;
        }
        return collaboration;
    }

    public async Task<Collaboration> CreateAsync(string name, string? description, string? path, string? gitRepositoryUrl, string? gitBranch, string? gitUsername, string? gitEmail, string? gitAccessToken, string userId)
    {
        var collaboration = new Collaboration
        {
            Name = name,
            Description = description,
            Path = path,
            Status = CollaborationStatus.Active,
            UserId = userId,
            GitRepositoryUrl = gitRepositoryUrl,
            GitBranch = gitBranch,
            GitUsername = gitUsername,
            GitEmail = gitEmail,
            GitAccessToken = gitAccessToken,
        };

        return await _collaborationRepository.CreateAsync(collaboration);
    }

    public async Task<Collaboration> UpdateAsync(Collaboration collaboration)
    {
        collaboration.UpdatedAt = DateTime.UtcNow;
        return await _collaborationRepository.UpdateAsync(collaboration);
    }

    public async Task<bool> DeleteAsync(long id, string userId)
    {
        var collaboration = await _collaborationRepository.GetByIdAsync(id);
        if (collaboration == null || collaboration.UserId != userId)
        {
            return false;
        }
        return await _collaborationRepository.DeleteAsync(id);
    }

    public async Task<bool> AddAgentAsync(long collaborationId, long agentId, string? role, string userId)
    {
        var collaboration = await _collaborationRepository.GetByIdAsync(collaborationId);
        if (collaboration == null || collaboration.UserId != userId)
        {
            return false;
        }
        return await _collaborationRepository.AddAgentAsync(collaborationId, agentId, role);
    }

    public async Task<bool> RemoveAgentAsync(long collaborationId, long agentId, string userId)
    {
        var collaboration = await _collaborationRepository.GetByIdAsync(collaborationId);
        if (collaboration == null || collaboration.UserId != userId)
        {
            return false;
        }
        return await _collaborationRepository.RemoveAgentAsync(collaborationId, agentId);
    }

    public async Task<List<CollaborationAgent>> GetAgentsAsync(long collaborationId)
    {
        return await _collaborationRepository.GetAgentsAsync(collaborationId);
    }

    public async Task<CollaborationTask> CreateTaskAsync(long collaborationId, string title, string? description, string userId)
    {
        var collaboration = await _collaborationRepository.GetByIdAsync(collaborationId);
        if (collaboration == null || collaboration.UserId != userId)
        {
            throw new UnauthorizedAccessException("Collaboration not found or access denied");
        }

        var task = new CollaborationTask
        {
            CollaborationId = collaborationId,
            Title = title,
            Description = description,
            Status = CollaborationTaskStatus.Pending,
        };

        return await _taskRepository.CreateAsync(task);
    }

    public async Task<CollaborationTask> UpdateTaskStatusAsync(long taskId, CollaborationTaskStatus status, string userId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
        {
            throw new InvalidOperationException($"Task with id {taskId} not found");
        }

        var collaboration = await _collaborationRepository.GetByIdAsync(task.CollaborationId);
        if (collaboration == null || collaboration.UserId != userId)
        {
            throw new UnauthorizedAccessException("Access denied");
        }

        task.Status = status;
        if (status == CollaborationTaskStatus.Completed)
        {
            task.CompletedAt = DateTime.UtcNow;
        }

        return await _taskRepository.UpdateAsync(task);
    }

    public async Task<bool> DeleteTaskAsync(long taskId, string userId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
        {
            return false;
        }

        var collaboration = await _collaborationRepository.GetByIdAsync(task.CollaborationId);
        if (collaboration == null || collaboration.UserId != userId)
        {
            return false;
        }

        return await _taskRepository.DeleteAsync(taskId);
    }
}
