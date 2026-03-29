using MAFStudio.Application.Clients;
using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using Microsoft.Extensions.AI;
using OpenAI;

namespace MAFStudio.Application.Services;

public class AgentFactoryService : IAgentFactoryService
{
    private readonly IAgentRepository _agentRepository;
    private readonly ILlmConfigRepository _llmConfigRepository;
    private readonly ILlmModelConfigRepository _llmModelConfigRepository;

    public AgentFactoryService(
        IAgentRepository agentRepository,
        ILlmConfigRepository llmConfigRepository,
        ILlmModelConfigRepository llmModelConfigRepository)
    {
        _agentRepository = agentRepository;
        _llmConfigRepository = llmConfigRepository;
        _llmModelConfigRepository = llmModelConfigRepository;
    }

    public async Task<IChatClient> CreateAgentAsync(long agentId)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent == null)
        {
            throw new NotFoundException($"Agent {agentId} not found");
        }

        if (!agent.LlmConfigId.HasValue || !agent.LlmModelConfigId.HasValue)
        {
            throw new BusinessException("Agent缺少LLM配置");
        }

        return await CreateChatClientAsync(agent.LlmConfigId.Value, agent.LlmModelConfigId.Value);
    }

    public async Task<IChatClient> CreateChatClientAsync(long llmConfigId, long llmModelConfigId)
    {
        var llmConfig = await _llmConfigRepository.GetByIdAsync(llmConfigId);
        if (llmConfig == null)
        {
            throw new NotFoundException($"LLM配置 {llmConfigId} 不存在");
        }

        var modelConfig = await _llmModelConfigRepository.GetByIdAsync(llmModelConfigId);
        if (modelConfig == null)
        {
            throw new NotFoundException($"模型配置 {llmModelConfigId} 不存在");
        }

        return llmConfig.Provider.ToLower() switch
        {
            "openai" => CreateOpenAIChatClient(llmConfig, modelConfig),
            "azure" => CreateAzureOpenAIChatClient(llmConfig, modelConfig),
            "qwen" => CreateCustomChatClient(llmConfig, modelConfig),
            "deepseek" => CreateCustomChatClient(llmConfig, modelConfig),
            _ => throw new NotSupportedException($"不支持的LLM提供商: {llmConfig.Provider}")
        };
    }

    private IChatClient CreateOpenAIChatClient(LlmConfig config, LlmModelConfig model)
    {
        var client = new OpenAIClient(config.ApiKey);
        return client.GetChatClient(model.ModelName).AsIChatClient();
    }

    private IChatClient CreateAzureOpenAIChatClient(LlmConfig config, LlmModelConfig model)
    {
        throw new NotImplementedException("Azure OpenAI ChatClient待实现，需要Azure.Identity包");
    }

    private IChatClient CreateCustomChatClient(LlmConfig config, LlmModelConfig model)
    {
        var baseUrl = config.Endpoint ?? "https://api.openai.com";
        
        return new CustomOpenAICompatibleChatClient(
            config.ApiKey ?? string.Empty,
            baseUrl,
            model.ModelName,
            config.Provider
        );
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}
