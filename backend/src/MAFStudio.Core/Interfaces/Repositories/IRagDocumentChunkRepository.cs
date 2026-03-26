using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface IRagDocumentChunkRepository
{
    Task<RagDocumentChunk?> GetByIdAsync(Guid id);
    Task<List<RagDocumentChunk>> GetByDocumentIdAsync(Guid documentId);
    Task<RagDocumentChunk> CreateAsync(RagDocumentChunk chunk);
    Task<List<RagDocumentChunk>> CreateBatchAsync(List<RagDocumentChunk> chunks);
    Task<bool> DeleteByDocumentIdAsync(Guid documentId);
}
