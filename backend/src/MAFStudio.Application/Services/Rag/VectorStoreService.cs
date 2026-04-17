using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Application.Services.Rag;

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
