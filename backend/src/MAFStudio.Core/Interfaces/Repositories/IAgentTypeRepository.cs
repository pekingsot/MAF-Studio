using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface IAgentTypeRepository
{
    Task<AgentType?> GetByIdAsync(long id);
    Task<AgentType?> GetByCodeAsync(string code);
    Task<List<AgentType>> GetAllAsync();
    Task<AgentType> CreateAsync(AgentType agentType);
    Task<AgentType> UpdateAsync(AgentType agentType);
    Task<bool> DeleteAsync(long id);
}
