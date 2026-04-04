namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("workflow_plans")]
public class WorkflowPlan
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long CollaborationId { get; set; }

    public string Task { get; set; } = string.Empty;

    public string WorkflowDefinition { get; set; } = string.Empty;

    public string Status { get; set; } = "pending";

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedAt { get; set; }

    public long? ApprovedBy { get; set; }

    public DateTime? ExecutedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
