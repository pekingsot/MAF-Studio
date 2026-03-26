using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Application.VOs;

namespace MAFStudio.Application.Mappers;

public static class EntityMapper
{
    public static AgentVo ToVo(this Agent agent)
    {
        return new AgentVo
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            Type = agent.Type,
            Configuration = agent.Configuration,
            Avatar = agent.Avatar,
            UserId = agent.UserId,
            Status = agent.Status,
            LlmConfigId = agent.LlmConfigId,
            LlmModelConfigId = agent.LlmModelConfigId,
            CreatedAt = agent.CreatedAt,
            LlmConfig = agent.LlmConfig?.ToVo()
        };
    }

    public static AgentListItemVo ToListItemVo(this Agent agent)
    {
        return new AgentListItemVo
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            Type = agent.Type,
            Avatar = agent.Avatar,
            Status = agent.Status,
            LlmConfigId = agent.LlmConfigId,
            LlmConfigName = agent.LlmConfig?.Name,
            CreatedAt = agent.CreatedAt
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
            Id = config.Id,
            Name = config.Name,
            Provider = config.Provider,
            Endpoint = config.Endpoint,
            DefaultModel = config.DefaultModel,
            UserId = config.UserId,
            CreatedAt = config.CreatedAt
        };
    }
}
