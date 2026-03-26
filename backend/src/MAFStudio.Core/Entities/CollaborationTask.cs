namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("collaboration_tasks")]
public class CollaborationTask
{
    [Dapper.Contrib.Extensions.Key]
    public Guid Id { get; set; }

    public Guid CollaborationId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Enums.CollaborationTaskStatus Status { get; set; } = Enums.CollaborationTaskStatus.Pending;

    public string? AssignedTo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}
