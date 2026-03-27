namespace MAFStudio.Core.DTOs;

/// <summary>
/// 副模型配置项
/// </summary>
public class FallbackModelConfig
{
    /// <summary>
    /// 大模型配置ID
    /// </summary>
    public long LlmConfigId { get; set; }

    /// <summary>
    /// 具体模型配置ID
    /// </summary>
    public long? LlmModelConfigId { get; set; }

    /// <summary>
    /// 优先级（数字越小优先级越高）
    /// </summary>
    public int Priority { get; set; }
}
