using Microsoft.EntityFrameworkCore;

namespace MAFStudio.Backend.Data
{
    /// <summary>
    /// 应用程序数据库上下文
    /// 管理所有实体的数据库映射
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// 用户表
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// 智能体表
        /// </summary>
        public DbSet<Agent> Agents { get; set; }

        /// <summary>
        /// 智能体类型表
        /// </summary>
        public DbSet<AgentType> AgentTypes { get; set; }

        /// <summary>
        /// 智能体消息表
        /// </summary>
        public DbSet<AgentMessage> AgentMessages { get; set; }

        /// <summary>
        /// 协作项目表
        /// </summary>
        public DbSet<Collaboration> Collaborations { get; set; }

        /// <summary>
        /// 协作智能体关联表
        /// </summary>
        public DbSet<CollaborationAgent> CollaborationAgents { get; set; }

        /// <summary>
        /// 协作任务表
        /// </summary>
        public DbSet<CollaborationTask> CollaborationTasks { get; set; }

        /// <summary>
        /// 操作日志表
        /// </summary>
        public DbSet<OperationLog> OperationLogs { get; set; }

        /// <summary>
        /// 大模型配置表
        /// </summary>
        public DbSet<LLMConfig> LLMConfigs { get; set; }

        /// <summary>
        /// 大模型子配置表
        /// </summary>
        public DbSet<LLMModelConfig> LLMModelConfigs { get; set; }

        /// <summary>
        /// 大模型测试记录表
        /// </summary>
        public DbSet<LLMTestRecord> LLMTestRecords { get; set; }

        /// <summary>
        /// 系统配置表
        /// </summary>
        public DbSet<SystemConfig> SystemConfigs { get; set; }

        /// <summary>
        /// RAG文档表
        /// </summary>
        public DbSet<RagDocument> RagDocuments { get; set; }

        /// <summary>
        /// RAG文档分块表
        /// </summary>
        public DbSet<RagDocumentChunk> RagDocumentChunks { get; set; }

        /// <summary>
        /// 系统日志表
        /// </summary>
        public DbSet<SystemLog> SystemLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 用户表索引配置
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // 智能体表关系配置
            modelBuilder.Entity<Agent>()
                .HasOne(a => a.User)
                .WithMany(u => u.Agents)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Agent>()
                .HasIndex(a => a.Name);

            // 智能体类型表配置
            modelBuilder.Entity<AgentType>()
                .HasIndex(t => t.Code)
                .IsUnique();

            // 智能体消息表关系配置
            modelBuilder.Entity<AgentMessage>()
                .HasOne(am => am.FromAgent)
                .WithMany(a => a.SentMessages)
                .HasForeignKey(am => am.FromAgentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AgentMessage>()
                .HasOne(am => am.ToAgent)
                .WithMany(a => a.ReceivedMessages)
                .HasForeignKey(am => am.ToAgentId)
                .OnDelete(DeleteBehavior.Restrict);

            // 协作智能体关联表配置
            modelBuilder.Entity<CollaborationAgent>()
                .HasIndex(ca => new { ca.CollaborationId, ca.AgentId })
                .IsUnique();

            modelBuilder.Entity<CollaborationAgent>()
                .HasOne(ca => ca.Collaboration)
                .WithMany(c => c.Agents)
                .HasForeignKey(ca => ca.CollaborationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CollaborationAgent>()
                .HasOne(ca => ca.Agent)
                .WithMany(a => a.Collaborations)
                .HasForeignKey(ca => ca.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            // 协作任务表关系配置
            modelBuilder.Entity<CollaborationTask>()
                .HasOne(ct => ct.Collaboration)
                .WithMany(c => c.Tasks)
                .HasForeignKey(ct => ct.CollaborationId)
                .OnDelete(DeleteBehavior.Cascade);

            // 操作日志表配置
            modelBuilder.Entity<OperationLog>()
                .HasIndex(ol => ol.UserId);

            modelBuilder.Entity<OperationLog>()
                .HasIndex(ol => ol.CreatedAt);

            // 大模型配置表配置
            modelBuilder.Entity<LLMConfig>()
                .HasIndex(l => l.Name);

            modelBuilder.Entity<LLMConfig>()
                .HasIndex(l => l.Provider);

            // 大模型子配置表配置
            modelBuilder.Entity<LLMModelConfig>()
                .HasOne(m => m.LLMConfig)
                .WithMany(l => l.Models)
                .HasForeignKey(m => m.LLMConfigId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LLMModelConfig>()
                .HasIndex(m => m.LLMConfigId);

            modelBuilder.Entity<LLMModelConfig>()
                .HasIndex(m => m.ModelName);

            // 大模型测试记录表配置
            modelBuilder.Entity<LLMTestRecord>()
                .HasOne(r => r.LLMConfig)
                .WithMany(l => l.TestRecords)
                .HasForeignKey(r => r.LLMConfigId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LLMTestRecord>()
                .HasOne(r => r.LLMModelConfig)
                .WithMany()
                .HasForeignKey(r => r.LLMModelConfigId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<LLMTestRecord>()
                .HasIndex(r => r.LLMConfigId);

            modelBuilder.Entity<LLMTestRecord>()
                .HasIndex(r => r.TestedAt);

            // 系统配置表配置
            modelBuilder.Entity<SystemConfig>()
                .HasIndex(s => s.Key)
                .IsUnique();

            // RAG文档表配置
            modelBuilder.Entity<RagDocument>()
                .HasIndex(d => d.FileName);

            modelBuilder.Entity<RagDocument>()
                .HasIndex(d => d.Status);

            // RAG文档分块表配置
            modelBuilder.Entity<RagDocumentChunk>()
                .HasOne(c => c.Document)
                .WithMany(d => d.Chunks)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RagDocumentChunk>()
                .HasIndex(c => c.DocumentId);

            // 系统日志表配置
            modelBuilder.Entity<SystemLog>()
                .HasIndex(s => s.Level);

            modelBuilder.Entity<SystemLog>()
                .HasIndex(s => s.Category);

            modelBuilder.Entity<SystemLog>()
                .HasIndex(s => s.CreatedAt);
        }
    }
}