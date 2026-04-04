namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("coordination_participants")]
public class CoordinationParticipant
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long SessionId { get; set; }

    public long AgentId { get; set; }

    public string AgentName { get; set; } = string.Empty;

    public string? AgentRole { get; set; }

    public bool IsManager { get; set; } = false;

    public int SpeakCount { get; set; } = 0;

    public int TotalTokens { get; set; } = 0;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
