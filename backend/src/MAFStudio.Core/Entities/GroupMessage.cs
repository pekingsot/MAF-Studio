namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("group_messages")]
public class GroupMessage
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long CollaborationId { get; set; }

    public string MessageType { get; set; } = "chat";

    public string SenderType { get; set; } = "User";

    public long? FromAgentId { get; set; }

    public string? FromAgentName { get; set; }

    public string? FromAgentRole { get; set; }

    public string? FromAgentType { get; set; }

    public string? FromAgentAvatar { get; set; }

    public string? ModelName { get; set; }

    public string Content { get; set; } = string.Empty;

    public bool IsMentioned { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
