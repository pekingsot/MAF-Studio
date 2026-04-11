using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MAFStudio.Application.Clients;

/// <summary>
/// 带故障转移功能的ChatClient
/// 当主模型调用失败时，自动切换到副模型
/// </summary>
public class FallbackChatClient : DelegatingChatClient
{
    private readonly List<ChatClientInfo> _clients;
    private readonly ILogger? _logger;
    private readonly int _maxRetries;
    private readonly TimeSpan _retryDelay;

    /// <summary>
    /// 创建FallbackChatClient
    /// </summary>
    /// <param name="clients">客户端列表（按优先级排序，第一个是主模型）</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="maxRetries">每个客户端最大重试次数</param>
    /// <param name="retryDelay">重试延迟</param>
    public FallbackChatClient(
        List<ChatClientInfo> clients,
        ILogger? logger = null,
        int maxRetries = 2,
        TimeSpan? retryDelay = null)
        : base(clients.FirstOrDefault()?.Client ?? throw new ArgumentException("至少需要一个客户端", nameof(clients)))
    {
        _clients = clients ?? throw new ArgumentNullException(nameof(clients));
        _logger = logger;
        _maxRetries = maxRetries;
        _retryDelay = retryDelay ?? TimeSpan.FromSeconds(1);

        if (_clients.Count == 0)
        {
            throw new ArgumentException("至少需要一个客户端", nameof(clients));
        }
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<Exception>();

        foreach (var clientInfo in _clients)
        {
            for (int retry = 0; retry < _maxRetries; retry++)
            {
                try
                {
                    _logger?.LogInformation(
                        "尝试使用模型: {ModelName} (优先级: {Priority}, 重试: {Retry}/{MaxRetries})",
                        clientInfo.ModelName, clientInfo.Priority, retry + 1, _maxRetries);

                    var response = await clientInfo.Client.GetResponseAsync(messages, options, cancellationToken);

                    if (retry > 0 || clientInfo.Priority > 1)
                    {
                        _logger?.LogInformation(
                            "模型调用成功: {ModelName} (优先级: {Priority})",
                            clientInfo.ModelName, clientInfo.Priority);
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    errors.Add(new Exception($"模型 {clientInfo.ModelName} 调用失败: {ex.Message}", ex));

                    _logger?.LogWarning(ex,
                        "模型调用失败: {ModelName} (优先级: {Priority}, 重试: {Retry}/{MaxRetries})",
                        clientInfo.ModelName, clientInfo.Priority, retry + 1, _maxRetries);

                    if (retry < _maxRetries - 1)
                    {
                        await Task.Delay(_retryDelay, cancellationToken);
                    }
                }
            }

            if (clientInfo.Priority < _clients.Count)
            {
                _logger?.LogWarning(
                    "主模型 {ModelName} 失败，切换到副模型...",
                    clientInfo.ModelName);
            }
        }

        var allErrors = string.Join("; ", errors.Select(e => e.Message));
        var failureDetails = _clients
            .Zip(errors.Take(_clients.Count * _maxRetries))
            .Select((pair, idx) => new ModelFailureDetail(pair.First.ModelName, pair.Second.Message, pair.First.Priority))
            .GroupBy(d => d.ModelName)
            .Select(g => g.First())
            .ToList();
        throw new ModelCallFailedException($"所有模型调用失败: {allErrors}", failureDetails);
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (success, updates, errors) = await TryGetStreamingResponseAsync(messages, options, cancellationToken);

        if (success && updates != null)
        {
            foreach (var update in updates)
            {
                yield return update;
            }
            yield break;
        }

        _logger?.LogWarning("所有模型流式调用失败，降级为非流式调用...");

        List<ChatResponseUpdate>? fallbackUpdates = null;
        try
        {
            var response = await GetResponseAsync(messages, options, cancellationToken);
            var text = response.Text ?? "";
            if (!string.IsNullOrEmpty(text))
            {
                fallbackUpdates = new List<ChatResponseUpdate>
                {
                    new(ChatRole.Assistant, text)
                    {
                        ResponseId = response.ResponseId,
                        ModelId = response.ModelId
                    }
                };
            }
        }
        catch (Exception fallbackEx)
        {
            errors.Add(new Exception($"非流式降级调用也失败: {fallbackEx.Message}", fallbackEx));
        }

        if (fallbackUpdates != null)
        {
            foreach (var update in fallbackUpdates)
            {
                yield return update;
            }
            yield break;
        }

        var allErrors = string.Join("; ", errors.Select(e => e.Message));
        var failureDetails = errors
            .Select((e, idx) => new ModelFailureDetail($"模型{(idx / _maxRetries) + 1}", e.Message, idx / _maxRetries + 1))
            .GroupBy(d => d.ModelName)
            .Select(g => g.First())
            .ToList();
        throw new ModelCallFailedException($"所有模型调用失败（流式+非流式）: {allErrors}", failureDetails);
    }

    private async Task<(bool Success, List<ChatResponseUpdate>? Updates, List<Exception> Errors)> TryGetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        var errors = new List<Exception>();
        var allUpdates = new List<ChatResponseUpdate>();

        foreach (var clientInfo in _clients)
        {
            for (int retry = 0; retry < _maxRetries; retry++)
            {
                try
                {
                    _logger?.LogInformation(
                        "尝试使用模型(流式): {ModelName} (优先级: {Priority}, 重试: {Retry}/{MaxRetries})",
                        clientInfo.ModelName, clientInfo.Priority, retry + 1, _maxRetries);

                    var updates = new List<ChatResponseUpdate>();
                    await foreach (var update in clientInfo.Client.GetStreamingResponseAsync(messages, options, cancellationToken))
                    {
                        updates.Add(update);
                    }

                    if (updates.Count > 0)
                    {
                        if (retry > 0 || clientInfo.Priority > 1)
                        {
                            _logger?.LogInformation(
                                "模型流式调用成功: {ModelName} (优先级: {Priority})",
                                clientInfo.ModelName, clientInfo.Priority);
                        }

                        return (true, updates, errors);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new Exception($"模型 {clientInfo.ModelName} 流式调用失败: {ex.Message}", ex));

                    _logger?.LogWarning(ex,
                        "模型流式调用失败: {ModelName} (优先级: {Priority}, 重试: {Retry}/{MaxRetries})",
                        clientInfo.ModelName, clientInfo.Priority, retry + 1, _maxRetries);

                    if (retry < _maxRetries - 1)
                    {
                        await Task.Delay(_retryDelay, cancellationToken);
                    }
                }
            }

            if (clientInfo.Priority < _clients.Count)
            {
                _logger?.LogWarning(
                    "主模型 {ModelName} 流式调用失败，切换到副模型...",
                    clientInfo.ModelName);
            }
        }

        return (false, null, errors);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var clientInfo in _clients)
            {
                try
                {
                    clientInfo.Client.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "释放客户端资源失败: {ModelName}", clientInfo.ModelName);
                }
            }
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// ChatClient信息
/// </summary>
public class ChatClientInfo
{
    /// <summary>
    /// ChatClient实例
    /// </summary>
    public IChatClient Client { get; set; } = null!;

    /// <summary>
    /// 模型名称
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// 优先级（1=主模型，2+=副模型）
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// LLM配置ID
    /// </summary>
    public long LlmConfigId { get; set; }

    /// <summary>
    /// 模型配置ID
    /// </summary>
    public long? LlmModelConfigId { get; set; }
}
