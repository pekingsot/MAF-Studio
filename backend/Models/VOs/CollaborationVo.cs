namespace MAFStudio.Backend.Models.VOs
{
    /// <summary>
    /// 协作项目视图对象
    /// </summary>
    public class CollaborationVo : BaseVo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string? UpdatedAt { get; set; }
        public List<CollaborationAgentVo> Agents { get; set; } = new();
        public List<CollaborationTaskVo> Tasks { get; set; } = new();
    }

    /// <summary>
    /// 协作智能体视图对象
    /// </summary>
    public class CollaborationAgentVo : BaseVo
    {
        public Guid AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public string? AgentAvatar { get; set; }
        public string AgentType { get; set; } = string.Empty;
        public string JoinedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// 协作任务视图对象
    /// </summary>
    public class CollaborationTaskVo : BaseVo
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string? CompletedAt { get; set; }
    }

    /// <summary>
    /// 消息视图对象
    /// </summary>
    public class MessageVo : BaseVo
    {
        public Guid Id { get; set; }
        public string? FromAgentId { get; set; }
        public string FromAgentName { get; set; } = string.Empty;
        public string? FromAgentAvatar { get; set; }
        public string? ToAgentId { get; set; }
        public string? ToAgentName { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string SenderType { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public bool IsStreaming { get; set; }
    }
}
