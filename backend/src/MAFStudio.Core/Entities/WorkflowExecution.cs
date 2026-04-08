namespace MAFStudio.Core.Entities;

public class WorkflowExecution
{
    public long Id { get; set; }
    public long CollaborationId { get; set; }
    public long? TaskId { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class WorkflowExecutionMessage
{
    public long Id { get; set; }
    public long ExecutionId { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
