using System.ComponentModel.DataAnnotations;

namespace MAFStudio.Backend.Data
{
    /// <summary>
    /// 智能体类型实体
    /// 定义智能体的类型及其默认配置
    /// </summary>
    public class AgentType
    {
        /// <summary>
        /// 类型ID
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// 类型编码（唯一标识）
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 类型名称
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 类型描述
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// 默认系统提示词
        /// </summary>
        public string? DefaultSystemPrompt { get; set; }

        /// <summary>
        /// 默认温度参数
        /// </summary>
        public double DefaultTemperature { get; set; } = 0.7;

        /// <summary>
        /// 默认最大Token数
        /// </summary>
        public int DefaultMaxTokens { get; set; } = 4096;

        /// <summary>
        /// 图标
        /// </summary>
        [MaxLength(50)]
        public string? Icon { get; set; }

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// 是否为系统内置类型
        /// </summary>
        public bool IsSystem { get; set; } = false;

        /// <summary>
        /// 创建者用户ID (系统类型为null，所有人可见)
        /// </summary>
        [MaxLength(100)]
        public string? UserId { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

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
