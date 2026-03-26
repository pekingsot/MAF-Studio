using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ILlmTestRecordRepository
{
    Task<LlmTestRecord?> GetByIdAsync(Guid id);
    Task<List<LlmTestRecord>> GetByLlmConfigIdAsync(Guid llmConfigId, int limit = 50);
    Task<LlmTestRecord> CreateAsync(LlmTestRecord record);
    Task<bool> DeleteAsync(Guid id);
}
