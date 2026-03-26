using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface IOperationLogRepository
{
    Task<OperationLog?> GetByIdAsync(long id);
    Task<List<OperationLog>> GetByUserIdAsync(string userId, int limit = 100);
    Task<List<OperationLog>> GetAllAsync(int limit = 100);
    Task<OperationLog> CreateAsync(OperationLog log);
}
