using System.ComponentModel.DataAnnotations;

namespace MAFStudio.Backend.Data
{
    /// <summary>
    /// 操作日志实体
    /// 记录系统中的所有操作
    /// </summary>
    public class OperationLog
    {
        /// <summary>
        /// 日志ID
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// 操作类型 (Create, Update, Delete, Query等)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// 操作模块 (Agent, Collaboration, LLMConfig等)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Module { get; set; } = string.Empty;

        /// <summary>
        /// 操作描述
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// 操作用户ID
        /// </summary>
        [MaxLength(100)]
        public string? UserId { get; set; }

        /// <summary>
        /// 操作用户名
        /// </summary>
        [MaxLength(100)]
        public string? UserName { get; set; }

        /// <summary>
        /// 操作IP地址
        /// </summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// 请求参数 (JSON)
        /// </summary>
        public string? RequestData { get; set; }

        /// <summary>
        /// 响应结果 (JSON)
        /// </summary>
        public string? ResponseData { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; } = true;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 执行耗时(毫秒)
        /// </summary>
        public long? Duration { get; set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
