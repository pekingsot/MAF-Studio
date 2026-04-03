using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories
{
    public interface IAgentMessageRepository
    {
        Task<List<AgentMessage>> GetByCollaborationIdAsync(long collaborationId);
        Task<AgentMessage> CreateAsync(AgentMessage message);
    }
}
