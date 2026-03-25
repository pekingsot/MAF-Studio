using MAFStudio.Backend.Data;
using MAFStudio.Backend.Models;

namespace MAFStudio.Backend.Abstractions
{
    /// <summary>
    /// RAG服务接口
    /// 提供文档处理、文本分割、向量入库和检索功能
    /// </summary>
    public interface IRagService
    {
        /// <summary>
        /// 上传文档
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="content">文件内容</param>
        /// <param name="fileType">文件类型</param>
        /// <param name="splitMethod">分割方法</param>
        /// <param name="chunkSize">分块大小</param>
        /// <param name="chunkOverlap">分块重叠</param>
        /// <param name="skipExtensions">跳过的扩展名</param>
        /// <param name="userId">用户ID</param>
        /// <returns>创建的文档实体</returns>
        Task<RagDocument> UploadDocumentAsync(string fileName, string content, string? fileType, string? splitMethod, int? chunkSize, int? chunkOverlap, string? skipExtensions = null, string? userId = null);

        /// <summary>
        /// 获取所有文档
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>文档列表</returns>
        Task<List<RagDocument>> GetAllDocumentsAsync(string? userId = null);

        /// <summary>
        /// 获取文档详情
        /// </summary>
        /// <param name="id">文档ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>文档实体，不存在返回null</returns>
        Task<RagDocument?> GetDocumentByIdAsync(Guid id, string? userId = null);

        /// <summary>
        /// 获取文档分块
        /// </summary>
        /// <param name="documentId">文档ID</param>
        /// <returns>分块列表</returns>
        Task<List<RagDocumentChunk>> GetDocumentChunksAsync(Guid documentId);

        /// <summary>
        /// 删除文档
        /// </summary>
        /// <param name="id">文档ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteDocumentAsync(Guid id, string? userId = null);

        /// <summary>
        /// 分割文本
        /// </summary>
        /// <param name="text">待分割文本</param>
        /// <param name="method">分割方法</param>
        /// <param name="chunkSize">分块大小</param>
        /// <param name="chunkOverlap">分块重叠</param>
        /// <returns>分割后的文本块列表</returns>
        List<string> SplitText(string text, string method, int chunkSize, int chunkOverlap);

        /// <summary>
        /// 测试文本分割
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="splitMethod">分割方法</param>
        /// <param name="chunkSize">分块大小</param>
        /// <param name="chunkOverlap">分块重叠</param>
        /// <returns>包含分块结果的文档实体</returns>
        Task<RagDocument> TestSplitAsync(string content, string splitMethod, int chunkSize, int chunkOverlap);

        /// <summary>
        /// 向量入库
        /// </summary>
        /// <param name="documentId">文档ID</param>
        /// <param name="vectorizationEndpoint">向量化端点</param>
        /// <param name="vectorDbEndpoint">向量数据库端点</param>
        /// <param name="collection">集合名称</param>
        /// <param name="userId">用户ID</param>
        /// <returns>入库的向量数量</returns>
        Task<int> VectorizeDocumentAsync(Guid documentId, string vectorizationEndpoint, string vectorDbEndpoint, string collection, string? userId = null);

        /// <summary>
        /// RAG检索
        /// </summary>
        /// <param name="query">查询文本</param>
        /// <param name="vectorizationEndpoint">向量化端点</param>
        /// <param name="vectorDbEndpoint">向量数据库端点</param>
        /// <param name="collection">集合名称</param>
        /// <param name="llmConfig">大模型配置</param>
        /// <param name="modelConfig">模型配置</param>
        /// <param name="topK">返回数量</param>
        /// <param name="scoreThreshold">分数阈值</param>
        /// <param name="systemPrompt">系统提示词</param>
        /// <returns>检索结果</returns>
        Task<RagQueryResult> RagQueryAsync(string query, string vectorizationEndpoint, string vectorDbEndpoint, string collection, LLMConfig llmConfig, LLMModelConfig? modelConfig, int topK, double scoreThreshold, string? systemPrompt);

        /// <summary>
        /// 查询向量文档列表
        /// </summary>
        /// <param name="vectorDbEndpoint">向量数据库端点</param>
        /// <param name="collection">集合名称</param>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">搜索关键词</param>
        /// <returns>向量文档列表结果</returns>
        Task<VectorDocumentsResult> GetVectorDocumentsAsync(string vectorDbEndpoint, string collection, int page, int pageSize, string? keyword);

        /// <summary>
        /// 删除向量文档
        /// </summary>
        /// <param name="vectorDbEndpoint">向量数据库端点</param>
        /// <param name="collection">集合名称</param>
        /// <param name="vectorId">向量ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteVectorDocumentAsync(string vectorDbEndpoint, string collection, string vectorId);
    }
}
