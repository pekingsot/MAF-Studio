using MAFStudio.Core.Utils;

namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("operation_logs")]
public class OperationLog
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public string? ResourceId { get; set; }

    public string? Description { get; set; }

    public string? Details { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? RequestPath { get; set; }

    public string? RequestMethod { get; set; }

    public int? StatusCode { get; set; }

    public long? DurationMs { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void GenerateId()
    {
        Id = SnowflakeIdGenerator.Instance.NextId();
    }
}
