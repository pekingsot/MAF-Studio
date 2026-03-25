using MAFStudio.Backend.Data;

namespace MAFStudio.Backend.Services.LLMProviders
{
    /// <summary>
    /// 大模型供应商接口
    /// 定义所有大模型供应商必须实现的方法
    /// </summary>
    public interface ILLMProvider
    {
        /// <summary>
        /// 供应商标识
        /// </summary>
        string ProviderId { get; }

        /// <summary>
        /// 供应商显示名称
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 默认端点地址
        /// </summary>
        string DefaultEndpoint { get; }

        /// <summary>
        /// 默认模型
        /// </summary>
        string DefaultModel { get; }

        /// <summary>
        /// 测试供应商连通性
        /// </summary>
        Task<(bool success, string message, int latencyMs)> TestConnectionAsync(LLMConfig config, string? modelName = null);

        /// <summary>
        /// 测试指定模型的连通性
        /// </summary>
        Task<(bool success, string message, int latencyMs)> TestModelConnectionAsync(LLMConfig config, LLMModelConfig modelConfig);

        /// <summary>
        /// 获取可用模型列表
        /// </summary>
        Task<List<string>> GetAvailableModelsAsync(LLMConfig config);
    }
}
