namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("rag_document_chunks")]
public class RagDocumentChunk
{
    [Dapper.Contrib.Extensions.Key]
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
