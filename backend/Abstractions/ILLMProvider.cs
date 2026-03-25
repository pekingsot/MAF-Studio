using MAFStudio.Backend.Data;

namespace MAFStudio.Backend.Abstractions
{
    /// <summary>
    /// 大模型供应商接口
    /// 定义所有大模型供应商必须实现的方法
    /// 使用策略模式，不同供应商实现不同的连接和测试逻辑
    /// </summary>
    public interface ILLMProvider
    {
        /// <summary>
        /// 供应商标识（如 qwen、openai、zhipu）
        /// </summary>
        string ProviderId { get; }

        /// <summary>
        /// 供应商显示名称（如 阿里千问、OpenAI、智谱AI）
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
        /// <param name="config">大模型配置</param>
        /// <param name="modelName">模型名称，为空使用默认模型</param>
        /// <returns>测试结果（是否成功、消息、延迟毫秒数）</returns>
        Task<(bool success, string message, int latencyMs)> TestConnectionAsync(LLMConfig config, string? modelName = null);

        /// <summary>
        /// 测试指定模型的连通性
        /// </summary>
        /// <param name="config">大模型配置</param>
        /// <param name="modelConfig">模型配置</param>
        /// <returns>测试结果（是否成功、消息、延迟毫秒数）</returns>
        Task<(bool success, string message, int latencyMs)> TestModelConnectionAsync(LLMConfig config, LLMModelConfig modelConfig);

        /// <summary>
        /// 获取可用模型列表
        /// </summary>
        /// <param name="config">大模型配置</param>
        /// <returns>模型名称列表</returns>
        Task<List<string>> GetAvailableModelsAsync(LLMConfig config);
    }
}
