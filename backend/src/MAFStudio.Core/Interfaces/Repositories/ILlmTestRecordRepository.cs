using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ILlmTestRecordRepository
{
    Task<LlmTestRecord?> GetByIdAsync(long id);
    Task<List<LlmTestRecord>> GetByLlmConfigIdAsync(long llmConfigId, int limit = 50);
    Task<LlmTestRecord> CreateAsync(LlmTestRecord record);
    Task<bool> DeleteAsync(long id);
}
