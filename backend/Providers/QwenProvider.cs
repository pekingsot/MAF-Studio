using MAFStudio.Backend.Data;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Backend.Providers
{
    /// <summary>
    /// 阿里千问供应商实现
    /// 使用 OpenAI 兼容模式，支持流式输出
    /// </summary>
    public class QwenProvider : BaseLLMProvider
    {
        /// <summary>
        /// 供应商标识
        /// </summary>
        public override string ProviderId => "qwen";

        /// <summary>
        /// 供应商显示名称
        /// </summary>
        public override string DisplayName => "阿里千问";

        /// <summary>
        /// 默认端点地址（OpenAI兼容模式）
        /// </summary>
        public override string DefaultEndpoint => "https://dashscope.aliyuncs.com/compatible-mode/v1";

        /// <summary>
        /// 默认模型
        /// </summary>
        public override string DefaultModel => "qwen-turbo";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public QwenProvider(ILogger<QwenProvider> logger) : base(logger)
        {
        }

        /// <summary>
        /// 获取可用模型列表
        /// 返回阿里千问支持的所有模型
        /// </summary>
        /// <param name="config">大模型配置</param>
        /// <returns>模型名称列表</returns>
        public override Task<List<string>> GetAvailableModelsAsync(LLMConfig config)
        {
            var models = new List<string>
            {
                "qwen-turbo",
                "qwen-plus",
                "qwen-max",
                "qwen-max-longcontext",
                "qwen-long",
                "qwen-vl-plus",
                "qwen-vl-max",
                "qwen-audio-turbo",
                "qwen2.5-72b-instruct",
                "qwen2.5-32b-instruct",
                "qwen2.5-14b-instruct",
                "qwen2.5-7b-instruct",
                "qwen2.5-3b-instruct",
                "qwen2.5-1.5b-instruct",
                "qwen2.5-0.5b-instruct",
                "qvq-max-2025-03-25",
                "qvq-max-latest"
            };
            return Task.FromResult(models);
        }
    }
}
