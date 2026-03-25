using MAFStudio.Backend.Data;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Backend.Services.LLMProviders
{
    /// <summary>
    /// OpenAI 供应商实现
    /// </summary>
    public class OpenAIProvider : BaseLLMProvider
    {
        public override string ProviderId => "openai";
        public override string DisplayName => "OpenAI";
        public override string DefaultEndpoint => "https://api.openai.com/v1";
        public override string DefaultModel => "gpt-3.5-turbo";

        public OpenAIProvider(ILogger<OpenAIProvider> logger) : base(logger)
        {
        }

        /// <summary>
        /// 获取可用模型列表
        /// </summary>
        public override Task<List<string>> GetAvailableModelsAsync(LLMConfig config)
        {
            var models = new List<string>
            {
                "gpt-4o",
                "gpt-4o-mini",
                "gpt-4-turbo",
                "gpt-4",
                "gpt-4-32k",
                "gpt-3.5-turbo",
                "gpt-3.5-turbo-16k",
                "o1-preview",
                "o1-mini"
            };
            return Task.FromResult(models);
        }
    }
}
