using MAFStudio.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

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
    /// 大模型选择（JSON格式，包含主模型和副模型）
    /// 格式：[
    ///   {
    ///     "llmConfigId": 123,
    ///     "llmConfigName": "阿里云-通义千问3.5",
    ///     "llmModelConfigId": 456,
    ///     "modelName": "qwen3.5-35b-a3b",
    ///     "isPrimary": true,
    ///     "priority": 1,
    ///     "isValid": true,
    ///     "lastChecked": "2026-04-11T10:00:00Z",
    ///     "msg": "250ms"
    ///   }
    /// ]
    /// </summary>
    [Column("llm_configs")]
    public string? LlmConfigs { get; set; }

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
