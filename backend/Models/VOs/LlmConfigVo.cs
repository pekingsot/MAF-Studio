namespace MAFStudio.Backend.Models.VOs
{
    /// <summary>
    /// LLM配置视图对象
    /// </summary>
    public class LlmConfigVo : BaseVo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? Endpoint { get; set; }
        public bool IsDefault { get; set; }
        public bool IsEnabled { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? UpdatedAt { get; set; }
        public List<LlmModelConfigVo> Models { get; set; } = new();
    }

    /// <summary>
    /// LLM模配置视图对象
    /// </summary>
    public class LlmModelConfigVo : BaseVo
    {
        public Guid Id { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public double Temperature { get; set; }
        public int MaxTokens { get; set; }
        public int ContextWindow { get; set; }
        public bool IsDefault { get; set; }
        public bool IsEnabled { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? UpdatedAt { get; set; }
    }
}
