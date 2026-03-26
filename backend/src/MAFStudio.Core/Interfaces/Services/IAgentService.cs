using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Services;

public interface IAgentService
{
    Task<List<Agent>> GetAllAsync();
    Task<List<Agent>> GetByUserIdAsync(string userId, bool isAdmin);
    Task<Agent?> GetByIdAsync(Guid id);
    Task<Agent> CreateAsync(string name, string? description, string type, string configuration, string? avatar, string userId, Guid? llmConfigId = null, Guid? llmModelConfigId = null);
    Task<Agent> UpdateAsync(Guid id, string name, string? description, string? configuration, string? avatar, Guid? llmConfigId = null, Guid? llmModelConfigId = null);
    Task<bool> DeleteAsync(Guid id);
    Task<Agent> UpdateStatusAsync(Guid id, Core.Enums.AgentStatus status);
}
