using MAFStudio.Core.Enums;

namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("agents")]
public class Agent
{
    [Dapper.Contrib.Extensions.Key]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Type { get; set; } = "Assistant";

    public string Configuration { get; set; } = "{}";

    public string? Avatar { get; set; }

    public string UserId { get; set; } = string.Empty;

    public AgentStatus Status { get; set; } = AgentStatus.Inactive;

    public Guid? LlmConfigId { get; set; }

    public Guid? LlmModelConfigId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Dapper.Contrib.Extensions.Write(false)]
    public LlmConfig? LlmConfig { get; set; }
}
