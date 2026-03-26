using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface IRagDocumentRepository
{
    Task<RagDocument?> GetByIdAsync(Guid id);
    Task<List<RagDocument>> GetByUserIdAsync(string userId);
    Task<RagDocument> CreateAsync(RagDocument document);
    Task<RagDocument> UpdateAsync(RagDocument document);
    Task<bool> DeleteAsync(Guid id);
}
