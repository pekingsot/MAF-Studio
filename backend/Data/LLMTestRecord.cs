using System.ComponentModel.DataAnnotations;

namespace MAFStudio.Backend.Data
{
    /// <summary>
    /// 大模型测试记录实体
    /// 记录每次连通性测试的结果
    /// </summary>
    public class LLMTestRecord
    {
        /// <summary>
        /// 记录ID
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// 供应商配置ID
        /// </summary>
        public Guid LLMConfigId { get; set; }

        /// <summary>
        /// 供应商配置
        /// </summary>
        public LLMConfig LLMConfig { get; set; } = null!;

        /// <summary>
        /// 模型配置ID (可选)
        /// </summary>
        public Guid? LLMModelConfigId { get; set; }

        /// <summary>
        /// 模型配置
        /// </summary>
        public LLMModelConfig? LLMModelConfig { get; set; }

        /// <summary>
        /// 供应商类型 (qwen, openai, zhipu等)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// 测试的模型名称
        /// </summary>
        [MaxLength(100)]
        public string? ModelName { get; set; }

        /// <summary>
        /// 测试是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 测试消息
        /// </summary>
        [MaxLength(500)]
        public string? Message { get; set; }

        /// <summary>
        /// 响应延迟(毫秒)
        /// </summary>
        public int LatencyMs { get; set; }

        /// <summary>
        /// 测试时间
        /// </summary>
        public DateTime TestedAt { get; set; } = DateTime.UtcNow;
    }
}
