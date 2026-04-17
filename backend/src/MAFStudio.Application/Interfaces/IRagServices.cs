namespace MAFStudio.Application.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text);
    Task<List<float[]>> EmbedBatchAsync(List<string> texts);
    int EmbeddingDimension { get; }
}

public interface IRerankService
{
    Task<List<RerankResult>> RerankAsync(string query, List<string> documents, int topK = 5);
}

public interface IVectorStoreService
{
    Task EnsureCollectionExistsAsync(string collectionName, int vectorSize);
    Task UpsertAsync(string collectionName, string id, float[] vector, Dictionary<string, string> metadata, string content);
    Task DeleteAsync(string collectionName, string id);
    Task<List<VectorSearchResult>> SearchAsync(string collectionName, float[] queryVector, int topK = 5, float scoreThreshold = 0.5f);
    Task<List<VectorSearchResult>> SearchWithKeywordAsync(string collectionName, string keyword, int page = 1, int pageSize = 10);
    Task<long> CountAsync(string collectionName);
}

public interface ITextSplitterService
{
    List<TextChunk> Split(string text, string? method = null, int? chunkSize = null, int? chunkOverlap = null);
}

public class RerankResult
{
    public int Index { get; set; }
    public string Text { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
}

public class VectorSearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Score { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class TextChunk
{
    public int Index { get; set; }
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}
