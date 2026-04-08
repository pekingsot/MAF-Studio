using Microsoft.Extensions.AI;

namespace MAFStudio.Core.Interfaces.Services;

/// <summary>
/// IChatClient 工厂接口
/// 根据大模型配置创建统一的 IChatClient 实例
/// </summary>
public interface IChatClientFactory
{
    /// <summary>
    /// 根据配置ID创建 IChatClient
    /// </summary>
    /// <param name="llmConfigId">大模型配置ID</param>
    /// <param name="modelConfigId">模型配置ID（可选，使用默认模型）</param>
    /// <returns>IChatClient 实例</returns>
    Task<IChatClient> CreateClientAsync(long llmConfigId, long? modelConfigId = null);

    /// <summary>
    /// 根据配置信息创建 IChatClient
    /// </summary>
    /// <param name="provider">供应商</param>
    /// <param name="apiKey">API Key</param>
    /// <param name="endpoint">API 端点</param>
    /// <param name="modelName">模型名称</param>
    /// <returns>IChatClient 实例</returns>
    IChatClient CreateClient(string provider, string apiKey, string? endpoint, string modelName);

    /// <summary>
    /// 创建带故障转移功能的 IChatClient
    /// </summary>
    /// <param name="primaryLlmConfigId">主模型配置ID</param>
    /// <param name="primaryModelConfigId">主模型ID（可选）</param>
    /// <param name="fallbackModels">副模型配置列表</param>
    /// <returns>带故障转移的 IChatClient 实例</returns>
    Task<IChatClient> CreateClientWithFallbackAsync(
        long primaryLlmConfigId,
        long? primaryModelConfigId = null,
        List<FallbackModelConfig>? fallbackModels = null);

    /// <summary>
    /// 获取支持的供应商列表
    /// </summary>
    IReadOnlyList<ProviderInfo> GetSupportedProviders();
}

/// <summary>
/// 副模型配置
/// </summary>
public class FallbackModelConfig
{
    public long LlmConfigId { get; set; }
    public long? LlmModelConfigId { get; set; }
    public int Priority { get; set; }
}

/// <summary>
/// 供应商信息
/// </summary>
public class ProviderInfo
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DefaultEndpoint { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = string.Empty;
    public bool SupportsStreaming { get; set; } = true;
    public bool SupportsFunctionCalling { get; set; } = true;
}
