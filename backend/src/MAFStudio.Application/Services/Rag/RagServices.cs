using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Application.Services.Rag;

public class EmbeddingService : IEmbeddingService
{
    private readonly ISystemConfigRepository _configRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EmbeddingService> _logger;
    private int? _cachedDimension;

    public int EmbeddingDimension => _cachedDimension ?? 1024;

    public EmbeddingService(
        ISystemConfigRepository configRepo,
        IHttpClientFactory httpClientFactory,
        ILogger<EmbeddingService> logger)
    {
        _configRepo = configRepo;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(string text)
    {
        var results = await EmbedBatchAsync(new List<string> { text });
        return results[0];
    }

    public async Task<List<float[]>> EmbedBatchAsync(List<string> texts)
    {
        try
        {
            var endpoint = await GetConfigValue("embedding_endpoint", "http://localhost:7997");
            var model = await GetConfigValue("embedding_model", "BAAI/bge-m3");

            var client = _httpClientFactory.CreateClient("Infinity");
            var request = new
            {
                model,
                input = texts,
            };

            var response = await client.PostAsJsonAsync($"{endpoint}/embeddings", request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var data = json.GetProperty("data");
            var results = new List<float[]>();

            foreach (var item in data.EnumerateArray())
            {
                var embedding = item.GetProperty("embedding");
                var vector = new float[embedding.GetArrayLength()];
                int i = 0;
                foreach (var v in embedding.EnumerateArray())
                {
                    vector[i++] = v.GetSingle();
                }
                results.Add(vector);
            }

            if (_cachedDimension == null && results.Count > 0)
            {
                _cachedDimension = results[0].Length;
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "向量化失败");
            throw;
        }
    }

    private async Task<string> GetConfigValue(string key, string defaultValue)
    {
        var config = await _configRepo.GetByKeyAsync(key);
        return config?.Value ?? defaultValue;
    }
}

public class RerankService : IRerankService
{
    private readonly ISystemConfigRepository _configRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RerankService> _logger;

    public RerankService(
        ISystemConfigRepository configRepo,
        IHttpClientFactory _httpClientFactory,
        ILogger<RerankService> logger)
    {
        this._configRepo = configRepo;
        this._httpClientFactory = _httpClientFactory;
        _logger = logger;
    }

    public async Task<List<RerankResult>> RerankAsync(string query, List<string> documents, int topK = 5)
    {
        try
        {
            var endpoint = await GetConfigValue("rerank_endpoint", "http://localhost:7997");
            var model = await GetConfigValue("rerank_model", "BAAI/bge-reranker-v2-m3");

            var client = _httpClientFactory.CreateClient("Infinity");
            var request = new
            {
                model,
                query,
                documents,
                top_n = topK,
            };

            var response = await client.PostAsJsonAsync($"{endpoint}/rerank", request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var results = json.GetProperty("results");

            var rerankResults = new List<RerankResult>();
            foreach (var item in results.EnumerateArray())
            {
                rerankResults.Add(new RerankResult
                {
                    Index = item.GetProperty("index").GetInt32(),
                    Text = item.TryGetProperty("document", out var docEl)
                        ? docEl.GetProperty("text").GetString() ?? ""
                        : documents[item.GetProperty("index").GetInt32()],
                    RelevanceScore = item.GetProperty("relevance_score").GetDouble(),
                });
            }

            return rerankResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重排序失败");
            throw;
        }
    }

    private async Task<string> GetConfigValue(string key, string defaultValue)
    {
        var config = await _configRepo.GetByKeyAsync(key);
        return config?.Value ?? defaultValue;
    }
}

public class VectorStoreService : IVectorStoreService
{
    private readonly ISystemConfigRepository _configRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<VectorStoreService> _logger;

    public VectorStoreService(
        ISystemConfigRepository configRepo,
        IHttpClientFactory httpClientFactory,
        ILogger<VectorStoreService> logger)
    {
        _configRepo = configRepo;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task EnsureCollectionExistsAsync(string collectionName, int vectorSize)
    {
        var endpoint = await GetConfigValue("vector_db_endpoint", "http://localhost:6333");
        var client = _httpClientFactory.CreateClient("Qdrant");

        var response = await client.GetAsync($"{endpoint}/collections/{collectionName}");
        if (response.IsSuccessStatusCode) return;

        var createRequest = new
        {
            vectors = new
            {
                size = vectorSize,
                distance = "Cosine"
            }
        };

        await client.PutAsJsonAsync($"{endpoint}/collections/{collectionName}", createRequest);
        _logger.LogInformation("创建 Qdrant 集合: {Collection}, 向量维度: {Size}", collectionName, vectorSize);
    }

    public async Task UpsertAsync(string collectionName, string id, float[] vector, Dictionary<string, string> metadata, string content)
    {
        var endpoint = await GetConfigValue("vector_db_endpoint", "http://localhost:6333");
        var client = _httpClientFactory.CreateClient("Qdrant");

        var payload = new Dictionary<string, object>
        {
            ["content"] = content,
        };
        foreach (var kv in metadata)
        {
            payload[kv.Key] = kv.Value;
        }

        var point = new
        {
            id,
            vector,
            payload,
        };

        var request = new
        {
            points = new[] { point }
        };

        await client.PutAsJsonAsync($"{endpoint}/collections/{collectionName}/points", request);
    }

    public async Task DeleteAsync(string collectionName, string id)
    {
        var endpoint = await GetConfigValue("vector_db_endpoint", "http://localhost:6333");
        var client = _httpClientFactory.CreateClient("Qdrant");

        var request = new
        {
            points = new[] { id }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/collections/{collectionName}/points/delete")
        {
            Content = content
        };
        await client.SendAsync(httpRequest);
    }

    public async Task<List<VectorSearchResult>> SearchAsync(string collectionName, float[] queryVector, int topK = 5, float scoreThreshold = 0.5f)
    {
        var endpoint = await GetConfigValue("vector_db_endpoint", "http://localhost:6333");
        var client = _httpClientFactory.CreateClient("Qdrant");

        var request = new
        {
            vector = queryVector,
            limit = topK,
            score_threshold = scoreThreshold,
            with_payload = true,
        };

        var response = await client.PostAsJsonAsync($"{endpoint}/collections/{collectionName}/points/search", request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var results = json.GetProperty("result");

        var searchResults = new List<VectorSearchResult>();
        foreach (var item in results.EnumerateArray())
        {
            var payload = item.GetProperty("payload");
            var metadata = new Dictionary<string, string>();
            foreach (var prop in payload.EnumerateObject())
            {
                if (prop.Name != "content")
                {
                    metadata[prop.Name] = prop.Value.GetString() ?? "";
                }
            }

            searchResults.Add(new VectorSearchResult
            {
                Id = item.GetProperty("id").GetString() ?? "",
                Content = payload.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "",
                Score = item.GetProperty("score").GetDouble(),
                Metadata = metadata,
            });
        }

        return searchResults;
    }

    public async Task<List<VectorSearchResult>> SearchWithKeywordAsync(string collectionName, string keyword, int page = 1, int pageSize = 10)
    {
        var endpoint = await GetConfigValue("vector_db_endpoint", "http://localhost:6333");
        var client = _httpClientFactory.CreateClient("Qdrant");

        var offset = (page - 1) * pageSize;
        var requestObj = new
        {
            filter = string.IsNullOrEmpty(keyword) ? (object?)null : new
            {
                should = new[]
                {
                    new
                    {
                        field = "content",
                        match = new { text = keyword }
                    }
                }
            },
            limit = pageSize,
            offset,
            with_payload = true,
        };

        var response = await client.PostAsJsonAsync($"{endpoint}/collections/{collectionName}/points/scroll", requestObj);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var points = json.GetProperty("result").GetProperty("points");

        var results = new List<VectorSearchResult>();
        foreach (var point in points.EnumerateArray())
        {
            var payload = point.GetProperty("payload");
            var metadata = new Dictionary<string, string>();
            foreach (var prop in payload.EnumerateObject())
            {
                if (prop.Name != "content")
                {
                    metadata[prop.Name] = prop.Value.GetString() ?? "";
                }
            }

            results.Add(new VectorSearchResult
            {
                Id = point.GetProperty("id").GetString() ?? "",
                Content = payload.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "",
                Score = 0,
                Metadata = metadata,
            });
        }

        return results;
    }

    public async Task<long> CountAsync(string collectionName)
    {
        var endpoint = await GetConfigValue("vector_db_endpoint", "http://localhost:6333");
        var client = _httpClientFactory.CreateClient("Qdrant");

        var response = await client.GetAsync($"{endpoint}/collections/{collectionName}");
        if (!response.IsSuccessStatusCode) return 0;

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("result").GetProperty("points_count").GetInt64();
    }

    private async Task<string> GetConfigValue(string key, string defaultValue)
    {
        var config = await _configRepo.GetByKeyAsync(key);
        return config?.Value ?? defaultValue;
    }
}

public class TextSplitterService : ITextSplitterService
{
    private readonly ISystemConfigRepository _configRepo;

    public TextSplitterService(ISystemConfigRepository configRepo)
    {
        _configRepo = configRepo;
    }

    public List<TextChunk> Split(string text, string? method = null, int? chunkSize = null, int? chunkOverlap = null)
    {
        var splitMethod = method ?? "recursive";
        var size = chunkSize ?? 500;
        var overlap = chunkOverlap ?? 50;

        return splitMethod switch
        {
            "recursive" => RecursiveSplit(text, size, overlap),
            "character" => CharacterSplit(text, size, overlap),
            "separator" => SeparatorSplit(text),
            _ => RecursiveSplit(text, size, overlap),
        };
    }

    private List<TextChunk> RecursiveSplit(string text, int chunkSize, int overlap)
    {
        var chunks = new List<TextChunk>();
        var separators = new[] { "\n\n", "\n", "。", ".", "！", "!", "？", "?", "；", ";", " ", "" };

        var pieces = SplitBySeparators(text, separators, chunkSize);
        var currentChunk = "";
        var index = 0;

        foreach (var piece in pieces)
        {
            if (currentChunk.Length + piece.Length > chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(new TextChunk { Index = index++, Content = currentChunk.Trim() });
                if (overlap > 0 && currentChunk.Length > overlap)
                {
                    currentChunk = currentChunk[^overlap..] + piece;
                }
                else
                {
                    currentChunk = piece;
                }
            }
            else
            {
                currentChunk += piece;
            }
        }

        if (!string.IsNullOrWhiteSpace(currentChunk))
        {
            chunks.Add(new TextChunk { Index = index, Content = currentChunk.Trim() });
        }

        return chunks;
    }

    private List<string> SplitBySeparators(string text, string[] separators, int chunkSize)
    {
        if (separators.Length == 0 || string.IsNullOrEmpty(text))
            return new List<string> { text };

        var separator = separators[0];
        var remainingSeparators = separators[1..];

        if (string.IsNullOrEmpty(separator))
            return SplitBySize(text, chunkSize);

        var parts = text.Split(separator, StringSplitOptions.None);
        var result = new List<string>();

        foreach (var part in parts)
        {
            if (part.Length <= chunkSize)
            {
                if (!string.IsNullOrEmpty(part))
                    result.Add(part + separator);
            }
            else
            {
                var subParts = SplitBySeparators(part, remainingSeparators, chunkSize);
                result.AddRange(subParts);
            }
        }

        return result;
    }

    private List<string> SplitBySize(string text, int chunkSize)
    {
        var result = new List<string>();
        for (int i = 0; i < text.Length; i += chunkSize)
        {
            var length = Math.Min(chunkSize, text.Length - i);
            result.Add(text.Substring(i, length));
        }
        return result;
    }

    private List<TextChunk> CharacterSplit(string text, int chunkSize, int overlap)
    {
        var chunks = new List<TextChunk>();
        var index = 0;

        for (int i = 0; i < text.Length; i += chunkSize - overlap)
        {
            var length = Math.Min(chunkSize, text.Length - i);
            var content = text.Substring(i, length).Trim();
            if (!string.IsNullOrWhiteSpace(content))
            {
                chunks.Add(new TextChunk { Index = index++, Content = content });
            }
        }

        return chunks;
    }

    private List<TextChunk> SeparatorSplit(string text)
    {
        var separators = new[] { "\n\n", "\n" };
        var parts = new List<string>();

        foreach (var sep in separators)
        {
            if (text.Contains(sep))
            {
                parts = text.Split(sep, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();
                break;
            }
        }

        if (parts.Count == 0)
            parts = new List<string> { text.Trim() };

        return parts.Select((p, i) => new TextChunk { Index = i, Content = p }).ToList();
    }
}
