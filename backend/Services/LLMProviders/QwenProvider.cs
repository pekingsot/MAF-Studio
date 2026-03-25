using MAFStudio.Backend.Data;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Backend.Services.LLMProviders
{
    /// <summary>
    /// 阿里千问供应商实现
    /// 使用 OpenAI 兼容模式
    /// </summary>
    public class QwenProvider : BaseLLMProvider
    {
        public override string ProviderId => "qwen";
        public override string DisplayName => "阿里千问";
        public override string DefaultEndpoint => "https://dashscope.aliyuncs.com/compatible-mode/v1";
        public override string DefaultModel => "qwen-turbo";

        public QwenProvider(ILogger<QwenProvider> logger) : base(logger)
        {
        }

        /// <summary>
        /// 获取可用模型列表
        /// </summary>
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
