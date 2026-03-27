using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Services;

public interface IAgentService
{
    Task<List<Agent>> GetAllAsync();
    Task<List<Agent>> GetByUserIdAsync(string userId, bool isAdmin);
    Task<Agent?> GetByIdAsync(long id);
    Task<Agent> CreateAsync(
        string name, 
        string? description, 
        string type, 
        string? systemPrompt, 
        string? avatar, 
        string userId, 
        long? llmConfigId = null, 
        long? llmModelConfigId = null,
        string? fallbackModelsJson = null);
    Task<Agent> UpdateAsync(
        long id, 
        string name, 
        string? description, 
        string? systemPrompt, 
        string? avatar, 
        long? llmConfigId = null, 
        long? llmModelConfigId = null,
        string? fallbackModelsJson = null);
    Task<bool> DeleteAsync(long id);
    Task<Agent> UpdateStatusAsync(long id, Core.Enums.AgentStatus status);
}
