using MAFStudio.Backend.Data;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Backend.Services.LLMProviders
{
    /// <summary>
    /// 智谱AI供应商实现
    /// </summary>
    public class ZhipuProvider : BaseLLMProvider
    {
        public override string ProviderId => "zhipu";
        public override string DisplayName => "智谱AI";
        public override string DefaultEndpoint => "https://open.bigmodel.cn/api/paas/v4";
        public override string DefaultModel => "glm-4";

        public ZhipuProvider(ILogger<ZhipuProvider> logger) : base(logger)
        {
        }

        /// <summary>
        /// 获取可用模型列表
        /// </summary>
        public override Task<List<string>> GetAvailableModelsAsync(LLMConfig config)
        {
            var models = new List<string>
            {
                "glm-4",
                "glm-4-air",
                "glm-4-airx",
                "glm-4-flash",
                "glm-4-long",
                "glm-4v",
                "glm-4v-plus",
                "glm-z1-air",
                "glm-z1-airx",
                "glm-z1-flash",
                "glm-3-turbo"
            };
            return Task.FromResult(models);
        }
    }
}
