namespace MAFStudio.Backend.Models.VOs
{
    /// <summary>
    /// 智能体视图对象
    /// </summary>
    public class AgentVo : BaseVo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public Guid? LlmConfigId { get; set; }
        public string? LlmConfigName { get; set; }
        public string Configuration { get; set; } = "{}";
        public string CreatedAt { get; set; } = string.Empty;
        public string? UpdatedAt { get; set; }
        public bool IsEnabled { get; set; }
    }

    /// <summary>
    /// 智能体列表项视图对象
    /// </summary>
    public class AgentListItemVo : BaseVo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? LlmConfigName { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }
}
