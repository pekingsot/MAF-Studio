using MAFStudio.Core.Enums;

namespace MAFStudio.Application.VOs;

public class CollaborationVo : BaseVo
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Path { get; set; }
    public CollaborationStatus Status { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? GitRepositoryUrl { get; set; }
    public string? GitBranch { get; set; }
    public List<CollaborationAgentVo> Agents { get; set; } = new();
    public List<CollaborationTaskVo> Tasks { get; set; } = new();
}

public class CollaborationAgentVo
{
    public long AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string? AgentAvatar { get; set; }
    public string? Role { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class CollaborationTaskVo : BaseVo
{
    public long CollaborationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CollaborationTaskStatus Status { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? CompletedAt { get; set; }
}
