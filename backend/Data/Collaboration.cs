using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MAFStudio.Backend.Data
{
    public class Collaboration
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [MaxLength(500)]
        public string? Path { get; set; }

        /// <summary>
        /// Git仓库地址
        /// </summary>
        [MaxLength(500)]
        public string? GitRepositoryUrl { get; set; }

        /// <summary>
        /// Git分支
        /// </summary>
        [MaxLength(100)]
        public string? GitBranch { get; set; } = "main";

        /// <summary>
        /// Git用户名
        /// </summary>
        [MaxLength(100)]
        public string? GitUsername { get; set; }

        /// <summary>
        /// Git邮箱
        /// </summary>
        [MaxLength(100)]
        public string? GitEmail { get; set; }

        /// <summary>
        /// Git访问令牌(加密存储)
        /// </summary>
        [MaxLength(500)]
        public string? GitAccessToken { get; set; }
        
        [Required]
        public CollaborationStatus Status { get; set; } = CollaborationStatus.Active;
        
        /// <summary>
        /// 创建者用户ID
        /// </summary>
        [MaxLength(100)]
        public string? UserId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public virtual ICollection<CollaborationAgent> Agents { get; set; } = new List<CollaborationAgent>();
        
        public virtual ICollection<CollaborationTask> Tasks { get; set; } = new List<CollaborationTask>();
    }

    public class CollaborationAgent
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public Guid CollaborationId { get; set; }
        
        [Required]
        public Guid AgentId { get; set; }
        
        [MaxLength(100)]
        public string? Role { get; set; }
        
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        
        [ForeignKey("CollaborationId")]
        public virtual Collaboration Collaboration { get; set; } = null!;
        
        [ForeignKey("AgentId")]
        public virtual Agent Agent { get; set; } = null!;
    }

    public class CollaborationTask
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public Guid CollaborationId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string? Description { get; set; }
        
        [Required]
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? CompletedAt { get; set; }
        
        [ForeignKey("CollaborationId")]
        public virtual Collaboration Collaboration { get; set; } = null!;
    }

    public enum CollaborationStatus
    {
        Active,
        Paused,
        Completed,
        Cancelled
    }

    public enum TaskStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }
}