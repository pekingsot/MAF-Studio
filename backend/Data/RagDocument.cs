using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MAFStudio.Backend.Data
{
    /// <summary>
    /// RAG文档实体
    /// 用于存储上传的文档信息
    /// </summary>
    public class RagDocument
    {
        /// <summary>
        /// 文档ID
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// 文档名称
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 原始文件名
        /// </summary>
        [MaxLength(500)]
        public string? OriginalFileName { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        [MaxLength(1000)]
        public string? FilePath { get; set; }

        /// <summary>
        /// 文件类型/扩展名
        /// </summary>
        [MaxLength(50)]
        public string? FileType { get; set; }

        /// <summary>
        /// 文件大小(字节)
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 文件内容(MD5)
        /// </summary>
        [MaxLength(100)]
        public string? ContentHash { get; set; }

        /// <summary>
        /// 分割方式 (character, recursive, semantic, custom)
        /// </summary>
        [MaxLength(50)]
        public string? SplitMethod { get; set; }

        /// <summary>
        /// 分块大小
        /// </summary>
        public int? ChunkSize { get; set; }

        /// <summary>
        /// 分块重叠大小
        /// </summary>
        public int? ChunkOverlap { get; set; }

        /// <summary>
        /// 分块数量
        /// </summary>
        public int ChunkCount { get; set; }

        /// <summary>
        /// 处理状态
        /// </summary>
        public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 创建者用户ID
        /// </summary>
        [MaxLength(100)]
        public string? UserId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 文档分块列表
        /// </summary>
        public virtual ICollection<RagDocumentChunk> Chunks { get; set; } = new List<RagDocumentChunk>();
    }

    /// <summary>
    /// 文档处理状态
    /// </summary>
    public enum DocumentStatus
    {
        /// <summary>
        /// 待处理
        /// </summary>
        Pending,
        /// <summary>
        /// 处理中
        /// </summary>
        Processing,
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        /// <summary>
        /// 失败
        /// </summary>
        Failed
    }

    /// <summary>
    /// RAG文档分块实体
    /// 存储文档分割后的文本块
    /// </summary>
    public class RagDocumentChunk
    {
        /// <summary>
        /// 分块ID
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// 所属文档ID
        /// </summary>
        public Guid DocumentId { get; set; }

        /// <summary>
        /// 所属文档
        /// </summary>
        [ForeignKey("DocumentId")]
        public virtual RagDocument? Document { get; set; }

        /// <summary>
        /// 分块内容
        /// </summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 分块索引
        /// </summary>
        public int ChunkIndex { get; set; }

        /// <summary>
        /// 向量ID (在向量库中的ID)
        /// </summary>
        [MaxLength(100)]
        public string? VectorId { get; set; }

        /// <summary>
        /// 元数据 (JSON)
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 文本分割方式
    /// </summary>
    public enum SplitMethod
    {
        /// <summary>
        /// 按字符数分割
        /// </summary>
        Character,
        /// <summary>
        /// 递归分割 (按段落、句子、词)
        /// </summary>
        Recursive,
        /// <summary>
        /// 语义分割
        /// </summary>
        Semantic,
        /// <summary>
        /// 按分隔符分割
        /// </summary>
        Separator
    }
}
