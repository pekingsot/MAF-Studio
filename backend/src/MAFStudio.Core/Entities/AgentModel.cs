using MAFStudio.Core.Utils;

namespace MAFStudio.Core.Entities;

/// <summary>
/// 智能体模型配置实体
/// 支持主模型 + 副模型故障转移机制
/// </summary>
[Dapper.Contrib.Extensions.Table("agent_models")]
public class AgentModel
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    /// <summary>
    /// 智能体ID
    /// </summary>
    public long AgentId { get; set; }

    /// <summary>
    /// 大模型配置ID
    /// </summary>
    public long LlmConfigId { get; set; }

    /// <summary>
    /// 模型配置ID（可选，不填则使用默认模型）
    /// </summary>
    public long? LlmModelConfigId { get; set; }

    /// <summary>
    /// 优先级，数字越小优先级越高
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 是否为主模型
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 生成新的雪花ID
    /// </summary>
    public void GenerateId()
    {
        Id = SnowflakeIdGenerator.Instance.NextId();
    }
}
