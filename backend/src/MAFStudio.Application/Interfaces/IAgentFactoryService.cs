namespace MAFStudio.Application.Interfaces;

using Microsoft.Extensions.AI;

public interface IAgentFactoryService
{
    Task<IChatClient> CreateAgentAsync(long agentId);

    Task<IChatClient> CreateAgentWithoutCapabilitiesAsync(long agentId);

    Task<IChatClient> CreateChatClientAsync(long llmConfigId, long llmModelConfigId);

    Task<IChatClient> CreateManagerClientAsync();
}
