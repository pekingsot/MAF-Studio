using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ITaskAgentRepository
{
    Task<TaskAgent?> GetByIdAsync(long id);
    Task<List<TaskAgent>> GetByTaskIdAsync(long taskId);
    Task<List<TaskAgent>> GetByAgentIdAsync(long agentId);
    Task<TaskAgent> CreateAsync(TaskAgent taskAgent);
    Task<List<TaskAgent>> CreateBatchAsync(List<TaskAgent> taskAgents);
    Task<bool> DeleteByTaskIdAsync(long taskId);
    Task<bool> DeleteAsync(long taskId, long agentId);
}
