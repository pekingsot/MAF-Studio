using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MAFStudio.Backend.Providers
{
    /// <summary>
    /// 大模型供应商工厂
    /// 使用工厂模式根据供应商标识获取对应的供应商实例
    /// </summary>
    public class LLMProviderFactory
    {
        /// <summary>
        /// 服务提供者
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 供应商注册表
        /// </summary>
        private readonly Dictionary<string, Type> _providers;

        /// <summary>
        /// 构造函数
        /// 注册所有支持的供应商
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        public LLMProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            // 注册所有供应商（不区分大小写）
            _providers = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                { "qwen", typeof(QwenProvider) },
                { "openai", typeof(OpenAIProvider) },
                { "zhipu", typeof(ZhipuProvider) }
            };
        }

        /// <summary>
        /// 获取供应商实例
        /// </summary>
        /// <param name="providerId">供应商标识</param>
        /// <returns>供应商实例，如果未找到返回null</returns>
        public ILLMProvider? GetProvider(string providerId)
        {
            if (string.IsNullOrEmpty(providerId))
            {
                return null;
            }

            if (_providers.TryGetValue(providerId, out var providerType))
            {
                return _serviceProvider.GetService(providerType) as ILLMProvider;
            }

            return null;
        }

        /// <summary>
        /// 获取所有已注册的供应商信息
        /// </summary>
        /// <returns>供应商信息列表</returns>
        public List<ProviderInfo> GetAllProviders()
        {
            var result = new List<ProviderInfo>();

            foreach (var kvp in _providers)
            {
                var provider = GetProvider(kvp.Key);
                if (provider != null)
                {
                    result.Add(new ProviderInfo
                    {
                        Id = provider.ProviderId,
                        DisplayName = provider.DisplayName,
                        DefaultEndpoint = provider.DefaultEndpoint,
                        DefaultModel = provider.DefaultModel
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// 检查供应商是否已注册
        /// </summary>
        /// <param name="providerId">供应商标识</param>
        /// <returns>是否已注册</returns>
        public bool IsProviderRegistered(string providerId)
        {
            return !string.IsNullOrEmpty(providerId) && _providers.ContainsKey(providerId);
        }
    }
}
