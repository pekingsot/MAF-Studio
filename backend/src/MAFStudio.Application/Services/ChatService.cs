using Microsoft.Extensions.AI;
using MAFStudio.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MAFStudio.Application.Services;

/// <summary>
/// 统一的聊天服务实现
/// 基于 IChatClient 提供统一的大模型调用能力
/// </summary>
public class ChatService : IChatService
{
    private readonly IChatClientFactory _chatClientFactory;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IChatClientFactory chatClientFactory,
        ILogger<ChatService> logger)
    {
        _chatClientFactory = chatClientFactory;
        _logger = logger;
    }

    public async Task<ChatResponse> SendMessageAsync(
        long llmConfigId,
        long? modelConfigId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var client = await _chatClientFactory.CreateClientAsync(llmConfigId, modelConfigId);

            _logger.LogInformation("发送聊天消息: LlmConfigId={LlmConfigId}, ModelConfigId={ModelConfigId}, MessageCount={Count}",
                llmConfigId, modelConfigId, messages.Count());

            var response = await client.GetResponseAsync(messages, options, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation("聊天响应完成: Duration={Duration}ms", stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "聊天请求失败: LlmConfigId={LlmConfigId}, Duration={Duration}ms",
                llmConfigId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async IAsyncEnumerable<StreamingChatResponse> SendMessageStreamingAsync(
        long llmConfigId,
        long? modelConfigId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var client = await _chatClientFactory.CreateClientAsync(llmConfigId, modelConfigId);

        _logger.LogInformation("发送流式聊天消息: LlmConfigId={LlmConfigId}, ModelConfigId={ModelConfigId}",
            llmConfigId, modelConfigId);

        await foreach (var update in client.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            yield return new StreamingChatResponse
            {
                Text = update.Text,
                IsComplete = update.FinishReason != null,
                ToolCallInfo = null,
                Usage = null
            };
        }

        _logger.LogInformation("流式聊天响应完成: LlmConfigId={LlmConfigId}", llmConfigId);
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(
        long llmConfigId,
        long? modelConfigId = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var client = await _chatClientFactory.CreateClientAsync(llmConfigId, modelConfigId);

            var testMessage = new ChatMessage(ChatRole.User, "Hi");
            var options = new ChatOptions
            {
                MaxOutputTokens = 10
            };

            var response = await client.GetResponseAsync(
                new[] { testMessage },
                options,
                cancellationToken);

            stopwatch.Stop();

            return new ConnectionTestResult
            {
                Success = true,
                Message = "连接成功",
                LatencyMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "测试连接失败: LlmConfigId={LlmConfigId}", llmConfigId);

            return new ConnectionTestResult
            {
                Success = false,
                Message = ex.Message,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableModelsAsync(
        long llmConfigId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = await _chatClientFactory.CreateClientAsync(llmConfigId);

            var metadata = client.GetService(typeof(ChatClientMetadata)) as ChatClientMetadata;
            if (metadata != null)
            {
                return new List<string>().AsReadOnly();
            }

            return new List<string>().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模型列表失败: LlmConfigId={LlmConfigId}", llmConfigId);
            return new List<string>().AsReadOnly();
        }
    }
}
