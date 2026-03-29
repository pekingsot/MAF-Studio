using MAFStudio.Core.Enums;

namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("agent_messages")]
public class AgentMessage
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long? FromAgentId { get; set; }

    public long? ToAgentId { get; set; }

    public long CollaborationId { get; set; }

    public string Content { get; set; } = string.Empty;

    public SenderType SenderType { get; set; } = SenderType.User;

    public string? SenderName { get; set; }

    public long? UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsStreaming { get; set; } = false;
}
