namespace MAFStudio.Backend.Models.Requests
{
    /// <summary>
    /// 上传文档请求
    /// </summary>
    public class UploadDocumentRequest
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 文件类型
        /// </summary>
        public string? FileType { get; set; }

        /// <summary>
        /// 分割方法
        /// </summary>
        public string? SplitMethod { get; set; }

        /// <summary>
        /// 分块大小
        /// </summary>
        public int? ChunkSize { get; set; }

        /// <summary>
        /// 分块重叠
        /// </summary>
        public int? ChunkOverlap { get; set; }
    }

    /// <summary>
    /// 测试分割请求
    /// </summary>
    public class TestSplitRequest
    {
        /// <summary>
        /// 文本内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 分割方法
        /// </summary>
        public string? SplitMethod { get; set; }

        /// <summary>
        /// 分块大小
        /// </summary>
        public int? ChunkSize { get; set; }

        /// <summary>
        /// 分块重叠
        /// </summary>
        public int? ChunkOverlap { get; set; }
    }

    /// <summary>
    /// RAG检索请求
    /// </summary>
    public class RagQueryRequest
    {
        /// <summary>
        /// 查询文本
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// 大模型配置ID
        /// </summary>
        public Guid LlmConfigId { get; set; }

        /// <summary>
        /// 模型配置ID
        /// </summary>
        public Guid? LlmModelConfigId { get; set; }

        /// <summary>
        /// 返回数量
        /// </summary>
        public int TopK { get; set; } = 5;

        /// <summary>
        /// 分数阈值
        /// </summary>
        public double ScoreThreshold { get; set; } = 0.5;

        /// <summary>
        /// 系统提示词
        /// </summary>
        public string? SystemPrompt { get; set; }
    }

    /// <summary>
    /// 向量文档查询请求
    /// </summary>
    public class VectorDocumentQueryRequest
    {
        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页数量
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string? Keyword { get; set; }
    }
}
