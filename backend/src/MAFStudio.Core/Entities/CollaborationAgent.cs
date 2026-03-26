namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("collaboration_agents")]
public class CollaborationAgent
{
    [Dapper.Contrib.Extensions.Key]
    public Guid Id { get; set; }

    public Guid CollaborationId { get; set; }

    public Guid AgentId { get; set; }

    public string? Role { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
