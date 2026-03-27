using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Services;

public class LlmConfigService : ILlmConfigService
{
    private readonly ILlmConfigRepository _llmConfigRepository;
    private readonly ILlmModelConfigRepository _modelConfigRepository;
    private readonly IChatClientFactory _chatClientFactory;
    private readonly ILogger<LlmConfigService> _logger;

    public LlmConfigService(
        ILlmConfigRepository llmConfigRepository,
        ILlmModelConfigRepository modelConfigRepository,
        IChatClientFactory chatClientFactory,
        ILogger<LlmConfigService> logger)
    {
        _llmConfigRepository = llmConfigRepository;
        _modelConfigRepository = modelConfigRepository;
        _chatClientFactory = chatClientFactory;
        _logger = logger;
    }

    public async Task<List<LlmConfig>> GetByUserIdAsync(string userId)
    {
        var configs = await _llmConfigRepository.GetByUserIdAsync(userId);
        foreach (var config in configs)
        {
            config.Models = await _modelConfigRepository.GetByLlmConfigIdAsync(config.Id);
        }
        return configs;
    }

    public async Task<List<LlmConfig>> GetAllAsync()
    {
        var configs = await _llmConfigRepository.GetAllAsync();
        foreach (var config in configs)
        {
            config.Models = await _modelConfigRepository.GetByLlmConfigIdAsync(config.Id);
        }
        return configs;
    }

    public async Task<LlmConfig?> GetByIdAsync(long id)
    {
        var config = await _llmConfigRepository.GetByIdAsync(id);
        if (config != null)
        {
            config.Models = await _modelConfigRepository.GetByLlmConfigIdAsync(config.Id);
        }
        return config;
    }

    public async Task<LlmConfig> CreateAsync(string name, string provider, string? apiKey, string? endpoint, string? defaultModel, string userId)
    {
        var config = new LlmConfig
        {
            Name = name,
            Provider = provider,
            ApiKey = apiKey,
            Endpoint = endpoint,
            DefaultModel = defaultModel,
            UserId = userId,
        };

        return await _llmConfigRepository.CreateAsync(config);
    }

    public async Task<LlmConfig> UpdateAsync(long id, string name, string? apiKey, string? endpoint, string? defaultModel)
    {
        var config = await _llmConfigRepository.GetByIdAsync(id);
        if (config == null)
        {
            throw new InvalidOperationException($"LLM config with id {id} not found");
        }

        config.Name = name;
        config.ApiKey = apiKey ?? config.ApiKey;
        config.Endpoint = endpoint ?? config.Endpoint;
        config.DefaultModel = defaultModel ?? config.DefaultModel;
        config.MarkAsUpdated();

        return await _llmConfigRepository.UpdateAsync(config);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await _llmConfigRepository.DeleteAsync(id);
    }

    public async Task SetDefaultAsync(long id, string userId)
    {
        await _llmConfigRepository.SetDefaultAsync(id, userId);
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(long id)
    {
        var config = await _llmConfigRepository.GetByIdAsync(id);
        if (config == null)
        {
            return new ConnectionTestResult { Success = false, Message = "配置不存在", LatencyMs = 0 };
        }

        if (string.IsNullOrEmpty(config.ApiKey))
        {
            return new ConnectionTestResult { Success = false, Message = "API Key未配置", LatencyMs = 0 };
        }

        try
        {
            var models = await _modelConfigRepository.GetByLlmConfigIdAsync(id);
            var defaultModel = models.FirstOrDefault(m => m.IsDefault) ?? models.FirstOrDefault();
            var modelName = defaultModel?.ModelName ?? config.DefaultModel ?? "gpt-4o";

            using var client = _chatClientFactory.CreateClient(
                config.Provider,
                config.ApiKey,
                config.Endpoint,
                modelName);

            var testMessage = new Microsoft.Extensions.AI.ChatMessage(
                Microsoft.Extensions.AI.ChatRole.User, "Hi");
            var options = new Microsoft.Extensions.AI.ChatOptions
            {
                MaxOutputTokens = 10
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await client.GetResponseAsync(new[] { testMessage }, options);
            stopwatch.Stop();

            return new ConnectionTestResult
            {
                Success = true,
                Message = "连接成功",
                LatencyMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试连接失败: {Id}", id);
            return new ConnectionTestResult { Success = false, Message = ex.Message, LatencyMs = 0 };
        }
    }

    public async Task<ConnectionTestResult> TestModelConnectionAsync(long configId, long modelId)
    {
        var config = await _llmConfigRepository.GetByIdAsync(configId);
        if (config == null)
        {
            return new ConnectionTestResult { Success = false, Message = "配置不存在", LatencyMs = 0 };
        }

        var model = await _modelConfigRepository.GetByIdAsync(modelId);
        if (model == null)
        {
            return new ConnectionTestResult { Success = false, Message = "模型配置不存在", LatencyMs = 0 };
        }

        if (string.IsNullOrEmpty(config.ApiKey))
        {
            return new ConnectionTestResult { Success = false, Message = "API Key未配置", LatencyMs = 0 };
        }

        try
        {
            using var client = _chatClientFactory.CreateClient(
                config.Provider,
                config.ApiKey,
                config.Endpoint,
                model.ModelName);

            var testMessage = new Microsoft.Extensions.AI.ChatMessage(
                Microsoft.Extensions.AI.ChatRole.User, "Hi");
            var options = new Microsoft.Extensions.AI.ChatOptions
            {
                MaxOutputTokens = 10
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await client.GetResponseAsync(new[] { testMessage }, options);
            stopwatch.Stop();

            return new ConnectionTestResult
            {
                Success = true,
                Message = "连接成功",
                LatencyMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试模型连接失败: {ConfigId}, {ModelId}", configId, modelId);
            return new ConnectionTestResult { Success = false, Message = ex.Message, LatencyMs = 0 };
        }
    }
}
