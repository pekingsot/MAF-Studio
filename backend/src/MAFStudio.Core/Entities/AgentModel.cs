namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("agent_models")]
public class AgentModel
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long AgentId { get; set; }

    public long LlmConfigId { get; set; }

    public long? LlmModelConfigId { get; set; }

    public int Priority { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime CreatedAt { get; set; }
}
