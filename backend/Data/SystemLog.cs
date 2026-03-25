using System.ComponentModel.DataAnnotations;

namespace MAFStudio.Backend.Data
{
    /// <summary>
    /// 系统日志实体
    /// 用于存储应用程序运行时的日志信息
    /// </summary>
    public class SystemLog
    {
        /// <summary>
        /// 日志ID
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 日志级别 (Trace, Debug, Information, Warning, Error, Critical)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Level { get; set; } = string.Empty;

        /// <summary>
        /// 日志类别/来源
        /// </summary>
        [MaxLength(500)]
        public string? Category { get; set; }

        /// <summary>
        /// 日志消息
        /// </summary>
        [Required]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 异常信息
        /// </summary>
        public string? Exception { get; set; }

        /// <summary>
        /// 异常堆栈跟踪
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// 请求路径
        /// </summary>
        [MaxLength(500)]
        public string? RequestPath { get; set; }

        /// <summary>
        /// 请求方法
        /// </summary>
        [MaxLength(10)]
        public string? RequestMethod { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [MaxLength(100)]
        public string? UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [MaxLength(100)]
        public string? UserName { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// 额外数据（JSON格式）
        /// </summary>
        public string? ExtraData { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
