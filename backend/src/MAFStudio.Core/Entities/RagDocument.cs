using MAFStudio.Core.Enums;
using MAFStudio.Core.Utils;

namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("rag_documents")]
public class RagDocument
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string? FilePath { get; set; }

    public string? FileType { get; set; }

    public long FileSize { get; set; }

    public RagDocumentStatus Status { get; set; } = RagDocumentStatus.Pending;

    public string? ErrorMessage { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// 生成新的雪花ID
    /// </summary>
    public void GenerateId()
    {
        Id = SnowflakeIdGenerator.Instance.NextId();
    }
}
