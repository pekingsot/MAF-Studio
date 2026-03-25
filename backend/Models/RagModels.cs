namespace MAFStudio.Backend.Models
{
    /// <summary>
    /// RAG检索结果
    /// </summary>
    public class RagQueryResult
    {
        /// <summary>
        /// AI回答
        /// </summary>
        public string Answer { get; set; } = string.Empty;

        /// <summary>
        /// 相关文档片段
        /// </summary>
        public List<RagSource> Sources { get; set; } = new();
    }

    /// <summary>
    /// RAG来源文档
    /// </summary>
    public class RagSource
    {
        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 相似度分数
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// 文档ID
        /// </summary>
        public string? DocumentId { get; set; }
    }

    /// <summary>
    /// 向量文档查询结果
    /// </summary>
    public class VectorDocumentsResult
    {
        /// <summary>
        /// 文档列表
        /// </summary>
        public List<VectorDocumentItem> Items { get; set; } = new();

        /// <summary>
        /// 总数
        /// </summary>
        public int Total { get; set; }
    }

    /// <summary>
    /// 向量文档项
    /// </summary>
    public class VectorDocumentItem
    {
        /// <summary>
        /// 向量ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 文档ID
        /// </summary>
        public string? DocumentId { get; set; }

        /// <summary>
        /// 分块序号
        /// </summary>
        public int? ChunkIndex { get; set; }
    }
}
