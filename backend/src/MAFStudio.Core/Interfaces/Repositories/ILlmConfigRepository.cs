using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ILlmConfigRepository
{
    Task<LlmConfig?> GetByIdAsync(long id);
    Task<List<LlmConfig>> GetByUserIdAsync(string userId);
    Task<List<LlmConfig>> GetAllAsync();
    Task<LlmConfig> CreateAsync(LlmConfig config);
    Task<LlmConfig> UpdateAsync(LlmConfig config);
    Task<bool> DeleteAsync(long id);
    Task SetDefaultAsync(long id, string userId);
}
