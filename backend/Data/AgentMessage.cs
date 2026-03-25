using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MAFStudio.Backend.Data
{
    public class AgentMessage
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public Guid FromAgentId { get; set; }

        /// <summary>
        /// 协作项目ID（可选）
        /// </summary>
        public Guid? CollaborationId { get; set; }
        
        [Required]
        public Guid ToAgentId { get; set; }
        
        [Required]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;
        
        [Required]
        public MessageType Type { get; set; } = MessageType.Text;
        
        [Required]
        public MessageStatus Status { get; set; } = MessageStatus.Pending;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ProcessedAt { get; set; }
        
        [ForeignKey("FromAgentId")]
        public virtual Agent FromAgent { get; set; } = null!;
        
        [ForeignKey("ToAgentId")]
        public virtual Agent ToAgent { get; set; } = null!;

        [ForeignKey("CollaborationId")]
        public virtual Collaboration? Collaboration { get; set; }
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