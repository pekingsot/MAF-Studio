using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using MAFStudio.Application.Clients;

namespace MAFStudio.Application.Services;

/// <summary>
/// IChatClient 工厂实现
/// 根据配置创建不同供应商的 IChatClient 实例
/// </summary>
public class ChatClientFactory : IChatClientFactory
{
    private readonly ILlmConfigRepository _llmConfigRepository;
    private readonly ILlmModelConfigRepository _modelConfigRepository;
    private readonly ILogger<ChatClientFactory> _logger;

    private static readonly List<ProviderInfo> _supportedProviders = new()
    {
        new ProviderInfo
        {
            Id = "qwen",
            DisplayName = "阿里千问",
            DefaultEndpoint = "https://dashscope.aliyuncs.com/compatible-mode/v1",
            DefaultModel = "qwen-max",
            SupportsStreaming = true,
            SupportsFunctionCalling = true
        },
        new ProviderInfo
        {
            Id = "zhipu",
            DisplayName = "智谱AI",
            DefaultEndpoint = "https://open.bigmodel.cn/api/paas/v4",
            DefaultModel = "glm-4",
            SupportsStreaming = true,
            SupportsFunctionCalling = true
        },
        new ProviderInfo
        {
            Id = "openai",
            DisplayName = "OpenAI",
            DefaultEndpoint = "https://api.openai.com/v1",
            DefaultModel = "gpt-4o",
            SupportsStreaming = true,
            SupportsFunctionCalling = true
        },
        new ProviderInfo
        {
            Id = "deepseek",
            DisplayName = "DeepSeek",
            DefaultEndpoint = "https://api.deepseek.com/v1",
            DefaultModel = "deepseek-chat",
            SupportsStreaming = true,
            SupportsFunctionCalling = true
        },
        new ProviderInfo
        {
            Id = "anthropic",
            DisplayName = "Anthropic",
            DefaultEndpoint = "https://api.anthropic.com/v1",
            DefaultModel = "claude-3-opus-20240229",
            SupportsStreaming = true,
            SupportsFunctionCalling = true
        },
        new ProviderInfo
        {
            Id = "openai_compatible",
            DisplayName = "OpenAI兼容",
            DefaultEndpoint = "",
            DefaultModel = "gpt-4o",
            SupportsStreaming = true,
            SupportsFunctionCalling = true
        },
        new ProviderInfo
        {
            Id = "ollama",
            DisplayName = "Ollama (本地)",
            DefaultEndpoint = "http://localhost:11434/v1",
            DefaultModel = "llama3",
            SupportsStreaming = true,
            SupportsFunctionCalling = false
        }
    };

    public ChatClientFactory(
        ILlmConfigRepository llmConfigRepository,
        ILlmModelConfigRepository modelConfigRepository,
        ILogger<ChatClientFactory> logger)
    {
        _llmConfigRepository = llmConfigRepository;
        _modelConfigRepository = modelConfigRepository;
        _logger = logger;
    }

    public async Task<IChatClient> CreateClientAsync(long llmConfigId, long? modelConfigId = null)
    {
        var config = await _llmConfigRepository.GetByIdAsync(llmConfigId);
        if (config == null)
        {
            throw new InvalidOperationException($"LLM配置不存在: {llmConfigId}");
        }

        string modelName;
        if (modelConfigId.HasValue)
        {
            var modelConfig = await _modelConfigRepository.GetByIdAsync(modelConfigId.Value);
            if (modelConfig == null || modelConfig.LlmConfigId != llmConfigId)
            {
                throw new InvalidOperationException($"模型配置不存在或不属于该LLM配置: {modelConfigId}");
            }
            modelName = modelConfig.ModelName;
        }
        else
        {
            var models = await _modelConfigRepository.GetByLlmConfigIdAsync(llmConfigId);
            var defaultModel = models.FirstOrDefault(m => m.IsDefault) ?? models.FirstOrDefault();
            modelName = defaultModel?.ModelName ?? config.DefaultModel ?? GetDefaultModel(config.Provider);
        }

        return CreateClient(config.Provider, config.ApiKey!, config.Endpoint, modelName);
    }

    public IChatClient CreateClient(string provider, string apiKey, string? endpoint, string modelName)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("API Key 不能为空", nameof(apiKey));
        }

        var providerInfo = _supportedProviders.FirstOrDefault(p => p.Id.Equals(provider, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotSupportedException($"不支持的供应商: {provider}");

        var actualEndpoint = endpoint ?? providerInfo.DefaultEndpoint;

        _logger.LogInformation("创建 IChatClient: Provider={Provider}, Model={Model}, Endpoint={Endpoint}",
            provider, modelName, actualEndpoint);

        return provider.ToLower() switch
        {
            "qwen" or "zhipu" or "deepseek" or "openai_compatible" or "ollama"
                => CreateOpenAICompatibleClient(apiKey, actualEndpoint, modelName),
            "openai"
                => CreateOpenAIClient(apiKey, actualEndpoint, modelName),
            "anthropic"
                => CreateAnthropicClient(apiKey, actualEndpoint, modelName),
            _ => throw new NotSupportedException($"不支持的供应商: {provider}")
        };
    }

    public IReadOnlyList<ProviderInfo> GetSupportedProviders() => _supportedProviders.AsReadOnly();

    public async Task<IChatClient> CreateClientWithFallbackAsync(
        long primaryLlmConfigId,
        long? primaryModelConfigId = null,
        List<FallbackModelConfig>? fallbackModels = null)
    {
        var clients = new List<ChatClientInfo>();

        var (primaryClient, primaryModelName) = await CreateClientInternalAsync(primaryLlmConfigId, primaryModelConfigId);
        clients.Add(new ChatClientInfo
        {
            Client = primaryClient,
            ModelName = primaryModelName,
            Priority = 1,
            LlmConfigId = primaryLlmConfigId,
            LlmModelConfigId = primaryModelConfigId
        });

        _logger.LogInformation("创建主模型客户端: LlmConfigId={LlmConfigId}, Model={Model}", 
            primaryLlmConfigId, primaryModelName);

        if (fallbackModels != null && fallbackModels.Count > 0)
        {
            var sortedFallbacks = fallbackModels.OrderBy(f => f.Priority).ToList();

            foreach (var fallback in sortedFallbacks)
            {
                try
                {
                    var (fallbackClient, fallbackModelName) = await CreateClientInternalAsync(
                        fallback.LlmConfigId, 
                        fallback.LlmModelConfigId);

                    clients.Add(new ChatClientInfo
                    {
                        Client = fallbackClient,
                        ModelName = fallbackModelName,
                        Priority = fallback.Priority,
                        LlmConfigId = fallback.LlmConfigId,
                        LlmModelConfigId = fallback.LlmModelConfigId
                    });

                    _logger.LogInformation("添加副模型客户端: LlmConfigId={LlmConfigId}, Model={Model}, Priority={Priority}",
                        fallback.LlmConfigId, fallbackModelName, fallback.Priority);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "创建副模型客户端失败: LlmConfigId={LlmConfigId}", fallback.LlmConfigId);
                }
            }
        }

        if (clients.Count == 1)
        {
            _logger.LogInformation("无副模型配置，返回单一客户端");
            return clients[0].Client;
        }

        _logger.LogInformation("创建FallbackChatClient，共 {Count} 个模型", clients.Count);
        return new FallbackChatClient(clients, _logger);
    }

    private async Task<(IChatClient Client, string ModelName)> CreateClientInternalAsync(long llmConfigId, long? modelConfigId = null)
    {
        var config = await _llmConfigRepository.GetByIdAsync(llmConfigId);
        if (config == null)
        {
            throw new InvalidOperationException($"LLM配置不存在: {llmConfigId}");
        }

        string modelName;
        if (modelConfigId.HasValue)
        {
            var modelConfig = await _modelConfigRepository.GetByIdAsync(modelConfigId.Value);
            if (modelConfig == null || modelConfig.LlmConfigId != llmConfigId)
            {
                throw new InvalidOperationException($"模型配置不存在或不属于该LLM配置: {modelConfigId}");
            }
            modelName = modelConfig.ModelName;
        }
        else
        {
            var models = await _modelConfigRepository.GetByLlmConfigIdAsync(llmConfigId);
            var defaultModel = models.FirstOrDefault(m => m.IsDefault) ?? models.FirstOrDefault();
            modelName = defaultModel?.ModelName ?? config.DefaultModel ?? GetDefaultModel(config.Provider);
        }

        var client = CreateClient(config.Provider, config.ApiKey!, config.Endpoint, modelName);
        return (client, modelName);
    }

    private IChatClient CreateOpenAIClient(string apiKey, string? endpoint, string modelName)
    {
        var credential = new ApiKeyCredential(apiKey);
        var clientOptions = new OpenAIClientOptions
        {
            NetworkTimeout = TimeSpan.FromMinutes(10)
        };
        
        if (!string.IsNullOrEmpty(endpoint) && endpoint != "https://api.openai.com/v1")
        {
            clientOptions.Endpoint = new Uri(endpoint);
        }

        var client = new OpenAIClient(credential, clientOptions);
        return client.GetChatClient(modelName).AsIChatClient();
    }

    private IChatClient CreateOpenAICompatibleClient(string apiKey, string endpoint, string modelName)
    {
        var credential = new ApiKeyCredential(apiKey);
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint),
            NetworkTimeout = TimeSpan.FromMinutes(10)
        };

        var client = new OpenAIClient(credential, clientOptions);
        return client.GetChatClient(modelName).AsIChatClient();
    }

    private IChatClient CreateAnthropicClient(string apiKey, string endpoint, string modelName)
    {
        var credential = new ApiKeyCredential(apiKey);
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint),
            NetworkTimeout = TimeSpan.FromMinutes(10)
        };

        var client = new OpenAIClient(credential, clientOptions);
        return client.GetChatClient(modelName).AsIChatClient();
    }

    private static string GetDefaultModel(string provider)
    {
        var info = _supportedProviders.FirstOrDefault(p => p.Id.Equals(provider, StringComparison.OrdinalIgnoreCase));
        return info?.DefaultModel ?? "gpt-4o";
    }
}
