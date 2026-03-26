using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Services;

public interface IMessageService
{
    Task<AgentMessage> CreateUserMessageAsync(Guid collaborationId, string content, string? userId, string? senderName);
    Task<AgentMessage> CreateAgentMessageAsync(Guid collaborationId, Guid agentId, string content, string senderName);
    Task<List<AgentMessage>> GetByCollaborationIdAsync(Guid collaborationId, int limit = 100);
}
