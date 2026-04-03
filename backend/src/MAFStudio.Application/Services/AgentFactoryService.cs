using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using Microsoft.Extensions.AI;

namespace MAFStudio.Application.Services;

public class AgentFactoryService : IAgentFactoryService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IChatClientFactory _chatClientFactory;

    public AgentFactoryService(
        IAgentRepository agentRepository,
        IChatClientFactory chatClientFactory)
    {
        _agentRepository = agentRepository;
        _chatClientFactory = chatClientFactory;
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

        return await _chatClientFactory.CreateClientAsync(agent.LlmConfigId.Value, agent.LlmModelConfigId.Value);
    }

    public async Task<IChatClient> CreateChatClientAsync(long llmConfigId, long llmModelConfigId)
    {
        return await _chatClientFactory.CreateClientAsync(llmConfigId, llmModelConfigId);
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
