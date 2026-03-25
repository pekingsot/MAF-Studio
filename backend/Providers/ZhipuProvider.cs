using MAFStudio.Backend.Data;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Backend.Providers
{
    /// <summary>
    /// 智谱AI供应商实现
    /// 支持 GLM 系列模型
    /// </summary>
    public class ZhipuProvider : BaseLLMProvider
    {
        /// <summary>
        /// 供应商标识
        /// </summary>
        public override string ProviderId => "zhipu";

        /// <summary>
        /// 供应商显示名称
        /// </summary>
        public override string DisplayName => "智谱AI";

        /// <summary>
        /// 默认端点地址
        /// </summary>
        public override string DefaultEndpoint => "https://open.bigmodel.cn/api/paas/v4";

        /// <summary>
        /// 默认模型
        /// </summary>
        public override string DefaultModel => "glm-4";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public ZhipuProvider(ILogger<ZhipuProvider> logger) : base(logger)
        {
        }

        /// <summary>
        /// 获取可用模型列表
        /// 返回智谱AI支持的所有模型
        /// </summary>
        /// <param name="config">大模型配置</param>
        /// <returns>模型名称列表</returns>
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
