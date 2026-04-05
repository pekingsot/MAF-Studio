using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface IWorkflowSessionRepository
{
    Task<WorkflowSession?> GetByIdAsync(long id);
    Task<WorkflowSession?> GetActiveByCollaborationIdAsync(long collaborationId);
    Task<List<WorkflowSession>> GetByCollaborationIdAsync(long collaborationId, int limit = 20);
    Task<List<WorkflowSession>> GetByTaskIdAsync(long taskId, int limit = 20);
    Task<WorkflowSession> CreateAsync(WorkflowSession session);
    Task<WorkflowSession> UpdateAsync(WorkflowSession session);
    Task<bool> EndSessionAsync(long id, string? conclusion = null, string? errorMessage = null);
    Task<bool> IncrementMessageCountAsync(long id);
}

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(long id);
    Task<List<Message>> GetBySessionIdAsync(long sessionId, int limit = 1000);
    Task<List<Message>> GetByCollaborationIdAsync(long collaborationId, int limit = 100);
    Task<List<Message>> GetByTaskIdAsync(long taskId, int limit = 100);
    Task<Message> CreateAsync(Message message);
    Task<int> GetMessageCountBySessionAsync(long sessionId);
}
