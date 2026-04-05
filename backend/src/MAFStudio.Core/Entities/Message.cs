namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("messages")]
public class Message
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long? SessionId { get; set; }

    public long? CollaborationId { get; set; }

    public long? TaskId { get; set; }

    public string? MessageType { get; set; }

    public int? RoundNumber { get; set; }

    public int? StepNumber { get; set; }

    public long? FromAgentId { get; set; }

    public string? FromAgentName { get; set; }

    public string? FromAgentRole { get; set; }

    public long? ToAgentId { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? ThinkingProcess { get; set; }

    public string? SelectedNextSpeaker { get; set; }

    public string? SelectionReason { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
