using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using System.Text.Json;

namespace MAFStudio.Application.Services;

public class AgentService : IAgentService
{
    private readonly IAgentRepository _agentRepository;

    public AgentService(IAgentRepository agentRepository)
    {
        _agentRepository = agentRepository;
    }

    public async Task<List<Agent>> GetAllAsync()
    {
        return await _agentRepository.GetAllAsync();
    }

    public async Task<List<Agent>> GetByUserIdAsync(string userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return await _agentRepository.GetAllAsync();
        }
        return await _agentRepository.GetByUserIdAsync(userId);
    }

    public async Task<Agent?> GetByIdAsync(long id)
    {
        return await _agentRepository.GetByIdAsync(id);
    }

    public async Task<Agent> CreateAsync(
        string name, 
        string? description, 
        string type, 
        string? systemPrompt, 
        string? avatar, 
        string userId, 
        long? llmConfigId = null, 
        long? llmModelConfigId = null,
        string? fallbackModelsJson = null)
    {
        var agent = new Agent
        {
            Name = name,
            Description = description,
            Type = type,
            SystemPrompt = systemPrompt,
            Avatar = avatar ?? "🤖",
            UserId = userId,
            LlmConfigId = llmConfigId,
            LlmModelConfigId = llmModelConfigId,
            FallbackModels = fallbackModelsJson,
            Status = AgentStatus.Inactive,
        };

        return await _agentRepository.CreateAsync(agent);
    }

    public async Task<Agent> UpdateAsync(
        long id, 
        string name, 
        string? description, 
        string? systemPrompt, 
        string? avatar, 
        long? llmConfigId = null, 
        long? llmModelConfigId = null,
        string? fallbackModelsJson = null)
    {
        var agent = await _agentRepository.GetByIdAsync(id);
        if (agent == null)
        {
            throw new InvalidOperationException($"Agent with id {id} not found");
        }

        agent.Name = name;
        agent.Description = description;
        agent.SystemPrompt = systemPrompt;
        agent.Avatar = avatar ?? agent.Avatar;
        agent.LlmConfigId = llmConfigId;
        agent.LlmModelConfigId = llmModelConfigId;
        agent.FallbackModels = fallbackModelsJson;
        agent.UpdatedAt = DateTime.UtcNow;

        return await _agentRepository.UpdateAsync(agent);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await _agentRepository.DeleteAsync(id);
    }

    public async Task<Agent> UpdateStatusAsync(long id, AgentStatus status)
    {
        var agent = await _agentRepository.GetByIdAsync(id);
        if (agent == null)
        {
            throw new InvalidOperationException($"Agent with id {id} not found");
        }

        await _agentRepository.UpdateStatusAsync(id, status);
        agent.Status = status;
        return agent;
    }
}
