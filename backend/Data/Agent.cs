using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MAFStudio.Backend.Data
{
    public class Agent
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 大模型供应商配置ID
        /// </summary>
        public Guid? LLMConfigId { get; set; }

        /// <summary>
        /// 大模型供应商配置
        /// </summary>
        [ForeignKey("LLMConfigId")]
        public virtual LLMConfig? LLMConfig { get; set; }

        /// <summary>
        /// 大模型子配置ID (具体使用的模型)
        /// </summary>
        public Guid? LLMModelConfigId { get; set; }

        /// <summary>
        /// 大模型子配置
        /// </summary>
        [ForeignKey("LLMModelConfigId")]
        public virtual LLMModelConfig? LLMModelConfig { get; set; }
        
        [Required]
        public string Configuration { get; set; } = "{}";
        
        [MaxLength(500)]
        public string? Avatar { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        
        [Required]
        public AgentStatus Status { get; set; } = AgentStatus.Inactive;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public DateTime? LastActiveAt { get; set; }
        
        public virtual ICollection<AgentMessage> SentMessages { get; set; } = new List<AgentMessage>();
        
        public virtual ICollection<AgentMessage> ReceivedMessages { get; set; } = new List<AgentMessage>();
        
        public virtual ICollection<CollaborationAgent> Collaborations { get; set; } = new List<CollaborationAgent>();
    }

    public enum AgentStatus
    {
        Inactive,
        Active,
        Busy,
        Error
    }
}
