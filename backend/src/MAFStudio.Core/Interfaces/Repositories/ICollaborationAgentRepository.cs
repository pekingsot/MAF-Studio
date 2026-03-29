using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface ICollaborationAgentRepository
{
    Task<List<CollaborationAgent>> GetByCollaborationIdAsync(long collaborationId);
    Task<List<CollaborationAgentWithDetails>> GetWithAgentDetailsByCollaborationIdAsync(long collaborationId);
    Task<List<CollaborationAgent>> GetByAgentIdAsync(long agentId);
    Task<CollaborationAgent?> GetByIdAsync(long id);
    Task<CollaborationAgent> CreateAsync(CollaborationAgent collaborationAgent);
    Task<bool> DeleteAsync(long id);
    Task<bool> DeleteByCollaborationAndAgentAsync(long collaborationId, long agentId);
}

public class CollaborationAgentWithDetails
{
    public long Id { get; set; }
    public long CollaborationId { get; set; }
    public long AgentId { get; set; }
    public string? Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string? AgentType { get; set; }
    public string? AgentStatus { get; set; }
    public string? AgentAvatar { get; set; }
}
