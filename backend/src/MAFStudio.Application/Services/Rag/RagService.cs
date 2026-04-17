using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Configuration;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace MAFStudio.Application.Services.Rag;

public class RagService : IRagService
{
    private readonly IRagDocumentRepository _documentRepo;
    private readonly IRagDocumentChunkRepository _chunkRepo;
    private readonly ISystemConfigRepository _configRepo;
    private readonly IEmbeddingService _embeddingService;
    private readonly IRerankService _rerankService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly ITextSplitterService _textSplitterService;
    private readonly IChatService _chatService;
    private readonly ILlmConfigService _llmConfigService;
    private readonly IOptions<WorkspaceOptions> _workspaceOptions;
    private readonly ILogger<RagService> _logger;

    public RagService(
        IRagDocumentRepository documentRepo,
        IRagDocumentChunkRepository chunkRepo,
        ISystemConfigRepository configRepo,
        IEmbeddingService embeddingService,
        IRerankService rerankService,
        IVectorStoreService vectorStoreService,
        ITextSplitterService textSplitterService,
        IChatService chatService,
        ILlmConfigService llmConfigService,
        IOptions<WorkspaceOptions> workspaceOptions,
        ILogger<RagService> logger)
    {
        _documentRepo = documentRepo;
        _chunkRepo = chunkRepo;
        _configRepo = configRepo;
        _embeddingService = embeddingService;
        _rerankService = rerankService;
        _vectorStoreService = vectorStoreService;
        _textSplitterService = textSplitterService;
        _chatService = chatService;
        _llmConfigService = llmConfigService;
        _workspaceOptions = workspaceOptions;
        _logger = logger;
    }

    public async Task<RagDocument> UploadDocumentAsync(string fileName, string? filePath, string? fileType, long fileSize, long userId, string? splitMethod = null, int? chunkSize = null, int? chunkOverlap = null)
    {
        var document = new RagDocument
        {
            FileName = fileName,
            FilePath = filePath,
            FileType = fileType ?? Path.GetExtension(fileName).TrimStart('.'),
            FileSize = fileSize,
            Status = RagDocumentStatus.Pending,
            UserId = userId,
            SplitMethod = splitMethod,
            ChunkSize = chunkSize,
            ChunkOverlap = chunkOverlap,
        };

        return await _documentRepo.CreateAsync(document);
    }

    public async Task<List<RagDocument>> GetDocumentsAsync(long userId)
    {
        return await _documentRepo.GetByUserIdAsync(userId.ToString());
    }

    public async Task<RagDocument?> GetDocumentAsync(long id)
    {
        return await _documentRepo.GetByIdAsync(id);
    }

    public async Task<bool> DeleteDocumentAsync(long id)
    {
        var doc = await _documentRepo.GetByIdAsync(id);
        if (doc == null) return false;

        await _chunkRepo.DeleteByDocumentIdAsync(id);

        var collectionName = await GetCollectionName();
        try
        {
            await _vectorStoreService.DeleteAsync(collectionName, id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从向量库删除文档向量失败: DocId={DocId}", id);
        }

        return await _documentRepo.DeleteAsync(id);
    }

    public async Task<List<RagDocumentChunk>> GetChunksAsync(long documentId)
    {
        return await _chunkRepo.GetByDocumentIdAsync(documentId);
    }

    public async Task<List<TextChunk>> TestSplitAsync(string content, string? method = null, int? chunkSize = null, int? chunkOverlap = null)
    {
        return _textSplitterService.Split(content, method, chunkSize, chunkOverlap);
    }

    public async Task<VectorizeResult> VectorizeAsync(long documentId)
    {
        var doc = await _documentRepo.GetByIdAsync(documentId);
        if (doc == null) throw new InvalidOperationException("文档不存在");

        doc.Status = RagDocumentStatus.Processing;
        await _documentRepo.UpdateAsync(doc);

        try
        {
            var content = await ReadDocumentContent(doc);
            if (string.IsNullOrWhiteSpace(content))
            {
                doc.Status = RagDocumentStatus.Failed;
                doc.ErrorMessage = "文档内容为空";
                await _documentRepo.UpdateAsync(doc);
                return new VectorizeResult { SuccessCount = 0, ErrorMessage = "文档内容为空" };
            }

            var method = await GetConfigValue("default_split_method", "recursive");
            var chunkSize = int.Parse(await GetConfigValue("default_chunk_size", "500"));
            var chunkOverlap = int.Parse(await GetConfigValue("default_chunk_overlap", "50"));

            var skipExtensions = await GetConfigValue("skip_extensions", "");
            var ext = Path.GetExtension(doc.FileName).ToLower();
            var shouldSkip = skipExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLower())
                .Contains(ext);

            List<TextChunk> textChunks;
            if (shouldSkip)
            {
                textChunks = new List<TextChunk> { new() { Index = 0, Content = content } };
            }
            else
            {
                textChunks = _textSplitterService.Split(content, method, chunkSize, chunkOverlap);
            }

            var chunks = textChunks.Select(tc => new RagDocumentChunk
            {
                DocumentId = documentId,
                ChunkIndex = tc.Index,
                Content = tc.Content,
                Metadata = JsonSerializer.Serialize(tc.Metadata),
            }).ToList();

            await _chunkRepo.DeleteByDocumentIdAsync(documentId);
            await _chunkRepo.CreateBatchAsync(chunks);

            var collectionName = await GetCollectionName();
            await _vectorStoreService.EnsureCollectionExistsAsync(collectionName, _embeddingService.EmbeddingDimension);

            var texts = chunks.Select(c => c.Content).ToList();
            var vectors = await _embeddingService.EmbedBatchAsync(texts);

            var successCount = 0;
            for (int i = 0; i < chunks.Count; i++)
            {
                try
                {
                    var metadata = new Dictionary<string, string>
                    {
                        ["document_id"] = documentId.ToString(),
                        ["chunk_index"] = chunks[i].ChunkIndex.ToString(),
                        ["file_name"] = doc.FileName,
                        ["file_type"] = doc.FileType ?? "",
                    };

                    await _vectorStoreService.UpsertAsync(
                        collectionName,
                        $"{documentId}_{chunks[i].ChunkIndex}",
                        vectors[i],
                        metadata,
                        chunks[i].Content);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "向量入库失败: Chunk {Index}", chunks[i].ChunkIndex);
                }
            }

            doc.Status = RagDocumentStatus.Completed;
            doc.ProcessedAt = DateTime.UtcNow;
            await _documentRepo.UpdateAsync(doc);

            return new VectorizeResult { SuccessCount = successCount };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "向量入库失败: DocId={DocId}", documentId);
            doc.Status = RagDocumentStatus.Failed;
            doc.ErrorMessage = ex.Message;
            await _documentRepo.UpdateAsync(doc);
            return new VectorizeResult { SuccessCount = 0, ErrorMessage = ex.Message };
        }
    }

    public async Task<RagQueryResult> QueryAsync(string query, int topK = 5, double scoreThreshold = 0.5, long? llmConfigId = null, string? systemPrompt = null)
    {
        var collectionName = await GetCollectionName();
        var queryVector = await _embeddingService.EmbedAsync(query);

        var searchResults = await _vectorStoreService.SearchAsync(
            collectionName, queryVector, topK, (float)scoreThreshold);

        var documents = searchResults.Select(r => r.Content).ToList();

        List<RerankResult>? rerankResults = null;
        try
        {
            if (documents.Count > 0)
            {
                rerankResults = await _rerankService.RerankAsync(query, documents, topK);
                searchResults = rerankResults.Select(r => searchResults[r.Index]).ToList();
                for (int i = 0; i < searchResults.Count; i++)
                {
                    searchResults[i].Score = rerankResults[i].RelevanceScore;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "重排序失败，使用原始排序");
        }

        string? answer = null;
        if (llmConfigId.HasValue && searchResults.Count > 0)
        {
            var context = string.Join("\n\n---\n\n", searchResults.Select(r =>
                $"[来源: {r.Metadata.GetValueOrDefault("file_name", "未知")} (相关度: {r.Score:F2})]\n{r.Content}"));

            var prompt = systemPrompt ?? "你是一个智能助手，请根据提供的上下文信息回答用户问题。如果上下文中没有相关信息，请诚实地说不知道。";
            var userMessage = $"上下文信息：\n{context}\n\n用户问题：{query}";

            try
            {
                var llmConfig = await _llmConfigService.GetByIdAsync(llmConfigId.Value);
                if (llmConfig != null)
                {
                    var messages = new List<ChatMessage>
                    {
                        new(ChatRole.System, prompt),
                        new(ChatRole.User, userMessage),
                    };
                    var chatResponse = await _chatService.SendMessageAsync(llmConfigId.Value, null, messages);
                    answer = chatResponse.Messages.LastOrDefault()?.Text;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM生成回答失败");
                answer = $"LLM生成回答失败: {ex.Message}";
            }
        }

        return new RagQueryResult
        {
            Answer = answer,
            Sources = searchResults.Select(s => new RagSource
            {
                Content = s.Content,
                Score = s.Score,
                FileName = s.Metadata.GetValueOrDefault("file_name", ""),
                DocumentId = s.Metadata.GetValueOrDefault("document_id", ""),
                ChunkIndex = s.Metadata.TryGetValue("chunk_index", out var ci) ? int.Parse(ci) : 0,
            }).ToList(),
        };
    }

    public async Task<VectorDocsResult> GetVectorDocsAsync(int page = 1, int pageSize = 10, string? keyword = null)
    {
        var collectionName = await GetCollectionName();
        var total = await _vectorStoreService.CountAsync(collectionName);

        List<VectorSearchResult> docs;
        if (!string.IsNullOrEmpty(keyword))
        {
            docs = await _vectorStoreService.SearchWithKeywordAsync(collectionName, keyword, page, pageSize);
        }
        else
        {
            docs = await _vectorStoreService.SearchWithKeywordAsync(collectionName, "", page, pageSize);
        }

        return new VectorDocsResult
        {
            Total = total,
            Documents = docs.Select(d => new VectorDocItem
            {
                Id = d.Id,
                Content = d.Content,
                Score = d.Score,
                DocumentId = d.Metadata.GetValueOrDefault("document_id", ""),
                ChunkIndex = d.Metadata.TryGetValue("chunk_index", out var ci) ? int.Parse(ci) : 0,
                FileName = d.Metadata.GetValueOrDefault("file_name", ""),
            }).ToList(),
        };
    }

    public async Task<bool> DeleteVectorDocAsync(string id)
    {
        var collectionName = await GetCollectionName();
        try
        {
            await _vectorStoreService.DeleteAsync(collectionName, id);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public List<SplitMethodInfo> GetSplitMethods()
    {
        return new List<SplitMethodInfo>
        {
            new() { Key = "recursive", Name = "递归分割", Description = "按分隔符层级递归分割，推荐" },
            new() { Key = "character", Name = "字符分割", Description = "按固定字符数分割" },
            new() { Key = "separator", Name = "分隔符分割", Description = "按段落分隔符分割" },
        };
    }

    public List<FileTypeInfo> GetFileTypes()
    {
        return new List<FileTypeInfo>
        {
            new() { Extension = ".pdf", Name = "PDF文档", Category = "document" },
            new() { Extension = ".docx", Name = "Word文档", Category = "document" },
            new() { Extension = ".xlsx", Name = "Excel表格", Category = "document" },
            new() { Extension = ".md", Name = "Markdown", Category = "text" },
            new() { Extension = ".txt", Name = "TXT文本", Category = "text" },
            new() { Extension = ".csv", Name = "CSV数据", Category = "data" },
            new() { Extension = ".html", Name = "HTML网页", Category = "web" },
            new() { Extension = ".json", Name = "JSON数据", Category = "data" },
            new() { Extension = ".xml", Name = "XML配置", Category = "data" },
            new() { Extension = ".yaml", Name = "YAML配置", Category = "data" },
            new() { Extension = ".yml", Name = "YAML配置", Category = "data" },
        };
    }

    private async Task<string> ReadDocumentContent(RagDocument doc)
    {
        if (!string.IsNullOrEmpty(doc.FilePath) && File.Exists(doc.FilePath))
        {
            return await File.ReadAllTextAsync(doc.FilePath);
        }

        var workspaceBase = _workspaceOptions.Value.BaseDir;
        var fullPath = Path.Combine(workspaceBase, doc.FilePath ?? doc.FileName);
        if (File.Exists(fullPath))
        {
            return await File.ReadAllTextAsync(fullPath);
        }

        return "";
    }

    private async Task<string> GetCollectionName()
    {
        return await GetConfigValue("vector_db_collection", "maf_documents");
    }

    private async Task<string> GetConfigValue(string key, string defaultValue)
    {
        var config = await _configRepo.GetByKeyAsync(key);
        return config?.Value ?? defaultValue;
    }
}
