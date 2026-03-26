using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Services;

public interface ILlmConfigService
{
    Task<List<LlmConfig>> GetByUserIdAsync(string userId);
    Task<List<LlmConfig>> GetAllAsync();
    Task<LlmConfig?> GetByIdAsync(Guid id);
    Task<LlmConfig> CreateAsync(string name, string provider, string? apiKey, string? endpoint, string? defaultModel, string? extraConfig, string userId);
    Task<LlmConfig> UpdateAsync(Guid id, string name, string? apiKey, string? endpoint, string? defaultModel, string? extraConfig);
    Task<bool> DeleteAsync(Guid id);
}
