using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Application.Services.Rag;

public class RerankService : IRerankService
{
    private readonly ISystemConfigRepository _configRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RerankService> _logger;

    public RerankService(
        ISystemConfigRepository configRepo,
        IHttpClientFactory httpClientFactory,
        ILogger<RerankService> logger)
    {
        _configRepo = configRepo;
        _httpClientFactory = httpClientFactory;
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
