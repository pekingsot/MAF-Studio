using MAFStudio.Core.Enums;

namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("agents")]
public class Agent : BaseEntityWithUpdate
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Type { get; set; } = "Assistant";

    /// <summary>
    /// 智能体类型名称（冗余字段，优化查询性能）
    /// </summary>
    public string? TypeName { get; set; }

    /// <summary>
    /// 系统提示词（智能体自定义的提示词）
    /// </summary>
    public string? SystemPrompt { get; set; }

    public string? Avatar { get; set; }

    public long UserId { get; set; }

    public AgentStatus Status { get; set; } = AgentStatus.Inactive;

    /// <summary>
    /// 主模型配置ID
    /// </summary>
    public long? LlmConfigId { get; set; }

    /// <summary>
    /// 主模型配置名称（冗余字段，优化查询性能）
    /// </summary>
    public string? LlmConfigName { get; set; }

    /// <summary>
    /// 主模型的具体模型ID
    /// </summary>
    public long? LlmModelConfigId { get; set; }

    /// <summary>
    /// 主模型名称（冗余字段，优化查询性能）
    /// </summary>
    public string? LlmModelName { get; set; }

    /// <summary>
    /// 副模型配置列表（JSON格式，用于故障转移）
    /// 格式：[{"llmConfigId":123,"llmConfigName":"配置名","llmModelConfigId":456,"modelName":"模型名","priority":1}]
    /// 包含冗余字段（llmConfigName、modelName）优化查询性能
    /// </summary>
    public string? FallbackModels { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Dapper.Contrib.Extensions.Write(false)]
    public LlmConfig? LlmConfig { get; set; }
    
    /// <summary>
    /// 所有相关的LLM配置（包括主模型和副模型）
    /// 仅用于兼容旧逻辑，新逻辑使用冗余字段
    /// </summary>
    [Dapper.Contrib.Extensions.Write(false)]
    public List<LlmConfig>? AllLlmConfigs { get; set; }
}
