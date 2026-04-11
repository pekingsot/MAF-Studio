using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Application.DTOs;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Services;

/// <summary>
/// 智能体服务实现
/// </summary>
public class AgentService : IAgentService
{
    private readonly IAgentRepository _agentRepository;
    private readonly ILlmConfigService _llmConfigService;
    private readonly ILogger<AgentService>? _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public AgentService(
        IAgentRepository agentRepository,
        ILlmConfigService llmConfigService,
        ILogger<AgentService>? logger = null)
    {
        _agentRepository = agentRepository;
        _llmConfigService = llmConfigService;
        _logger = logger;
    }

    public async Task<List<Agent>> GetAllAsync()
    {
        return await _agentRepository.GetAllAsync();
    }

    public async Task<List<Agent>> GetByUserIdAsync(long userId, bool isAdmin)
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
        long userId,
        string? llmConfigsJson = null,
        string? typeName = null)
    {
        var agent = new Agent
        {
            Name = name,
            Description = description,
            Type = type,
            TypeName = typeName,
            SystemPrompt = systemPrompt,
            Avatar = avatar ?? "🤖",
            UserId = userId,
            LlmConfigs = llmConfigsJson,
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
        string? llmConfigsJson = null,
        string? typeName = null)
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
        agent.TypeName = typeName;
        agent.LlmConfigs = llmConfigsJson;
        agent.UpdatedAt = DateTime.UtcNow;

        return await _agentRepository.UpdateAsync(agent);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await _agentRepository.DeleteAsync(id);
    }

    public async Task<bool> UpdateStatusAsync(long id, AgentStatus status)
    {
        var agent = await _agentRepository.GetByIdAsync(id);
        if (agent == null)
        {
            throw new InvalidOperationException($"Agent with id {id} not found");
        }

        // 激活智能体时验证所有模型
        if (status == AgentStatus.Active)
        {
            agent = await ValidateAgentModelsAsync(agent);
            
            // 如果模型验证失败，设置为 Error 状态
            if (!string.IsNullOrEmpty(agent.LlmConfigs))
            {
                var llmConfigs = JsonSerializer.Deserialize<List<LlmConfigInfo>>(agent.LlmConfigs, JsonOptions);
                if (llmConfigs != null && llmConfigs.Any(c => !c.IsValid))
                {
                    status = AgentStatus.Error;
                }
            }
        }

        var result = await _agentRepository.UpdateStatusAsync(id, status);
        
        // 更新数据库中的 LlmConfigs 字段（包含验证结果）
        if (result && !string.IsNullOrEmpty(agent.LlmConfigs))
        {
            await _agentRepository.UpdateAsync(agent);
        }
        
        return result;
    }

    /// <summary>
    /// 验证智能体的所有模型配置
    /// </summary>
    /// <param name="agent">智能体实体</param>
    /// <returns>验证后的智能体（包含验证结果）</returns>
    public async Task<Agent> ValidateAgentModelsAsync(Agent agent)
    {
        if (string.IsNullOrEmpty(agent.LlmConfigs))
        {
            return agent;
        }

        try
        {
            var llmConfigs = JsonSerializer.Deserialize<List<LlmConfigInfo>>(agent.LlmConfigs, JsonOptions)
                ?? new List<LlmConfigInfo>();

            foreach (var configInfo in llmConfigs)
            {
                try
                {
                    var result = await _llmConfigService.TestModelConnectionAsync(
                        configInfo.LlmConfigId, 
                        configInfo.LlmModelConfigId ?? 0);
                    
                    configInfo.IsValid = result.Success;
                    configInfo.LastChecked = DateTime.UtcNow;
                    configInfo.Msg = result.Success 
                        ? $"{result.LatencyMs}ms" 
                        : result.Message;
                    
                    if (result.Success)
                    {
                        _logger?.LogInformation("模型验证成功: {ModelName}, 响应时间: {LatencyMs}ms", 
                            configInfo.ModelName, result.LatencyMs);
                    }
                    else
                    {
                        _logger?.LogWarning("模型验证失败: {ModelName}, 错误: {Message}", 
                            configInfo.ModelName, result.Message);
                    }
                }
                catch (Exception ex)
                {
                    configInfo.IsValid = false;
                    configInfo.LastChecked = DateTime.UtcNow;
                    configInfo.Msg = ex.Message.Length > 200 
                        ? ex.Message.Substring(0, 200) + "..." 
                        : ex.Message;
                    
                    _logger?.LogWarning(ex, "模型验证失败: {ModelName}, 错误: {Message}", 
                        configInfo.ModelName, ex.Message);
                }
            }

            agent.LlmConfigs = JsonSerializer.Serialize(llmConfigs, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "解析 LlmConfigs 失败: {Message}", ex.Message);
        }

        return agent;
    }
}
