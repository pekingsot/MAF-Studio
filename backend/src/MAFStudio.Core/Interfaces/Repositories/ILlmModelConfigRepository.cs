using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ILlmModelConfigRepository
{
    Task<LlmModelConfig?> GetByIdAsync(long id);
    Task<List<LlmModelConfig>> GetByLlmConfigIdAsync(long llmConfigId);
    Task<LlmModelConfig> CreateAsync(LlmModelConfig config);
    Task<LlmModelConfig> UpdateAsync(LlmModelConfig config);
    Task<bool> DeleteAsync(long id);
    Task SetDefaultAsync(long llmConfigId, long modelId);
}
