namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("collaboration_agents")]
public class CollaborationAgent
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long CollaborationId { get; set; }

    public long AgentId { get; set; }

    /// <summary>
    /// Agent在工作流中的角色：Manager（协调者）或 Worker（执行者）
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Agent的自定义提示词，用于覆盖系统提示词
    /// </summary>
    public string? CustomPrompt { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
