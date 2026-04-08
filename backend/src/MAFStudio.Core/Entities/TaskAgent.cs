namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("task_agents")]
public class TaskAgent
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long TaskId { get; set; }

    public long AgentId { get; set; }

    public string? Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
