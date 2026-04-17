using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Entities;
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
