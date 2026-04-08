namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("coordination_sessions")]
public class CoordinationSession
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long CollaborationId { get; set; }

    public long? TaskId { get; set; }

    public long? WorkflowExecutionId { get; set; }

    public string OrchestrationMode { get; set; } = "RoundRobin";

    public string Status { get; set; } = "running";

    public string? Topic { get; set; }

    public string? Metadata { get; set; }

    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    public DateTime? EndTime { get; set; }

    public int TotalRounds { get; set; } = 0;

    public int TotalMessages { get; set; } = 0;

    public string? Conclusion { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
