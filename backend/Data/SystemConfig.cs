using System.ComponentModel.DataAnnotations;

namespace MAFStudio.Backend.Data
{
    /// <summary>
    /// 系统配置实体
    /// 用于存储系统级别的配置信息
    /// </summary>
    public class SystemConfig
    {
        /// <summary>
        /// 配置ID
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// 配置键名
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 配置值
        /// </summary>
        [Required]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 配置描述
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// 配置分组
        /// </summary>
        [MaxLength(50)]
        public string? Group { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// 系统配置键常量
    /// </summary>
    public static class SystemConfigKeys
    {
        /// <summary>
        /// 向量化接口地址 (Infinity)
        /// </summary>
        public const string EmbeddingEndpoint = "embedding_endpoint";

        /// <summary>
        /// 查询重排序接口地址 (Infinity)
        /// </summary>
        public const string RerankEndpoint = "rerank_endpoint";

        /// <summary>
        /// 向量库接口地址 (Qdrant)
        /// </summary>
        public const string VectorDbEndpoint = "vector_db_endpoint";

        /// <summary>
        /// 向量库集合名称
        /// </summary>
        public const string VectorDbCollection = "vector_db_collection";

        /// <summary>
        /// 文本分割跳过的文件扩展名
        /// </summary>
        public const string SkipSplitExtensions = "skip_split_extensions";

        /// <summary>
        /// 默认文本分割方式
        /// </summary>
        public const string DefaultSplitMethod = "default_split_method";

        /// <summary>
        /// 默认分块大小
        /// </summary>
        public const string DefaultChunkSize = "default_chunk_size";

        /// <summary>
        /// 默认重叠大小
        /// </summary>
        public const string DefaultChunkOverlap = "default_chunk_overlap";
    }
}
