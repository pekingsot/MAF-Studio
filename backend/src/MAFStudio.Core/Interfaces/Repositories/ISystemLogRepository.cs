using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ISystemLogRepository
{
    Task<SystemLog?> GetByIdAsync(long id);
    Task<List<SystemLog>> GetAsync(string? level = null, string? category = null, long? userId = null, int limit = 100);
    Task<SystemLog> CreateAsync(SystemLog log);
    Task<bool> DeleteAsync(long id);
}
