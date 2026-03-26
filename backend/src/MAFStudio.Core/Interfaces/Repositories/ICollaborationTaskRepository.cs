using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ICollaborationTaskRepository
{
    Task<CollaborationTask?> GetByIdAsync(Guid id);
    Task<List<CollaborationTask>> GetByCollaborationIdAsync(Guid collaborationId);
    Task<CollaborationTask> CreateAsync(CollaborationTask task);
    Task<CollaborationTask> UpdateAsync(CollaborationTask task);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> UpdateStatusAsync(Guid id, Enums.CollaborationTaskStatus status);
}
