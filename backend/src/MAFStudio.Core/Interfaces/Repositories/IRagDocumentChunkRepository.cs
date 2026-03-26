using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface IRagDocumentChunkRepository
{
    Task<RagDocumentChunk?> GetByIdAsync(long id);
    Task<List<RagDocumentChunk>> GetByDocumentIdAsync(long documentId);
    Task<RagDocumentChunk> CreateAsync(RagDocumentChunk chunk);
    Task<List<RagDocumentChunk>> CreateBatchAsync(List<RagDocumentChunk> chunks);
    Task<bool> DeleteByDocumentIdAsync(long documentId);
}
