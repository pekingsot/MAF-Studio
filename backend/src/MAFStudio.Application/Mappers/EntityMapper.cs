using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Application.VOs;
using System.Text.Json;

namespace MAFStudio.Application.Mappers;

public static class EntityMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static AgentVo ToVo(this Agent agent)
    {
        List<LlmConfigInfoVo>? llmConfigs = null;
        
        if (!string.IsNullOrEmpty(agent.LlmConfigs))
        {
            try
            {
                llmConfigs = JsonSerializer.Deserialize<List<LlmConfigInfoVo>>(agent.LlmConfigs, JsonOptions);
            }
            catch
            {
            }
        }
        
        return new AgentVo
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            Type = agent.Type,
            TypeName = agent.TypeName,
            SystemPrompt = agent.SystemPrompt,
            Avatar = agent.Avatar,
            UserId = agent.UserId,
            Status = agent.Status,
            CreatedAt = agent.CreatedAt,
            LlmConfig = agent.LlmConfig?.ToVo(),
            LlmConfigs = llmConfigs
        };
    }

    public static AgentListItemVo ToListItemVo(this Agent agent)
    {
        List<LlmConfigInfoVo>? llmConfigs = null;
        
        if (!string.IsNullOrEmpty(agent.LlmConfigs))
        {
            try
            {
                llmConfigs = JsonSerializer.Deserialize<List<LlmConfigInfoVo>>(agent.LlmConfigs, JsonOptions);
            }
            catch
            {
            }
        }
        
        return new AgentListItemVo
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            Type = agent.Type,
            TypeName = agent.TypeName,
            Avatar = agent.Avatar,
            Status = agent.Status,
            LlmConfigs = llmConfigs,
            CreatedAt = agent.CreatedAt,
            SystemPrompt = agent.SystemPrompt
        };
    }

    public static AgentTypeVo ToVo(this AgentType agentType)
    {
        return new AgentTypeVo
        {
            Id = agentType.Id,
            Name = agentType.Name,
            Code = agentType.Code,
            Description = agentType.Description,
            Icon = agentType.Icon,
            DefaultSystemPrompt = agentType.DefaultSystemPrompt,
            IsEnabled = agentType.IsEnabled,
            SortOrder = agentType.SortOrder
        };
    }

    public static CollaborationVo ToVo(this Collaboration collaboration)
    {
        return new CollaborationVo
        {
            Id = collaboration.Id,
            Name = collaboration.Name,
            Description = collaboration.Description,
            Path = collaboration.Path,
            Status = collaboration.Status,
            UserId = collaboration.UserId,
            GitRepositoryUrl = collaboration.GitRepositoryUrl,
            GitBranch = collaboration.GitBranch,
            Config = collaboration.Config,
            CreatedAt = collaboration.CreatedAt
        };
    }

    public static CollaborationTaskVo ToVo(this CollaborationTask task)
    {
        return new CollaborationTaskVo
        {
            Id = task.Id,
            CollaborationId = task.CollaborationId,
            Title = task.Title,
            Description = task.Description,
            Prompt = task.Prompt,
            Status = task.Status,
            AssignedTo = task.AssignedTo,
            GitUrl = task.GitUrl,
            GitBranch = task.GitBranch,
            CompletedAt = task.CompletedAt,
            CreatedAt = task.CreatedAt
        };
    }

    public static LlmConfigVo ToVo(this LlmConfig config)
    {
        return new LlmConfigVo
        {
            Id = config.Id,
            Name = config.Name,
            Provider = config.Provider,
            ApiKey = config.ApiKey,
            Endpoint = config.Endpoint,
            DefaultModel = config.DefaultModel,
            UserId = config.UserId,
            IsDefault = config.IsDefault,
            IsEnabled = config.IsEnabled,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt,
            Models = config.Models?.Select(m => m.ToVo()).ToList() ?? new List<LlmModelConfigVo>()
        };
    }

    public static LlmModelConfigVo ToVo(this LlmModelConfig model)
    {
        return new LlmModelConfigVo
        {
            Id = model.Id,
            ModelName = model.ModelName,
            DisplayName = model.DisplayName,
            Description = model.Description,
            Temperature = model.Temperature,
            MaxTokens = model.MaxTokens,
            ContextWindow = model.ContextWindow,
            TopP = model.TopP,
            FrequencyPenalty = model.FrequencyPenalty,
            PresencePenalty = model.PresencePenalty,
            StopSequences = model.StopSequences,
            IsDefault = model.IsDefault,
            IsEnabled = model.IsEnabled,
            SortOrder = model.SortOrder,
            LastTestTime = model.LastTestTime,
            AvailabilityStatus = model.AvailabilityStatus,
            TestResult = model.TestResult,
            CreatedAt = model.CreatedAt
        };
    }
}
