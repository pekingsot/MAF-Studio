using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Services;

public interface IRagService
{
    Task<RagDocument> UploadDocumentAsync(string fileName, string? filePath, string? fileType, long fileSize, long userId, string? splitMethod = null, int? chunkSize = null, int? chunkOverlap = null);
    Task<List<RagDocument>> GetDocumentsAsync(long userId);
    Task<RagDocument?> GetDocumentAsync(long id);
    Task<bool> DeleteDocumentAsync(long id);
    Task<List<RagDocumentChunk>> GetChunksAsync(long documentId);
    Task<List<TextChunk>> TestSplitAsync(string content, string? method = null, int? chunkSize = null, int? chunkOverlap = null);
    Task<VectorizeResult> VectorizeAsync(long documentId);
    Task<RagQueryResult> QueryAsync(string query, int topK = 5, double scoreThreshold = 0.5, long? llmConfigId = null, string? systemPrompt = null);
    Task<VectorDocsResult> GetVectorDocsAsync(int page = 1, int pageSize = 10, string? keyword = null);
    Task<bool> DeleteVectorDocAsync(string id);
    List<SplitMethodInfo> GetSplitMethods();
    List<FileTypeInfo> GetFileTypes();
}
