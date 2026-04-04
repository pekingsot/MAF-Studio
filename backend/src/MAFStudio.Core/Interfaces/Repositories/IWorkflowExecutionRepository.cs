using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface IWorkflowExecutionRepository
{
    Task<WorkflowExecution> CreateAsync(WorkflowExecution execution);
    Task<WorkflowExecution?> GetByIdAsync(long id);
    Task<List<WorkflowExecution>> GetByCollaborationIdAsync(long collaborationId);
    Task<List<WorkflowExecution>> GetByStatusAsync(string status);
    Task UpdateAsync(WorkflowExecution execution);
    Task AddMessageAsync(WorkflowExecutionMessage message);
    Task<List<WorkflowExecutionMessage>> GetMessagesAsync(long executionId);
}
