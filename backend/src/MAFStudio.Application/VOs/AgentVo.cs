using MAFStudio.Core.Enums;

namespace MAFStudio.Application.VOs;

public class AgentVo : BaseVo
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Configuration { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AgentStatus Status { get; set; }
    public Guid? LlmConfigId { get; set; }
    public Guid? LlmModelConfigId { get; set; }
    public LlmConfigVo? LlmConfig { get; set; }
}

public class AgentListItemVo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public AgentStatus Status { get; set; }
    public Guid? LlmConfigId { get; set; }
    public string? LlmConfigName { get; set; }
    public DateTime CreatedAt { get; set; }
}
