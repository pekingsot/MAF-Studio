using MAFStudio.Core.Enums;

namespace MAFStudio.Application.VOs;

public class CollaborationVo
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Path { get; set; }
    public CollaborationStatus Status { get; set; }
    public long UserId { get; set; }
    public string? GitRepositoryUrl { get; set; }
    public string? GitBranch { get; set; }
    public List<CollaborationAgentVo> Agents { get; set; } = new();
    public List<CollaborationTaskVo> Tasks { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CollaborationAgentVo
{
    public long AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string? AgentType { get; set; }
    public string? AgentStatus { get; set; }
    public string? AgentAvatar { get; set; }
    public string? Role { get; set; }
    public string? CustomPrompt { get; set; }
    public string? SystemPrompt { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class CollaborationTaskVo
{
    public long Id { get; set; }
    public long CollaborationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Prompt { get; set; }
    public CollaborationTaskStatus Status { get; set; }
    public long? AssignedTo { get; set; }
    public string? GitUrl { get; set; }
    public string? GitBranch { get; set; }
    public bool HasGitToken { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
