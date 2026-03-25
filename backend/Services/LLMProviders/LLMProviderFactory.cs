using Microsoft.Extensions.DependencyInjection;
using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Providers;
using MAFStudio.Backend.Models;

namespace MAFStudio.Backend.Services.LLMProviders
{
    /// <summary>
    /// 大模型供应商工厂
    /// 根据供应商标识获取对应的供应商实例
    /// 使用工厂模式创建不同的LLM供应商实例
    /// </summary>
    public class LLMProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, Type> _providers;

        /// <summary>
        /// 构造函数
        /// 注册所有支持的LLM供应商
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        public LLMProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            // 注册所有供应商
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
