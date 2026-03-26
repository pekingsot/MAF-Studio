using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ICollaborationTaskRepository
{
    Task<CollaborationTask?> GetByIdAsync(long id);
    Task<List<CollaborationTask>> GetByCollaborationIdAsync(long collaborationId);
    Task<CollaborationTask> CreateAsync(CollaborationTask task);
    Task<CollaborationTask> UpdateAsync(CollaborationTask task);
    Task<bool> DeleteAsync(long id);
    Task<bool> UpdateStatusAsync(long id, Enums.CollaborationTaskStatus status);
}
