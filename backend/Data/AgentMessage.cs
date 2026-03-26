using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MAFStudio.Backend.Data
{
    public class AgentMessage
    {
        [Key]
        public Guid Id { get; set; }
        
        /// <summary>
        /// 发送者智能体ID（用户消息时为空）
        /// </summary>
        public Guid? FromAgentId { get; set; }

        /// <summary>
        /// 发送者类型：User=用户，Agent=智能体
        /// </summary>
        [Required]
        public SenderType SenderType { get; set; } = SenderType.Agent;

        /// <summary>
        /// 发送者名称（用户消息时存储用户名）
        /// </summary>
        [MaxLength(100)]
        public string? SenderName { get; set; }

        /// <summary>
        /// 协作项目ID（可选）
        /// </summary>
        public Guid? CollaborationId { get; set; }
        
        /// <summary>
        /// 目标智能体ID（空表示广播给所有智能体）
        /// </summary>
        public Guid? ToAgentId { get; set; }

        /// <summary>
        /// 提及的智能体ID列表（@提及功能）
        /// </summary>
        public string? MentionedAgentIds { get; set; }
        
        [Required]
        [MaxLength(10000)]
        public string Content { get; set; } = string.Empty;
        
        [Required]
        public MessageType Type { get; set; } = MessageType.Text;
        
        [Required]
        public MessageStatus Status { get; set; } = MessageStatus.Pending;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ProcessedAt { get; set; }
        
        [ForeignKey("FromAgentId")]
        public virtual Agent? FromAgent { get; set; }
        
        [ForeignKey("ToAgentId")]
        public virtual Agent? ToAgent { get; set; }

        [ForeignKey("CollaborationId")]
        public virtual Collaboration? Collaboration { get; set; }
    }

    public enum SenderType
    {
        User,
        Agent
    }

    public enum MessageType
    {
        Text,
        Command,
        Query,
        Response,
        Error
    }

    public enum MessageStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }
}