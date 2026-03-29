using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Services;

public interface IMessageService
{
    Task<AgentMessage> CreateUserMessageAsync(long collaborationId, string content, long? userId, string? senderName);
    Task<AgentMessage> CreateAgentMessageAsync(long collaborationId, long agentId, string content, string senderName);
    Task<List<AgentMessage>> GetByCollaborationIdAsync(long collaborationId, int limit = 100);
}
