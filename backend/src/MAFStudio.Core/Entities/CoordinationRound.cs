namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("coordination_rounds")]
public class CoordinationRound
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long SessionId { get; set; }

    public int RoundNumber { get; set; }

    public long? SpeakerAgentId { get; set; }

    public string SpeakerName { get; set; } = string.Empty;

    public string? SpeakerRole { get; set; }

    public string MessageContent { get; set; } = string.Empty;

    public long? MessageId { get; set; }

    public string? ThinkingProcess { get; set; }

    public string? SelectedNextSpeaker { get; set; }

    public string? SelectionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
