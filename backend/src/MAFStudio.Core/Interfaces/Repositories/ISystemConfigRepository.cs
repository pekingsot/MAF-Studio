using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ISystemConfigRepository
{
    Task<SystemConfig?> GetByIdAsync(long id);
    Task<SystemConfig?> GetByKeyAsync(string key);
    Task<List<SystemConfig>> GetAllAsync();
    Task<SystemConfig> CreateAsync(SystemConfig config);
    Task<SystemConfig> UpdateAsync(SystemConfig config);
    Task<bool> DeleteAsync(long id);
}
