using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Application.VOs;
using System.Text.Json;

namespace MAFStudio.Application.Mappers;

public static class EntityMapper
{
    public static AgentVo ToVo(this Agent agent)
    {
        List<FallbackModelVo>? fallbackModels = null;
        
        if (!string.IsNullOrEmpty(agent.FallbackModels))
        {
            try
            {
                var configs = JsonSerializer.Deserialize<List<Core.DTOs.FallbackModelConfig>>(agent.FallbackModels);
                if (configs != null)
                {
                    fallbackModels = configs.Select(c =>
                    {
                        var modelVo = new FallbackModelVo
                        {
                            LlmConfigId = c.LlmConfigId.ToString(),
                            LlmModelConfigId = c.LlmModelConfigId?.ToString(),
                            Priority = c.Priority
                        };
                        
                        Console.WriteLine($"[DEBUG] ToListItemVo - FallbackModel LlmConfigId: {c.LlmConfigId}, LlmModelConfigId: {c.LlmModelConfigId}");
                        Console.WriteLine($"[DEBUG] ToListItemVo - AllLlmConfigs count: {agent.AllLlmConfigs?.Count ?? 0}");
                        
                        if (agent.AllLlmConfigs != null)
                        {
                            var llmConfig = agent.AllLlmConfigs.FirstOrDefault(lc => lc.Id == c.LlmConfigId);
                            Console.WriteLine($"[DEBUG] ToListItemVo - Found llmConfig: {llmConfig?.Name ?? "null"}");
                            
                            if (llmConfig?.Models != null)
                            {
                                var model = llmConfig.Models.FirstOrDefault(m => m.Id == c.LlmModelConfigId);
                                Console.WriteLine($"[DEBUG] ToListItemVo - Found model: {model?.DisplayName ?? model?.ModelName ?? "null"}");
                                
                                if (model != null)
                                {
                                    modelVo.ModelName = model.DisplayName ?? model.ModelName;
                                    modelVo.LlmConfigName = llmConfig.Name;
                                }
                            }
                        }
                        
                        return modelVo;
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] ToListItemVo - Exception: {ex.Message}");
            }
        }
        
        string? primaryModelName = null;
        string? primaryLlmConfigName = null;
        
        if (agent.LlmModelConfigId.HasValue && agent.LlmConfig?.Models != null)
        {
            var model = agent.LlmConfig.Models.FirstOrDefault(m => m.Id == agent.LlmModelConfigId.Value);
            if (model != null)
            {
                primaryModelName = model.DisplayName ?? model.ModelName;
                primaryLlmConfigName = agent.LlmConfig.Name;
            }
        }
        
        return new AgentVo
        {
            Id = agent.Id.ToString(),
            Name = agent.Name,
            Description = agent.Description,
            Type = agent.Type,
            SystemPrompt = agent.SystemPrompt,
            Avatar = agent.Avatar,
            UserId = agent.UserId,
            Status = agent.Status,
            LlmConfigId = agent.LlmConfigId,
            LlmModelConfigId = agent.LlmModelConfigId,
            LlmConfigName = primaryLlmConfigName,
            PrimaryModelName = primaryModelName,
            CreatedAt = agent.CreatedAt,
            LlmConfig = agent.LlmConfig?.ToVo(),
            FallbackModels = fallbackModels
        };
    }

    public static AgentListItemVo ToListItemVo(this Agent agent)
    {
        string? primaryModelName = null;
        string? primaryLlmConfigName = null;
        
        if (agent.LlmModelConfigId.HasValue && agent.LlmConfig?.Models != null)
        {
            var model = agent.LlmConfig.Models.FirstOrDefault(m => m.Id == agent.LlmModelConfigId.Value);
            if (model != null)
            {
                primaryModelName = model.DisplayName ?? model.ModelName;
                primaryLlmConfigName = agent.LlmConfig.Name;
            }
        }
        
        List<FallbackModelVo>? fallbackModels = null;
        
        if (!string.IsNullOrEmpty(agent.FallbackModels))
        {
            try
            {
                var configs = JsonSerializer.Deserialize<List<Core.DTOs.FallbackModelConfig>>(agent.FallbackModels);
                Console.WriteLine($"[DEBUG] ToListItemVo - FallbackModels JSON: {agent.FallbackModels}");
                Console.WriteLine($"[DEBUG] ToListItemVo - Deserialized configs count: {configs?.Count ?? 0}");
                Console.WriteLine($"[DEBUG] ToListItemVo - AllLlmConfigs count: {agent.AllLlmConfigs?.Count ?? 0}");
                
                if (configs != null)
                {
                    fallbackModels = configs.Select(c =>
                    {
                        var modelVo = new FallbackModelVo
                        {
                            LlmConfigId = c.LlmConfigId.ToString(),
                            LlmModelConfigId = c.LlmModelConfigId?.ToString(),
                            Priority = c.Priority
                        };
                        
                        Console.WriteLine($"[DEBUG] ToListItemVo - Processing FallbackModel LlmConfigId: {c.LlmConfigId}, LlmModelConfigId: {c.LlmModelConfigId}");
                        
                        if (agent.AllLlmConfigs != null)
                        {
                            var llmConfig = agent.AllLlmConfigs.FirstOrDefault(lc => lc.Id == c.LlmConfigId);
                            Console.WriteLine($"[DEBUG] ToListItemVo - Found llmConfig: {llmConfig?.Name ?? "null"}, Models count: {llmConfig?.Models?.Count ?? 0}");
                            
                            if (llmConfig?.Models != null)
                            {
                                Console.WriteLine($"[DEBUG] ToListItemVo - Available model IDs: {string.Join(", ", llmConfig.Models.Select(m => m.Id))}");
                                Console.WriteLine($"[DEBUG] ToListItemVo - Looking for model ID: {c.LlmModelConfigId}");
                                
                                var model = llmConfig.Models.FirstOrDefault(m => m.Id == c.LlmModelConfigId);
                                Console.WriteLine($"[DEBUG] ToListItemVo - Found model: {model?.DisplayName ?? model?.ModelName ?? "null"}");
                                
                                if (model != null)
                                {
                                    modelVo.ModelName = model.DisplayName ?? model.ModelName;
                                    modelVo.LlmConfigName = llmConfig.Name;
                                }
                            }
                        }
                        
                        Console.WriteLine($"[DEBUG] ToListItemVo - Result: LlmConfigName={modelVo.LlmConfigName}, ModelName={modelVo.ModelName}");
                        
                        return modelVo;
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] ToListItemVo - Exception: {ex.Message}");
            }
        }
        
        return new AgentListItemVo
        {
            Id = agent.Id.ToString(),
            Name = agent.Name,
            Description = agent.Description,
            Type = agent.Type,
            Avatar = agent.Avatar,
            Status = agent.Status,
            LlmConfigId = agent.LlmConfigId?.ToString(),
            LlmModelConfigId = agent.LlmModelConfigId?.ToString(),
            LlmConfigName = primaryLlmConfigName,
            PrimaryModelName = primaryModelName,
            FallbackModels = fallbackModels,
            CreatedAt = agent.CreatedAt,
            SystemPrompt = agent.SystemPrompt
        };
    }

    public static AgentTypeVo ToVo(this AgentType agentType)
    {
        return new AgentTypeVo
        {
            Id = agentType.Id.ToString(),
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
            Id = collaboration.Id.ToString(),
            Name = collaboration.Name,
            Description = collaboration.Description,
            Path = collaboration.Path,
            Status = collaboration.Status,
            UserId = collaboration.UserId,
            GitRepositoryUrl = collaboration.GitRepositoryUrl,
            GitBranch = collaboration.GitBranch,
            CreatedAt = collaboration.CreatedAt
        };
    }

    public static CollaborationTaskVo ToVo(this CollaborationTask task)
    {
        return new CollaborationTaskVo
        {
            Id = task.Id.ToString(),
            CollaborationId = task.CollaborationId,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            AssignedTo = task.AssignedTo,
            CompletedAt = task.CompletedAt,
            CreatedAt = task.CreatedAt
        };
    }

    public static LlmConfigVo ToVo(this LlmConfig config)
    {
        return new LlmConfigVo
        {
            Id = config.Id.ToString(),
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
            Id = model.Id.ToString(),
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
            CreatedAt = model.CreatedAt
        };
    }
}
