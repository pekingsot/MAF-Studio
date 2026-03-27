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
    /// 获取支持的供应商列表
    /// </summary>
    IReadOnlyList<ProviderInfo> GetSupportedProviders();
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
