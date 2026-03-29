namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("collaboration_agents")]
public class CollaborationAgent
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long CollaborationId { get; set; }

    public long AgentId { get; set; }

    public string? Role { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
