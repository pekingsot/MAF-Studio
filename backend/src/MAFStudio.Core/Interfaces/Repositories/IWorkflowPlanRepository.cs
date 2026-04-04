using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface IWorkflowPlanRepository
{
    Task<WorkflowPlan?> GetByIdAsync(long id);
    Task<List<WorkflowPlan>> GetByCollaborationIdAsync(long collaborationId);
    Task<List<WorkflowPlan>> GetPendingPlansAsync(long collaborationId);
    Task<long> CreateAsync(WorkflowPlan plan);
    Task<bool> UpdateAsync(WorkflowPlan plan);
    Task<bool> DeleteAsync(long id);
    Task<bool> UpdateStatusAsync(long id, string status, long? approvedBy = null);
}
