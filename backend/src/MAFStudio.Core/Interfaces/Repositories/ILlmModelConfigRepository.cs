using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ILlmModelConfigRepository
{
    Task<LlmModelConfig?> GetByIdAsync(Guid id);
    Task<List<LlmModelConfig>> GetByLlmConfigIdAsync(Guid llmConfigId);
    Task<LlmModelConfig?> GetDefaultByLlmConfigIdAsync(Guid llmConfigId);
    Task<LlmModelConfig> CreateAsync(LlmModelConfig config);
    Task<LlmModelConfig> UpdateAsync(LlmModelConfig config);
    Task<bool> DeleteAsync(Guid id);
}
