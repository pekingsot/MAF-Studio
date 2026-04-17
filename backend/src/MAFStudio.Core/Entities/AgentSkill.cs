namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("agent_skills")]
public class AgentSkill
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long AgentId { get; set; }

    public string SkillName { get; set; } = string.Empty;

    public string SkillContent { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public int Priority { get; set; }

    public string Runtime { get; set; } = "python";

    public string? EntryPoint { get; set; }

    public string? AllowedTools { get; set; }

    public string? Permissions { get; set; }

    public string? Parameters { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}

[Dapper.Contrib.Extensions.Table("skill_templates")]
public class SkillTemplate
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? Tags { get; set; }

    public string Runtime { get; set; } = "python";

    public int UsageCount { get; set; }

    public bool IsOfficial { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
