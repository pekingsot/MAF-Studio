namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("operation_logs")]
public class OperationLog
{
    [Dapper.Contrib.Extensions.Key]
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public string? ResourceId { get; set; }

    public string? Description { get; set; }

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
