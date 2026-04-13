using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MAFStudio.Application.Clients;

public class FallbackChatClient : DelegatingChatClient
{
    private readonly List<ChatClientInfo> _clients;
    private readonly ILogger? _logger;
    private readonly int _maxRetries;
    private readonly TimeSpan _retryDelay;

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
        var errors = new List<Exception>();

        foreach (var clientInfo in _clients)
        {
            for (int retry = 0; retry < _maxRetries; retry++)
            {
                var initResult = await TryInitStreamAsync(
                    clientInfo, messages, options, cancellationToken);

                if (initResult.Error != null)
                {
                    errors.Add(initResult.Error.Value.Exception);

                    _logger?.LogWarning(initResult.Error.Value.Exception,
                        "模型流式调用失败(未输出任何内容): {ModelName} (优先级: {Priority}, 重试: {Retry}/{MaxRetries})",
                        clientInfo.ModelName, clientInfo.Priority, retry + 1, _maxRetries);

                    if (retry < _maxRetries - 1)
                    {
                        await Task.Delay(_retryDelay, cancellationToken);
                    }
                    continue;
                }

                if (initResult.IsEmpty)
                {
                    _logger?.LogWarning(
                        "模型 {ModelName} 流式调用返回空响应 (重试: {Retry}/{MaxRetries})",
                        clientInfo.ModelName, retry + 1, _maxRetries);

                    if (retry < _maxRetries - 1)
                    {
                        await Task.Delay(_retryDelay, cancellationToken);
                    }
                    continue;
                }

                if (retry > 0 || clientInfo.Priority > 1)
                {
                    _logger?.LogInformation(
                        "模型流式调用成功: {ModelName} (优先级: {Priority})",
                        clientInfo.ModelName, clientInfo.Priority);
                }

                yield return initResult.FirstUpdate!;

                var remaining = initResult.RemainingStream!;
                while (await remaining.MoveNextAsync())
                {
                    yield return remaining.Current;
                }

                await remaining.DisposeAsync();
                yield break;
            }

            if (clientInfo.Priority < _clients.Count)
            {
                _logger?.LogWarning(
                    "主模型 {ModelName} 流式调用失败，切换到副模型...",
                    clientInfo.ModelName);
            }
        }

        _logger?.LogWarning("所有模型流式调用均未产生输出，降级为非流式调用...");

        ChatResponse? fallbackResponse = null;
        try
        {
            fallbackResponse = await GetResponseAsync(messages, options, cancellationToken);
        }
        catch (Exception fallbackEx)
        {
            errors.Add(new Exception($"非流式降级调用也失败: {fallbackEx.Message}", fallbackEx));
        }

        if (fallbackResponse != null)
        {
            var text = fallbackResponse.Text ?? "";
            if (!string.IsNullOrEmpty(text))
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, text)
                {
                    ResponseId = fallbackResponse.ResponseId,
                    ModelId = fallbackResponse.ModelId
                };
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

    private async Task<StreamInitResult> TryInitStreamAsync(
        ChatClientInfo clientInfo,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        _logger?.LogInformation(
            "尝试使用模型(流式): {ModelName} (优先级: {Priority})",
            clientInfo.ModelName, clientInfo.Priority);

        IAsyncEnumerator<ChatResponseUpdate>? enumerator = null;

        try
        {
            enumerator = clientInfo.Client
                .GetStreamingResponseAsync(messages, options, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);

            if (!await enumerator.MoveNextAsync())
            {
                await enumerator.DisposeAsync();
                return StreamInitResult.Empty();
            }

            var firstUpdate = enumerator.Current;
            return StreamInitResult.Success(firstUpdate, enumerator);
        }
        catch (Exception ex)
        {
            await enumerator.DisposeAsync();
            return StreamInitResult.Fail((ex, ex.Message));
        }
    }

    private readonly struct StreamInitResult
    {
        public bool IsEmpty { get; }
        public ChatResponseUpdate? FirstUpdate { get; }
        public IAsyncEnumerator<ChatResponseUpdate>? RemainingStream { get; }
        public (Exception Exception, string Message)? Error { get; }

        private StreamInitResult(bool isEmpty, ChatResponseUpdate? firstUpdate,
            IAsyncEnumerator<ChatResponseUpdate>? remainingStream,
            (Exception, string)? error)
        {
            IsEmpty = isEmpty;
            FirstUpdate = firstUpdate;
            RemainingStream = remainingStream;
            Error = error;
        }

        public static StreamInitResult Success(ChatResponseUpdate firstUpdate, IAsyncEnumerator<ChatResponseUpdate> remaining) =>
            new(false, firstUpdate, remaining, null);

        public static StreamInitResult Empty() =>
            new(true, null, null, null);

        public static StreamInitResult Fail((Exception, string) error) =>
            new(false, null, null, error);
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

public class ChatClientInfo
{
    public IChatClient Client { get; set; } = null!;
    public string ModelName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public long LlmConfigId { get; set; }
    public long? LlmModelConfigId { get; set; }
}
