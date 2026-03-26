namespace MAFStudio.Application.DTOs.Requests;

public class CreateCollaborationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Path { get; set; }
    public string? GitRepositoryUrl { get; set; }
    public string? GitBranch { get; set; }
    public string? GitUsername { get; set; }
    public string? GitEmail { get; set; }
    public string? GitAccessToken { get; set; }
}

public class UpdateCollaborationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Path { get; set; }
}

public class AddAgentRequest
{
    public Guid AgentId { get; set; }
    public string? Role { get; set; }
}

public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateTaskStatusRequest
{
    public string Status { get; set; } = "Pending";
}

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public Guid? ToAgentId { get; set; }
}
