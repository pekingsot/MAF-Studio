namespace MAFStudio.Application.DTOs;

/// <summary>
/// 大模型配置信息（包含验证状态）
/// </summary>
public class LlmConfigInfo
{
    /// <summary>
    /// LLM配置ID
    /// </summary>
    public long LlmConfigId { get; set; }

    /// <summary>
    /// LLM配置名称
    /// </summary>
    public string LlmConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 模型配置ID
    /// </summary>
    public long? LlmModelConfigId { get; set; }

    /// <summary>
    /// 模型名称
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// 是否主模型
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// 优先级（主模型 priority=1，副模型按顺序递增）
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 验证状态
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 最后检查时间
    /// </summary>
    public DateTime LastChecked { get; set; }

    /// <summary>
    /// 验证结果消息
    /// 成功时：响应时间（毫秒数），如 "250ms"
    /// 失败时：错误信息
    /// </summary>
    public string Msg { get; set; } = string.Empty;
}
