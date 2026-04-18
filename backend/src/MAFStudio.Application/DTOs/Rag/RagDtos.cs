namespace MAFStudio.Application.DTOs.Rag;

public class RagQueryDto
{
    public string Query { get; set; } = string.Empty;
    public int? TopK { get; set; }
    public double? ScoreThreshold { get; set; }
    public long? LlmConfigId { get; set; }
    public long? LlmModelConfigId { get; set; }
    public string? SystemPrompt { get; set; }
}

public class RagTestSplitDto
{
    public string Content { get; set; } = string.Empty;
    public string? SplitMethod { get; set; }
    public int? ChunkSize { get; set; }
    public int? ChunkOverlap { get; set; }
}

public class RagCreateDocumentDto
{
    public string? FileName { get; set; }
    public string? Content { get; set; }
    public string? FileType { get; set; }
    public string? SplitMethod { get; set; }
    public int? ChunkSize { get; set; }
    public int? ChunkOverlap { get; set; }
}
