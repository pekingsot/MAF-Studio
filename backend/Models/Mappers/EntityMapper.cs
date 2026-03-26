using MAFStudio.Backend.Data;
using MAFStudio.Backend.Models.VOs;

namespace MAFStudio.Backend.Models.Mappers
{
    /// <summary>
    /// 对象映射工具类
    /// 负责实体对象和VO之间的转换
    /// </summary>
    public static class EntityMapper
    {
        /// <summary>
        /// Agent 实体转 VO
        /// </summary>
        public static AgentVo ToVo(this Agent agent)
        {
            return new AgentVo
            {
                Id = agent.Id,
                Name = agent.Name,
                Description = agent.Description,
                Type = agent.Type,
                Avatar = agent.Avatar,
                LlmConfigId = agent.LLMConfigId,
                LlmConfigName = agent.LLMConfig?.Name,
                Configuration = agent.Configuration,
                CreatedAt = BaseVo.FormatDateTime(agent.CreatedAt),
                UpdatedAt = BaseVo.FormatDateTime(agent.UpdatedAt),
                IsEnabled = agent.Status == AgentStatus.Active
            };
        }

        /// <summary>
        /// Agent 实体转列表项 VO
        /// </summary>
        public static AgentListItemVo ToListItemVo(this Agent agent)
        {
            return new AgentListItemVo
            {
                Id = agent.Id,
                Name = agent.Name,
                Description = agent.Description,
                Type = agent.Type,
                Avatar = agent.Avatar,
                LlmConfigName = agent.LLMConfig?.Name,
                CreatedAt = BaseVo.FormatDateTime(agent.CreatedAt),
                IsEnabled = agent.Status == AgentStatus.Active
            };
        }

        /// <summary>
        /// Collaboration 实体转 VO
        /// </summary>
        public static CollaborationVo ToVo(this Collaboration collaboration, List<CollaborationAgent>? agents = null, List<CollaborationTask>? tasks = null)
        {
            return new CollaborationVo
            {
                Id = collaboration.Id,
                Name = collaboration.Name,
                Description = collaboration.Description,
                Status = collaboration.Status.ToString(),
                CreatedAt = BaseVo.FormatDateTime(collaboration.CreatedAt),
                UpdatedAt = BaseVo.FormatDateTime(collaboration.UpdatedAt),
                Agents = agents?.Select(a => a.ToVo()).ToList() ?? new List<CollaborationAgentVo>(),
                Tasks = tasks?.Select(t => t.ToVo()).ToList() ?? new List<CollaborationTaskVo>()
            };
        }

        /// <summary>
        /// CollaborationAgent 实体转 VO
        /// </summary>
        public static CollaborationAgentVo ToVo(this CollaborationAgent collaborationAgent)
        {
            return new CollaborationAgentVo
            {
                AgentId = collaborationAgent.AgentId,
                AgentName = collaborationAgent.Agent?.Name ?? "未知",
                AgentAvatar = collaborationAgent.Agent?.Avatar,
                AgentType = collaborationAgent.Agent?.Type ?? "Unknown",
                JoinedAt = BaseVo.FormatDateTime(collaborationAgent.JoinedAt)
            };
        }

        /// <summary>
        /// CollaborationTask 实体转 VO
        /// </summary>
        public static CollaborationTaskVo ToVo(this CollaborationTask task)
        {
            return new CollaborationTaskVo
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status.ToString(),
                CreatedAt = BaseVo.FormatDateTime(task.CreatedAt),
                CompletedAt = BaseVo.FormatDateTime(task.CompletedAt)
            };
        }

        /// <summary>
        /// AgentMessage 实体转 VO
        /// </summary>
        public static MessageVo ToVo(this AgentMessage message)
        {
            return new MessageVo
            {
                Id = message.Id,
                FromAgentId = message.FromAgentId?.ToString(),
                FromAgentName = message.SenderName ?? message.FromAgent?.Name ?? "未知",
                FromAgentAvatar = message.FromAgent?.Avatar,
                ToAgentId = message.ToAgentId?.ToString(),
                ToAgentName = message.ToAgent?.Name,
                Content = message.Content,
                Type = message.Type.ToString(),
                SenderType = message.SenderType.ToString(),
                Timestamp = BaseVo.FormatDateTime(message.CreatedAt),
                IsStreaming = false
            };
        }

        /// <summary>
        /// LLMConfig 实体转 VO
        /// </summary>
        public static LlmConfigVo ToVo(this LLMConfig config)
        {
            return new LlmConfigVo
            {
                Id = config.Id,
                Name = config.Name,
                Provider = config.Provider,
                Endpoint = config.Endpoint,
                IsDefault = config.IsDefault,
                IsEnabled = config.IsEnabled,
                CreatedAt = BaseVo.FormatDateTime(config.CreatedAt),
                UpdatedAt = BaseVo.FormatDateTime(config.UpdatedAt),
                Models = config.Models?.Select(m => m.ToVo()).ToList() ?? new List<LlmModelConfigVo>()
            };
        }

        /// <summary>
        /// LLMModelConfig 实体转 VO
        /// </summary>
        public static LlmModelConfigVo ToVo(this LLMModelConfig model)
        {
            return new LlmModelConfigVo
            {
                Id = model.Id,
                ModelName = model.ModelName,
                DisplayName = model.DisplayName,
                Temperature = model.Temperature,
                MaxTokens = model.MaxTokens,
                ContextWindow = model.ContextWindow,
                IsDefault = model.IsDefault,
                IsEnabled = model.IsEnabled,
                CreatedAt = BaseVo.FormatDateTime(model.CreatedAt),
                UpdatedAt = BaseVo.FormatDateTime(model.UpdatedAt)
            };
        }
    }
}
