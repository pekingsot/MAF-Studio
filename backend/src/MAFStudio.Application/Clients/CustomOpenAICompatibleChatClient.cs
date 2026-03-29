using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace MAFStudio.Application.Clients;

/// <summary>
/// 自定义OpenAI兼容的ChatClient（支持通义千问、DeepSeek等）
/// </summary>
public class CustomOpenAICompatibleChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _modelName;
    private readonly string _providerName;

    public CustomOpenAICompatibleChatClient(
        string apiKey,
        string baseUrl,
        string modelName,
        string providerName = "Custom")
    {
        _modelName = modelName;
        _providerName = providerName;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public ChatClientMetadata Metadata => new(_providerName, _httpClient.BaseAddress, _modelName);

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messages = chatMessages.ToList();
        var request = CreateRequest(messages, options);

        var response = await _httpClient.PostAsJsonAsync(
            "/v1/chat/completions",
            request,
            cancellationToken: cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>(cancellationToken);

        return new ChatResponse(
            new ChatMessage(
                ChatRole.Assistant,
                result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty)
            {
                RawRepresentation = result
            })
        {
            ResponseId = result?.Id,
            ModelId = result?.Model,
            CreatedAt = result?.Created != null ? DateTimeOffset.FromUnixTimeSeconds(result.Created) : null,
            Usage = result?.Usage != null ? new UsageDetails
            {
                InputTokenCount = result.Usage.PromptTokens,
                OutputTokenCount = result.Usage.CompletionTokens,
                TotalTokenCount = result.Usage.TotalTokens
            } : null
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = chatMessages.ToList();
        var request = CreateRequest(messages, options);
        request.Stream = true;

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                continue;

            var data = line.Substring(6);

            if (data == "[DONE]")
                break;

            var chunk = JsonSerializer.Deserialize<OpenAIStreamResponse>(data);

            if (chunk?.Choices?.FirstOrDefault()?.Delta?.Content != null)
            {
                yield return new ChatResponseUpdate(
                    ChatRole.Assistant,
                    chunk.Choices[0].Delta.Content)
                {
                    ResponseId = chunk.Id,
                    ModelId = chunk.Model,
                    CreatedAt = chunk.Created != null ? DateTimeOffset.FromUnixTimeSeconds(chunk.Created.Value) : null
                };
            }
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    public object? GetService(Type serviceType, object? key = null)
    {
        return null;
    }

    private OpenAIRequest CreateRequest(List<ChatMessage> chatMessages, ChatOptions? options)
    {
        return new OpenAIRequest
        {
            Model = _modelName,
            Messages = chatMessages.Select(m => new OpenAIMessage
            {
                Role = m.Role.ToString().ToLower(),
                Content = m.Text ?? string.Empty
            }).ToList(),
            Temperature = options?.Temperature,
            MaxTokens = options?.MaxOutputTokens,
            TopP = options?.TopP,
            Stream = false
        };
    }

    private class OpenAIRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<OpenAIMessage> Messages { get; set; } = new();
        public double? Temperature { get; set; }
        public int? MaxTokens { get; set; }
        public double? TopP { get; set; }
        public bool Stream { get; set; }
    }

    private class OpenAIMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private class OpenAIResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public long Created { get; set; }
        public List<OpenAIChoice>? Choices { get; set; }
        public OpenAIUsage? Usage { get; set; }
    }

    private class OpenAIChoice
    {
        public OpenAIMessage? Message { get; set; }
        public int Index { get; set; }
        public string? FinishReason { get; set; }
    }

    private class OpenAIUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    private class OpenAIStreamResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public long? Created { get; set; }
        public List<OpenAIStreamChoice>? Choices { get; set; }
    }

    private class OpenAIStreamChoice
    {
        public OpenAIDelta? Delta { get; set; }
        public int Index { get; set; }
        public string? FinishReason { get; set; }
    }

    private class OpenAIDelta
    {
        public string? Content { get; set; }
    }
}
