using MAFStudio.Core.Enums;

namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("rag_documents")]
public class RagDocument
{
    [Dapper.Contrib.Extensions.Key]
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string? FilePath { get; set; }

    public string? FileType { get; set; }

    public long FileSize { get; set; }

    public RagDocumentStatus Status { get; set; } = RagDocumentStatus.Pending;

    public string? ErrorMessage { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }
}
