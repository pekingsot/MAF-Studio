using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;

namespace MAFStudio.Application.Services;

public class CollaborationService : ICollaborationService
{
    private readonly ICollaborationRepository _collaborationRepository;
    private readonly ICollaborationAgentRepository _collaborationAgentRepository;
    private readonly ICollaborationTaskRepository _collaborationTaskRepository;

    public CollaborationService(
        ICollaborationRepository collaborationRepository,
        ICollaborationAgentRepository collaborationAgentRepository,
        ICollaborationTaskRepository collaborationTaskRepository)
    {
        _collaborationRepository = collaborationRepository;
        _collaborationAgentRepository = collaborationAgentRepository;
        _collaborationTaskRepository = collaborationTaskRepository;
    }

    public async Task<List<Collaboration>> GetByUserIdAsync(long userId)
    {
        return await _collaborationRepository.GetByUserIdAsync(userId);
    }

    public async Task<Collaboration?> GetByIdAsync(long id, long userId)
    {
        var collaboration = await _collaborationRepository.GetByIdAsync(id);
        if (collaboration == null || collaboration.UserId != userId) return null;
        return collaboration;
    }

    public async Task<Collaboration> CreateAsync(
        string name, 
        string? description, 
        string? path, 
        string? gitRepositoryUrl, 
        string? gitBranch, 
        string? gitUsername, 
        string? gitEmail, 
        string? gitAccessToken, 
        long userId)
    {
        var collaboration = new Collaboration
        {
            Name = name,
            Description = description,
            Path = path,
            GitRepositoryUrl = gitRepositoryUrl,
            GitBranch = gitBranch,
            GitUsername = gitUsername,
            GitEmail = gitEmail,
            GitAccessToken = gitAccessToken,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _collaborationRepository.CreateAsync(collaboration);
    }

    public async Task<Collaboration> UpdateAsync(Collaboration collaboration)
    {
        collaboration.UpdatedAt = DateTime.UtcNow;
        return await _collaborationRepository.UpdateAsync(collaboration);
    }

    public async Task<bool> DeleteAsync(long id, long userId)
    {
        var collaboration = await GetByIdAsync(id, userId);
        if (collaboration == null) return false;
        
        await _collaborationRepository.DeleteAsync(id);
        return true;
    }

    public async Task<bool> AddAgentAsync(long collaborationId, long agentId, string? role, long userId)
    {
        var collaboration = await GetByIdAsync(collaborationId, userId);
        if (collaboration == null) return false;

        return await _collaborationRepository.AddAgentAsync(collaborationId, agentId, role);
    }

    public async Task<bool> RemoveAgentAsync(long collaborationId, long agentId, long userId)
    {
        var collaboration = await GetByIdAsync(collaborationId, userId);
        if (collaboration == null) return false;

        return await _collaborationRepository.RemoveAgentAsync(collaborationId, agentId);
    }

    public async Task<List<CollaborationAgent>> GetAgentsAsync(long collaborationId)
    {
        return await _collaborationAgentRepository.GetByCollaborationIdAsync(collaborationId);
    }

    public async Task<CollaborationTask> CreateTaskAsync(long collaborationId, string title, string? description, long userId)
    {
        var collaboration = await GetByIdAsync(collaborationId, userId);
        if (collaboration == null)
            throw new NotFoundException($"协作 {collaborationId} 不存在");

        var task = new CollaborationTask
        {
            CollaborationId = collaborationId,
            Title = title,
            Description = description,
            Status = CollaborationTaskStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        return await _collaborationTaskRepository.CreateAsync(task);
    }

    public async Task<CollaborationTask> UpdateTaskStatusAsync(long taskId, CollaborationTaskStatus status, long userId)
    {
        var task = await _collaborationTaskRepository.GetByIdAsync(taskId);
        if (task == null)
            throw new NotFoundException($"任务 {taskId} 不存在");

        await _collaborationTaskRepository.UpdateStatusAsync(taskId, status);

        return await _collaborationTaskRepository.GetByIdAsync(taskId) ?? task;
    }

    public async Task<bool> DeleteTaskAsync(long taskId, long userId)
    {
        var task = await _collaborationTaskRepository.GetByIdAsync(taskId);
        if (task == null) return false;

        await _collaborationTaskRepository.DeleteAsync(taskId);
        return true;
    }
}
