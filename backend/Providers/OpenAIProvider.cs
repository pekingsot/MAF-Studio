using MAFStudio.Backend.Data;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Backend.Providers
{
    /// <summary>
    /// OpenAI 供应商实现
    /// 支持 GPT 系列模型
    /// </summary>
    public class OpenAIProvider : BaseLLMProvider
    {
        /// <summary>
        /// 供应商标识
        /// </summary>
        public override string ProviderId => "openai";

        /// <summary>
        /// 供应商显示名称
        /// </summary>
        public override string DisplayName => "OpenAI";

        /// <summary>
        /// 默认端点地址
        /// </summary>
        public override string DefaultEndpoint => "https://api.openai.com/v1";

        /// <summary>
        /// 默认模型
        /// </summary>
        public override string DefaultModel => "gpt-3.5-turbo";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public OpenAIProvider(ILogger<OpenAIProvider> logger) : base(logger)
        {
        }

        /// <summary>
        /// 获取可用模型列表
        /// 返回 OpenAI 支持的所有模型
        /// </summary>
        /// <param name="config">大模型配置</param>
        /// <returns>模型名称列表</returns>
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
