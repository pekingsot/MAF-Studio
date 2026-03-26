using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;

namespace MAFStudio.Application.Services;

public class LlmConfigService : ILlmConfigService
{
    private readonly ILlmConfigRepository _llmConfigRepository;

    public LlmConfigService(ILlmConfigRepository llmConfigRepository)
    {
        _llmConfigRepository = llmConfigRepository;
    }

    public async Task<List<LlmConfig>> GetByUserIdAsync(string userId)
    {
        return await _llmConfigRepository.GetByUserIdAsync(userId);
    }

    public async Task<List<LlmConfig>> GetAllAsync()
    {
        return await _llmConfigRepository.GetAllAsync();
    }

    public async Task<LlmConfig?> GetByIdAsync(Guid id)
    {
        return await _llmConfigRepository.GetByIdAsync(id);
    }

    public async Task<LlmConfig> CreateAsync(string name, string provider, string? apiKey, string? endpoint, string? defaultModel, string? extraConfig, string userId)
    {
        var config = new LlmConfig
        {
            Id = Guid.NewGuid(),
            Name = name,
            Provider = provider,
            ApiKey = apiKey,
            Endpoint = endpoint,
            DefaultModel = defaultModel,
            ExtraConfig = extraConfig,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        return await _llmConfigRepository.CreateAsync(config);
    }

    public async Task<LlmConfig> UpdateAsync(Guid id, string name, string? apiKey, string? endpoint, string? defaultModel, string? extraConfig)
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
        config.ExtraConfig = extraConfig ?? config.ExtraConfig;
        config.UpdatedAt = DateTime.UtcNow;

        return await _llmConfigRepository.UpdateAsync(config);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _llmConfigRepository.DeleteAsync(id);
    }
}
