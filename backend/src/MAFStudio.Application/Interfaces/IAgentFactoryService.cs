namespace MAFStudio.Application.Interfaces;

using Microsoft.Extensions.AI;

public interface IAgentFactoryService
{
    /// <summary>
    /// 创建可执行的AI Agent实例
    /// </summary>
    /// <param name="agentId">Agent ID</param>
    /// <returns>AI Agent实例</returns>
    Task<IChatClient> CreateAgentAsync(long agentId);

    /// <summary>
    /// 创建ChatClient
    /// </summary>
    /// <param name="llmConfigId">LLM配置ID</param>
    /// <param name="llmModelConfigId">模型配置ID</param>
    /// <returns>ChatClient实例</returns>
    Task<IChatClient> CreateChatClientAsync(long llmConfigId, long llmModelConfigId);

    /// <summary>
    /// 创建Magentic Manager客户端（用于协调工作流）
    /// </summary>
    /// <returns>Magentic Manager客户端实例</returns>
    Task<IChatClient> CreateManagerClientAsync();
}
