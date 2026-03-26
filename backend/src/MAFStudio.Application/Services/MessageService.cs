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

    public async Task<AgentMessage> CreateUserMessageAsync(Guid collaborationId, string content, string? userId, string? senderName)
    {
        var message = new AgentMessage
        {
            Id = Guid.NewGuid(),
            CollaborationId = collaborationId,
            Content = content,
            SenderType = SenderType.User,
            SenderName = senderName ?? "User",
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            IsStreaming = false
        };

        return await _messageRepository.CreateAsync(message);
    }

    public async Task<AgentMessage> CreateAgentMessageAsync(Guid collaborationId, Guid agentId, string content, string senderName)
    {
        var message = new AgentMessage
        {
            Id = Guid.NewGuid(),
            FromAgentId = agentId,
            CollaborationId = collaborationId,
            Content = content,
            SenderType = SenderType.Agent,
            SenderName = senderName,
            CreatedAt = DateTime.UtcNow,
            IsStreaming = false
        };

        return await _messageRepository.CreateAsync(message);
    }

    public async Task<List<AgentMessage>> GetByCollaborationIdAsync(Guid collaborationId, int limit = 100)
    {
        return await _messageRepository.GetByCollaborationIdAsync(collaborationId, limit);
    }
}
