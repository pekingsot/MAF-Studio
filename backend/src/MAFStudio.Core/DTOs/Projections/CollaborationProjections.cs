namespace MAFStudio.Core.DTOs.Projections;

public record CollaborationSummaryDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Path { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CollaborationAgentSummaryDto
{
    public long AgentId { get; init; }
    public string AgentName { get; init; } = string.Empty;
    public string? AgentType { get; init; }
    public string? AgentStatus { get; init; }
    public string? AgentAvatar { get; init; }
    public string? Role { get; init; }
    public string? CustomPrompt { get; init; }
    public string? SystemPrompt { get; init; }
    public DateTime JoinedAt { get; init; }
}

public record CollaborationTaskSummaryDto
{
    public long Id { get; init; }
    public long CollaborationId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Status { get; init; }
    public string? Config { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
