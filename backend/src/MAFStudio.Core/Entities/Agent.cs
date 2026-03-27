using MAFStudio.Core.Enums;
using MAFStudio.Core.Utils;

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
    /// 系统提示词（智能体自定义的提示词）
    /// </summary>
    public string? SystemPrompt { get; set; }

    public string? Avatar { get; set; }

    public string UserId { get; set; } = string.Empty;

    public AgentStatus Status { get; set; } = AgentStatus.Inactive;

    /// <summary>
    /// 主模型配置ID
    /// </summary>
    public long? LlmConfigId { get; set; }

    /// <summary>
    /// 主模型的具体模型ID
    /// </summary>
    public long? LlmModelConfigId { get; set; }

    /// <summary>
    /// 副模型配置列表（JSON格式，用于故障转移）
    /// 格式：[{"llmConfigId":123,"llmModelConfigId":456,"priority":1}]
    /// </summary>
    public string? FallbackModels { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Dapper.Contrib.Extensions.Write(false)]
    public LlmConfig? LlmConfig { get; set; }
    
    /// <summary>
    /// 所有相关的LLM配置（包括主模型和副模型）
    /// </summary>
    [Dapper.Contrib.Extensions.Write(false)]
    public List<LlmConfig>? AllLlmConfigs { get; set; }

    public void GenerateId()
    {
        Id = SnowflakeIdGenerator.Instance.NextId();
    }
}
