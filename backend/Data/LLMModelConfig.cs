using System.ComponentModel.DataAnnotations;

namespace MAFStudio.Backend.Data
{
    /// <summary>
    /// 大模型子配置实体
    /// 每个供应商配置下可以有多个模型配置
    /// </summary>
    public class LLMModelConfig
    {
        /// <summary>
        /// 配置ID
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// 所属供应商配置ID
        /// </summary>
        public Guid LLMConfigId { get; set; }

        /// <summary>
        /// 所属供应商配置
        /// </summary>
        public LLMConfig LLMConfig { get; set; } = null!;

        /// <summary>
        /// 模型名称
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// 模型显示名称
        /// </summary>
        [MaxLength(100)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// 温度参数 (0-2)
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// 最大输出Token数
        /// </summary>
        public int MaxTokens { get; set; } = 4096;

        /// <summary>
        /// 上下文窗口大小
        /// </summary>
        public int ContextWindow { get; set; } = 8192;

        /// <summary>
        /// Top P参数
        /// </summary>
        public double? TopP { get; set; }

        /// <summary>
        /// 频率惩罚参数
        /// </summary>
        public double? FrequencyPenalty { get; set; }

        /// <summary>
        /// 存在惩罚参数
        /// </summary>
        public double? PresencePenalty { get; set; }

        /// <summary>
        /// 停止词序列 (JSON数组)
        /// </summary>
        public string? StopSequences { get; set; }

        /// <summary>
        /// 是否为默认模型
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
