using MAFStudio.Core.Enums;

namespace MAFStudio.Application.VOs;

/// <summary>
/// 智能体列表统一返回数据结构
/// </summary>
public class AgentListVo
{
    /// <summary>
    /// 智能体列表
    /// </summary>
    public List<AgentListItemVo> Agents { get; set; } = new();
    
    /// <summary>
    /// 智能体类型列表
    /// </summary>
    public List<AgentTypeVo> AgentTypes { get; set; } = new();
}

public class AgentVo
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? TypeName { get; set; }
    public string? SystemPrompt { get; set; }
    public string? Avatar { get; set; }
    public long UserId { get; set; }
    public AgentStatus Status { get; set; }
    public long? LlmConfigId { get; set; }
    public long? LlmModelConfigId { get; set; }
    public string? LlmConfigName { get; set; }
    public string? PrimaryModelName { get; set; }
    public LlmConfigVo? LlmConfig { get; set; }
    
    /// <summary>
    /// 副模型配置列表（用于故障转移）
    /// </summary>
    public List<FallbackModelVo>? FallbackModels { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AgentListItemVo
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? TypeName { get; set; }
    public string? Avatar { get; set; }
    public AgentStatus Status { get; set; }
    public long? LlmConfigId { get; set; }
    public long? LlmModelConfigId { get; set; }
    public string? LlmConfigName { get; set; }
    public string? PrimaryModelName { get; set; }
    public List<FallbackModelVo>? FallbackModels { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? SystemPrompt { get; set; }
}

/// <summary>
/// 副模型配置VO
/// </summary>
public class FallbackModelVo
{
    public long LlmConfigId { get; set; }
    public string? LlmConfigName { get; set; }
    public long? LlmModelConfigId { get; set; }
    public string? ModelName { get; set; }
    public int Priority { get; set; }
}
