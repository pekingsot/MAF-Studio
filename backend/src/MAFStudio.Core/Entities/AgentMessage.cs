using MAFStudio.Core.Enums;

namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("agent_messages")]
public class AgentMessage
{
    [Dapper.Contrib.Extensions.Key]
    public Guid Id { get; set; }

    public Guid? FromAgentId { get; set; }

    public Guid? ToAgentId { get; set; }

    public Guid CollaborationId { get; set; }

    public string Content { get; set; } = string.Empty;

    public SenderType SenderType { get; set; } = SenderType.User;

    public string? SenderName { get; set; }

    public string? UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsStreaming { get; set; } = false;
}
