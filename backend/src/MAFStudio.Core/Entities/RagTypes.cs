namespace MAFStudio.Core.Entities;

public class TextChunk
{
    public int Index { get; set; }
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class VectorizeResult
{
    public int SuccessCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RagQueryResult
{
    public string? Answer { get; set; }
    public List<RagSource> Sources { get; set; } = new();
}

public class RagSource
{
    public string Content { get; set; } = string.Empty;
    public double Score { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
}

public class VectorDocsResult
{
    public long Total { get; set; }
    public List<VectorDocItem> Documents { get; set; } = new();
}

public class VectorDocItem
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Score { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public string FileName { get; set; } = string.Empty;
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

public class SplitMethodInfo
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class FileTypeInfo
{
    public string Extension { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
