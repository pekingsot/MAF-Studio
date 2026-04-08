namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("workflow_sessions")]
public class WorkflowSession
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long CollaborationId { get; set; }

    public long? TaskId { get; set; }

    public string WorkflowType { get; set; } = "GroupChat";

    public string? OrchestrationMode { get; set; }

    public string Status { get; set; } = "running";

    public string? Topic { get; set; }

    public string? Metadata { get; set; }

    public int TotalRounds { get; set; } = 0;

    public int TotalMessages { get; set; } = 0;

    public string? Conclusion { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
