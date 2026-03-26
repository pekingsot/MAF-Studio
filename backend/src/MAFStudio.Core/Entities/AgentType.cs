namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("agent_types")]
public class AgentType
{
    [Dapper.Contrib.Extensions.Key]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Icon { get; set; }

    public string? DefaultConfiguration { get; set; }

    public Guid? LlmConfigId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
