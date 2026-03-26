using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface IRagDocumentRepository
{
    Task<RagDocument?> GetByIdAsync(long id);
    Task<List<RagDocument>> GetByUserIdAsync(string userId);
    Task<RagDocument> CreateAsync(RagDocument document);
    Task<RagDocument> UpdateAsync(RagDocument document);
    Task<bool> DeleteAsync(long id);
}
