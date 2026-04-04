namespace MAFStudio.Application.DTOs;

public class WorkflowPlanDto
{
    public long Id { get; set; }
    public long CollaborationId { get; set; }
    public string Task { get; set; } = string.Empty;
    public WorkflowDefinitionDto WorkflowDefinition { get; set; } = new();
    public string Status { get; set; } = "pending";
    public long CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public long? ApprovedBy { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class GeneratePlanRequest
{
    public string Task { get; set; } = string.Empty;
}

public class UpdatePlanRequest
{
    public WorkflowDefinitionDto WorkflowDefinition { get; set; } = new();
}

public class ExecutePlanRequest
{
    public string Input { get; set; } = string.Empty;
}
