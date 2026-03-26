using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;

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

    public async Task<Agent?> GetByIdAsync(Guid id)
    {
        return await _agentRepository.GetByIdAsync(id);
    }

    public async Task<Agent> CreateAsync(string name, string? description, string type, string configuration, string? avatar, string userId, Guid? llmConfigId = null, Guid? llmModelConfigId = null)
    {
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Type = type,
            Configuration = configuration,
            Avatar = avatar ?? "🤖",
            UserId = userId,
            LlmConfigId = llmConfigId,
            LlmModelConfigId = llmModelConfigId,
            Status = AgentStatus.Inactive,
            CreatedAt = DateTime.UtcNow
        };

        return await _agentRepository.CreateAsync(agent);
    }

    public async Task<Agent> UpdateAsync(Guid id, string name, string? description, string? configuration, string? avatar, Guid? llmConfigId = null, Guid? llmModelConfigId = null)
    {
        var agent = await _agentRepository.GetByIdAsync(id);
        if (agent == null)
        {
            throw new InvalidOperationException($"Agent with id {id} not found");
        }

        agent.Name = name;
        agent.Description = description;
        agent.Configuration = configuration ?? agent.Configuration;
        agent.Avatar = avatar ?? agent.Avatar;
        agent.LlmConfigId = llmConfigId;
        agent.LlmModelConfigId = llmModelConfigId;
        agent.UpdatedAt = DateTime.UtcNow;

        return await _agentRepository.UpdateAsync(agent);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _agentRepository.DeleteAsync(id);
    }

    public async Task<Agent> UpdateStatusAsync(Guid id, AgentStatus status)
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
