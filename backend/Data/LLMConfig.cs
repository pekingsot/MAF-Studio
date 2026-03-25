using System.ComponentModel.DataAnnotations;

namespace MAFStudio.Backend.Data
{
    /// <summary>
    /// 大模型供应商配置实体
    /// 用于存储不同AI供应商的配置信息，每个供应商可以有多个模型配置
    /// </summary>
    public class LLMConfig
    {
        /// <summary>
        /// 配置ID
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// 配置名称
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 模型提供商 (openai, deepseek, qwen等)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// API密钥
        /// </summary>
        [Required]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// API端点地址
        /// </summary>
        [MaxLength(500)]
        public string? Endpoint { get; set; }

        /// <summary>
        /// 是否为默认配置
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建者用户ID
        /// </summary>
        [MaxLength(100)]
        public string? UserId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 子模型配置列表
        /// </summary>
        public ICollection<LLMModelConfig> Models { get; set; } = new List<LLMModelConfig>();

        /// <summary>
        /// 测试记录列表
        /// </summary>
        public ICollection<LLMTestRecord> TestRecords { get; set; } = new List<LLMTestRecord>();
    }
}
