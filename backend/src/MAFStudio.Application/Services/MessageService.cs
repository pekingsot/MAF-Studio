using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;

namespace MAFStudio.Application.Services;

public class MessageService : IMessageService
{
    private readonly IAgentMessageRepository _messageRepository;

    public MessageService(IAgentMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task<AgentMessage> CreateUserMessageAsync(long collaborationId, string content, long? userId, string? senderName)
    {
        var message = new AgentMessage
        {
            CollaborationId = collaborationId,
            Content = content,
            SenderType = SenderType.User,
            SenderName = senderName ?? "User",
            UserId = userId,
            IsStreaming = false
        };

        return await _messageRepository.CreateAsync(message);
    }

    public async Task<AgentMessage> CreateAgentMessageAsync(long collaborationId, long agentId, string content, string senderName)
    {
        var message = new AgentMessage
        {
            FromAgentId = agentId,
            CollaborationId = collaborationId,
            Content = content,
            SenderType = SenderType.Agent,
            SenderName = senderName,
            IsStreaming = false
        };

        return await _messageRepository.CreateAsync(message);
    }

    public async Task<List<AgentMessage>> GetByCollaborationIdAsync(long collaborationId, int limit = 100)
    {
        return await _messageRepository.GetByCollaborationIdAsync(collaborationId, limit);
    }
}
