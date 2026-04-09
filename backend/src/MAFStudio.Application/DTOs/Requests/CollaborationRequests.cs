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
    public string? Config { get; set; }
}

public class UpdateCollaborationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Path { get; set; }
}

public class AddAgentRequest
{
    public long AgentId { get; set; }
    public string? Role { get; set; }
    public string? CustomPrompt { get; set; }
}

public class UpdateAgentRoleRequest
{
    public string Role { get; set; } = string.Empty;
    public string? CustomPrompt { get; set; }
}

public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Prompt { get; set; }
    public string? GitUrl { get; set; }
    public string? GitBranch { get; set; }
    public string? GitToken { get; set; }
    public List<long>? AgentIds { get; set; }
    
    /// <summary>
    /// 任务配置（JSON格式）
    /// 包含：orchestrationMode, managerAgentId, maxIterations 等
    /// </summary>
    public string? Config { get; set; }
}

public class UpdateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Prompt { get; set; }
    public string? GitUrl { get; set; }
    public string? GitBranch { get; set; }
    public string? GitToken { get; set; }
    public List<long>? AgentIds { get; set; }
    
    /// <summary>
    /// 任务配置（JSON格式）
    /// </summary>
    public string? Config { get; set; }
}

public class UpdateTaskStatusRequest
{
    public string Status { get; set; } = "Pending";
}

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public long? ToAgentId { get; set; }
}
