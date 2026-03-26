using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface IAgentMessageRepository
{
    Task<AgentMessage?> GetByIdAsync(Guid id);
    Task<List<AgentMessage>> GetByCollaborationIdAsync(Guid collaborationId, int limit = 100);
    Task<AgentMessage> CreateAsync(AgentMessage message);
    Task<bool> DeleteAsync(Guid id);
}
