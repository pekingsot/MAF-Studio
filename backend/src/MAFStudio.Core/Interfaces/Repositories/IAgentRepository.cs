using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface IAgentRepository
{
    Task<Agent?> GetByIdAsync(long id);
    Task<List<Agent>> GetAllAsync();
    Task<List<Agent>> GetByUserIdAsync(long userId);
    Task<Agent> CreateAsync(Agent agent);
    Task<Agent> UpdateAsync(Agent agent);
    Task<bool> DeleteAsync(long id);
    Task<bool> UpdateStatusAsync(long id, AgentStatus status);
}
