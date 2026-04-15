using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface IGroupMessageRepository
{
    Task<List<GroupMessage>> GetByCollaborationIdAsync(long collaborationId, int limit = 50, long? beforeId = null);
    Task<GroupMessage> CreateAsync(GroupMessage message);
}
