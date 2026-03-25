using Microsoft.EntityFrameworkCore;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MAFStudio.Backend.Services
{
    /// <summary>
    /// RAG服务实现
    /// 提供文档处理、文本分割、向量入库和检索功能
    /// </summary>
    public class RagService : IRagService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISystemConfigService _systemConfigService;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// 构造函数
        /// </summary>
        public RagService(
            ApplicationDbContext context, 
            ISystemConfigService systemConfigService,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _systemConfigService = systemConfigService;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// 上传文档
        /// </summary>
        public async Task<RagDocument> UploadDocumentAsync(string fileName, string content, string? fileType, string? splitMethod, int? chunkSize, int? chunkOverlap, string? skipExtensions = null, string? userId = null)
        {
            // 获取默认配置
            var defaultSplitMethod = await _systemConfigService.GetConfigValueAsync(SystemConfigKeys.DefaultSplitMethod, "recursive");
            var defaultChunkSize = int.Parse(await _systemConfigService.GetConfigValueAsync(SystemConfigKeys.DefaultChunkSize, "500"));
            var defaultChunkOverlap = int.Parse(await _systemConfigService.GetConfigValueAsync(SystemConfigKeys.DefaultChunkOverlap, "50"));

            // 检查是否跳过分割
            var skipExts = skipExtensions ?? await _systemConfigService.GetConfigValueAsync(SystemConfigKeys.SkipSplitExtensions, "");
            var shouldSkipSplit = ShouldSkipSplit(fileType, skipExts);

            var document = new RagDocument
            {
                Id = Guid.NewGuid(),
                FileName = fileName,
                OriginalFileName = fileName,
                FileType = fileType,
                FileSize = content.Length,
                ContentHash = ComputeHash(content),
                SplitMethod = shouldSkipSplit ? "none" : (splitMethod ?? defaultSplitMethod),
                ChunkSize = chunkSize ?? defaultChunkSize,
                ChunkOverlap = chunkOverlap ?? defaultChunkOverlap,
                Status = DocumentStatus.Processing,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.RagDocuments.Add(document);
            await _context.SaveChangesAsync();

            try
            {
                // 执行文本分割
                var chunks = shouldSkipSplit 
                    ? new List<string> { content } 
                    : SplitText(content, document.SplitMethod!, document.ChunkSize!.Value, document.ChunkOverlap!.Value);

                // 保存分块
                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunk = new RagDocumentChunk
                    {
                        Id = Guid.NewGuid(),
                        DocumentId = document.Id,
                        Content = chunks[i],
                        ChunkIndex = i,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.RagDocumentChunks.Add(chunk);
                }

                document.ChunkCount = chunks.Count;
                document.Status = DocumentStatus.Completed;
                document.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                document.Status = DocumentStatus.Failed;
                document.ErrorMessage = ex.Message;
                document.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return document;
        }

        /// <summary>
        /// 获取所有文档
        /// </summary>
        public async Task<List<RagDocument>> GetAllDocumentsAsync(string? userId = null)
        {
            var query = _context.RagDocuments
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(d => d.UserId == userId);
            }

            return await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取文档详情
        /// </summary>
        public async Task<RagDocument?> GetDocumentByIdAsync(Guid id, string? userId = null)
        {
            var query = _context.RagDocuments
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(d => d.UserId == userId);
            }

            return await query.FirstOrDefaultAsync(d => d.Id == id);
        }

        /// <summary>
        /// 获取文档分块
        /// </summary>
        public async Task<List<RagDocumentChunk>> GetDocumentChunksAsync(Guid documentId)
        {
            return await _context.RagDocumentChunks
                .AsNoTracking()
                .Where(c => c.DocumentId == documentId)
                .OrderBy(c => c.ChunkIndex)
                .ToListAsync();
        }

        /// <summary>
        /// 删除文档
        /// </summary>
        public async Task<bool> DeleteDocumentAsync(Guid id, string? userId = null)
        {
            var document = await _context.RagDocuments.FindAsync(id);
            if (document == null) return false;
            
            // 只能删除自己的文档
            if (!string.IsNullOrEmpty(userId) && document.UserId != userId) return false;

            // 删除关联的分块
            var chunks = await _context.RagDocumentChunks
                .Where(c => c.DocumentId == id)
                .ToListAsync();
            _context.RagDocumentChunks.RemoveRange(chunks);

            _context.RagDocuments.Remove(document);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// 分割文本
        /// </summary>
        public List<string> SplitText(string text, string method, int chunkSize, int chunkOverlap)
        {
            return method.ToLower() switch
            {
                "character" => SplitByCharacter(text, chunkSize, chunkOverlap),
                "recursive" => SplitRecursively(text, chunkSize, chunkOverlap),
                "separator" => SplitBySeparator(text, "\n\n", chunkSize),
                _ => SplitRecursively(text, chunkSize, chunkOverlap)
            };
        }

        /// <summary>
        /// 测试文本分割
        /// </summary>
        public async Task<RagDocument> TestSplitAsync(string content, string splitMethod, int chunkSize, int chunkOverlap)
        {
            var chunks = SplitText(content, splitMethod, chunkSize, chunkOverlap);

            var document = new RagDocument
            {
                Id = Guid.NewGuid(),
                FileName = "test_split",
                FileType = "txt",
                FileSize = content.Length,
                ContentHash = ComputeHash(content),
                SplitMethod = splitMethod,
                ChunkSize = chunkSize,
                ChunkOverlap = chunkOverlap,
                ChunkCount = chunks.Count,
                Status = DocumentStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = new RagDocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    Content = chunks[i],
                    ChunkIndex = i,
                    CreatedAt = DateTime.UtcNow
                };
                document.Chunks.Add(chunk);
            }

            return document;
        }

        /// <summary>
        /// 向量入库
        /// </summary>
        public async Task<int> VectorizeDocumentAsync(Guid documentId, string vectorizationEndpoint, string vectorDbEndpoint, string collection, string? userId = null)
        {
            var document = await _context.RagDocuments.FindAsync(documentId);
            if (document == null) return 0;
            
            // 只能向量化自己的文档
            if (!string.IsNullOrEmpty(userId) && document.UserId != userId) return 0;
            
            var chunks = await _context.RagDocumentChunks
                .Where(c => c.DocumentId == documentId && c.VectorId == null)
                .ToListAsync();

            if (!chunks.Any()) return 0;

            var httpClient = _httpClientFactory.CreateClient();
            var successCount = 0;

            foreach (var chunk in chunks)
            {
                try
                {
                    // 调用向量化接口
                    var embedRequest = new { text = chunk.Content };
                    var embedResponse = await httpClient.PostAsJsonAsync(vectorizationEndpoint, embedRequest);
                    
                    if (!embedResponse.IsSuccessStatusCode) continue;

                    var embedResult = await embedResponse.Content.ReadFromJsonAsync<EmbeddingResponse>();
                    if (embedResult?.Embedding == null) continue;

                    // 存入向量数据库
                    var vectorId = Guid.NewGuid().ToString();
                    var upsertRequest = new
                    {
                        id = vectorId,
                        vector = embedResult.Embedding,
                        payload = new
                        {
                            document_id = documentId.ToString(),
                            chunk_index = chunk.ChunkIndex,
                            content = chunk.Content.Length > 1000 ? chunk.Content.Substring(0, 1000) : chunk.Content
                        }
                    };

                    var upsertUrl = $"{vectorDbEndpoint.TrimEnd('/')}/collections/{collection}/points";
                    var upsertResponse = await httpClient.PutAsJsonAsync(upsertUrl, new { points = new[] { upsertRequest } });

                    if (upsertResponse.IsSuccessStatusCode)
                    {
                        chunk.VectorId = vectorId;
                        successCount++;
                    }
                }
                catch
                {
                    // 忽略单个分块的错误，继续处理其他分块
                }
            }

            await _context.SaveChangesAsync();
            return successCount;
        }

        /// <summary>
        /// RAG检索
        /// </summary>
        public async Task<RagQueryResult> RagQueryAsync(string query, string vectorizationEndpoint, string vectorDbEndpoint, string collection, LLMConfig llmConfig, LLMModelConfig? modelConfig, int topK, double scoreThreshold, string? systemPrompt)
        {
            var httpClient = _httpClientFactory.CreateClient();

            // 1. 将查询向量化
            var embedRequest = new { text = query };
            var embedResponse = await httpClient.PostAsJsonAsync(vectorizationEndpoint, embedRequest);
            embedResponse.EnsureSuccessStatusCode();
            var embedResult = await embedResponse.Content.ReadFromJsonAsync<EmbeddingResponse>();

            if (embedResult?.Embedding == null)
            {
                throw new Exception("向量化失败");
            }

            // 2. 在向量数据库中检索
            var searchUrl = $"{vectorDbEndpoint.TrimEnd('/')}/collections/{collection}/search";
            var searchRequest = new
            {
                vector = embedResult.Embedding,
                limit = topK,
                score_threshold = scoreThreshold
            };
            var searchResponse = await httpClient.PostAsJsonAsync(searchUrl, searchRequest);
            searchResponse.EnsureSuccessStatusCode();
            var searchResult = await searchResponse.Content.ReadFromJsonAsync<SearchResponse>();

            var sources = new List<RagSource>();
            var contextBuilder = new StringBuilder();

            if (searchResult?.Results != null)
            {
                foreach (var result in searchResult.Results)
                {
                    var content = result.Payload?.Content ?? "";
                    sources.Add(new RagSource
                    {
                        Content = content,
                        Score = result.Score,
                        DocumentId = result.Payload?.DocumentId
                    });
                    contextBuilder.AppendLine(content);
                    contextBuilder.AppendLine();
                }
            }

            // 3. 调用大模型生成回答
            var modelName = modelConfig?.ModelName ?? "gpt-3.5-turbo";
            var temperature = modelConfig?.Temperature ?? 0.7;
            var maxTokens = modelConfig?.MaxTokens ?? 4096;

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt ?? "你是一个智能助手，请根据提供的上下文信息回答用户问题。如果上下文中没有相关信息，请诚实地说不知道。" },
                new { role = "user", content = $"上下文信息：\n{contextBuilder}\n\n问题：{query}" }
            };

            var llmRequest = new
            {
                model = modelName,
                messages,
                temperature = temperature,
                max_tokens = maxTokens
            };

            var llmUrl = $"{llmConfig.Endpoint?.TrimEnd('/') ?? "https://api.openai.com/v1"}/chat/completions";
            var llmRequestMessage = new HttpRequestMessage(HttpMethod.Post, llmUrl)
            {
                Content = JsonContent.Create(llmRequest)
            };
            llmRequestMessage.Headers.Add("Authorization", $"Bearer {llmConfig.ApiKey}");

            var llmResponse = await httpClient.SendAsync(llmRequestMessage);
            llmResponse.EnsureSuccessStatusCode();
            var llmResult = await llmResponse.Content.ReadFromJsonAsync<LlmResponse>();

            return new RagQueryResult
            {
                Answer = llmResult?.Choices?.FirstOrDefault()?.Message?.Content ?? "无法生成回答",
                Sources = sources
            };
        }

        /// <summary>
        /// 按字符数分割
        /// </summary>
        private List<string> SplitByCharacter(string text, int chunkSize, int chunkOverlap)
        {
            var chunks = new List<string>();
            var start = 0;

            while (start < text.Length)
            {
                var end = Math.Min(start + chunkSize, text.Length);
                chunks.Add(text.Substring(start, end - start));
                start += chunkSize - chunkOverlap;
            }

            return chunks;
        }

        /// <summary>
        /// 递归分割 (按段落、句子、词)
        /// </summary>
        private List<string> SplitRecursively(string text, int chunkSize, int chunkOverlap)
        {
            var chunks = new List<string>();
            var separators = new[] { "\n\n", "\n", "。", "！", "？", ".", "!", "?", "；", ";", "，", ",", " ", "" };

            var result = SplitBySeparators(text, separators, chunkSize, 0);
            
            // 合并过小的块
            var mergedChunks = new List<string>();
            var currentChunk = new StringBuilder();

            foreach (var chunk in result)
            {
                if (currentChunk.Length + chunk.Length > chunkSize && currentChunk.Length > 0)
                {
                    mergedChunks.Add(currentChunk.ToString().Trim());
                    // 处理重叠
                    if (chunkOverlap > 0 && currentChunk.Length > chunkOverlap)
                    {
                        currentChunk.Clear();
                        currentChunk.Append(currentChunk.ToString().Substring(currentChunk.Length - chunkOverlap));
                    }
                    else
                    {
                        currentChunk.Clear();
                    }
                }
                currentChunk.Append(chunk);
            }

            if (currentChunk.Length > 0)
            {
                mergedChunks.Add(currentChunk.ToString().Trim());
            }

            return mergedChunks;
        }

        /// <summary>
        /// 按分隔符分割
        /// </summary>
        private List<string> SplitBySeparator(string text, string separator, int chunkSize)
        {
            var parts = text.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            var chunks = new List<string>();
            var currentChunk = new StringBuilder();

            foreach (var part in parts)
            {
                if (currentChunk.Length + part.Length + separator.Length > chunkSize && currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }
                currentChunk.Append(part + separator);
            }

            if (currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
            }

            return chunks;
        }

        /// <summary>
        /// 递归按分隔符分割
        /// </summary>
        private List<string> SplitBySeparators(string text, string[] separators, int chunkSize, int separatorIndex)
        {
            if (string.IsNullOrEmpty(text) || separatorIndex >= separators.Length)
            {
                return string.IsNullOrEmpty(text) ? new List<string>() : new List<string> { text };
            }

            var separator = separators[separatorIndex];
            var parts = text.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>();
            var currentChunk = new StringBuilder();

            foreach (var part in parts)
            {
                if (currentChunk.Length + part.Length + separator.Length > chunkSize && currentChunk.Length > 0)
                {
                    // 当前块已满，检查是否需要进一步分割
                    if (currentChunk.Length > chunkSize && separatorIndex < separators.Length - 1)
                    {
                        result.AddRange(SplitBySeparators(currentChunk.ToString(), separators, chunkSize, separatorIndex + 1));
                    }
                    else
                    {
                        result.Add(currentChunk.ToString().Trim());
                    }
                    currentChunk.Clear();
                }
                currentChunk.Append(part + separator);
            }

            if (currentChunk.Length > 0)
            {
                if (currentChunk.Length > chunkSize && separatorIndex < separators.Length - 1)
                {
                    result.AddRange(SplitBySeparators(currentChunk.ToString(), separators, chunkSize, separatorIndex + 1));
                }
                else
                {
                    result.Add(currentChunk.ToString().Trim());
                }
            }

            return result;
        }

        /// <summary>
        /// 检查是否应该跳过分割
        /// </summary>
        private bool ShouldSkipSplit(string? fileType, string skipExtensions)
        {
            if (string.IsNullOrEmpty(fileType)) return false;

            var extensions = skipExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLower())
                .ToList();

            var ext = fileType.StartsWith(".") ? fileType.ToLower() : "." + fileType.ToLower();
            return extensions.Contains(ext);
        }

        /// <summary>
        /// 计算内容哈希
        /// </summary>
        private string ComputeHash(string content)
        {
            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(content));
            return Convert.ToHexString(bytes);
        }

        #region 内部响应类

        private class EmbeddingResponse
        {
            public float[]? Embedding { get; set; }
        }

        private class SearchResponse
        {
            public List<SearchResult>? Results { get; set; }
        }

        private class SearchResult
        {
            public string Id { get; set; } = string.Empty;
            public double Score { get; set; }
            public SearchResultPayload? Payload { get; set; }
        }

        private class SearchResultPayload
        {
            public string? DocumentId { get; set; }
            public string? Content { get; set; }
        }

        private class LlmResponse
        {
            public List<LlmChoice>? Choices { get; set; }
        }

        private class LlmChoice
        {
            public LlmMessage? Message { get; set; }
        }

        private class LlmMessage
        {
            public string? Content { get; set; }
        }

        private class QdrantCollectionInfo
        {
            public QdrantCollectionResult? Result { get; set; }
        }

        private class QdrantCollectionResult
        {
            public int PointsCount { get; set; }
        }

        private class QdrantScrollResponse
        {
            public QdrantScrollResult? Result { get; set; }
        }

        private class QdrantScrollResult
        {
            public List<QdrantPoint>? Points { get; set; }
        }

        private class QdrantPoint
        {
            public string Id { get; set; } = string.Empty;
            public QdrantPayload? Payload { get; set; }
        }

        private class QdrantPayload
        {
            public string? Content { get; set; }
            public string? DocumentId { get; set; }
            public int? ChunkIndex { get; set; }
        }

        #endregion

        /// <summary>
        /// 查询向量文档列表
        /// </summary>
        public async Task<VectorDocumentsResult> GetVectorDocumentsAsync(string vectorDbEndpoint, string collection, int page, int pageSize, string? keyword)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var result = new VectorDocumentsResult();

            try
            {
                // 获取集合信息以获取总数
                var collectionUrl = $"{vectorDbEndpoint.TrimEnd('/')}/collections/{collection}";
                var collectionResponse = await httpClient.GetAsync(collectionUrl);
                
                if (collectionResponse.IsSuccessStatusCode)
                {
                    var collectionInfo = await collectionResponse.Content.ReadFromJsonAsync<QdrantCollectionInfo>();
                    result.Total = collectionInfo?.Result?.PointsCount ?? 0;
                }

                // 滚动查询获取数据
                var scrollUrl = $"{vectorDbEndpoint.TrimEnd('/')}/collections/{collection}/points/scroll";
                var scrollRequest = new
                {
                    limit = pageSize,
                    offset = (page - 1) * pageSize,
                    with_payload = true,
                    with_vector = false
                };

                var scrollResponse = await httpClient.PostAsJsonAsync(scrollUrl, scrollRequest);
                if (scrollResponse.IsSuccessStatusCode)
                {
                    var scrollResult = await scrollResponse.Content.ReadFromJsonAsync<QdrantScrollResponse>();
                    
                    if (scrollResult?.Result?.Points != null)
                    {
                        foreach (var point in scrollResult.Result.Points)
                        {
                            var content = point.Payload?.Content ?? "";
                            
                            // 如果有关键词，进行过滤
                            if (!string.IsNullOrEmpty(keyword) && !content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            result.Items.Add(new VectorDocumentItem
                            {
                                Id = point.Id,
                                Content = content,
                                DocumentId = point.Payload?.DocumentId,
                                ChunkIndex = point.Payload?.ChunkIndex
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常，返回空结果
                Console.WriteLine($"查询向量文档失败: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 删除向量文档
        /// </summary>
        public async Task<bool> DeleteVectorDocumentAsync(string vectorDbEndpoint, string collection, string vectorId)
        {
            var httpClient = _httpClientFactory.CreateClient();

            try
            {
                var deleteUrl = $"{vectorDbEndpoint.TrimEnd('/')}/collections/{collection}/points/delete";
                var deleteRequest = new
                {
                    points = new[] { vectorId }
                };

                var response = await httpClient.PostAsJsonAsync(deleteUrl, deleteRequest);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
