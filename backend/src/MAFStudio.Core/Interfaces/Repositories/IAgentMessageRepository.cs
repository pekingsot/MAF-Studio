using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface IAgentMessageRepository
{
    Task<AgentMessage?> GetByIdAsync(long id);
    Task<List<AgentMessage>> GetByCollaborationIdAsync(long collaborationId, int limit = 100);
    Task<AgentMessage> CreateAsync(AgentMessage message);
    Task<bool> DeleteAsync(long id);
}
