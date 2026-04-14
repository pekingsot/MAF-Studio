using MAFStudio.Application.DTOs;
using MAFStudio.Application.Workflows;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MAFStudio.Application.Services;

public interface IWorkflowEventProcessor
{
    IAsyncEnumerable<ChatMessageDto> ProcessStreamAsync(
        IAsyncEnumerable<object> events,
        WorkflowProcessingContext context,
        CancellationToken cancellationToken = default);
}

public class WorkflowProcessingContext
{
    public required long CollaborationId { get; init; }
    public required long? TaskId { get; init; }
    public required long SessionId { get; init; }
    public required Dictionary<string, string> AgentIdToNameMap { get; init; }
    public required Dictionary<string, long> AgentIdMap { get; init; }
    public required List<CollaborationAgent> Members { get; init; }
    public required ConcurrentQueue<ManagerThinkingEventArgs> ThinkingQueue { get; init; }
}

public class WorkflowEventProcessor : IWorkflowEventProcessor
{
    private readonly IMessageRepository _messageRepository;
    private readonly IWorkflowSessionRepository _sessionRepository;
    private readonly ILogger<WorkflowEventProcessor> _logger;

    public WorkflowEventProcessor(
        IMessageRepository messageRepository,
        IWorkflowSessionRepository sessionRepository,
        ILogger<WorkflowEventProcessor> logger)
    {
        _messageRepository = messageRepository;
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    public async IAsyncEnumerable<ChatMessageDto> ProcessStreamAsync(
        IAsyncEnumerable<object> events,
        WorkflowProcessingContext context,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var aggregator = new StreamingMessageAggregator(context, _logger);

        await foreach (var evt in events.ConfigureAwait(false))
        {
            _logger.LogInformation("收到事件: {EventType}", evt.GetType().Name);

            if (evt is AgentResponseUpdateEvent updateEvent)
            {
                foreach (var msg in aggregator.OnAgentUpdate(updateEvent))
                {
                    await PersistMessageAsync(msg, context);
                    yield return msg;
                }

                foreach (var msg in aggregator.FlushThinkingMessages())
                {
                    yield return msg;
                }
            }
            else if (evt is WorkflowOutputEvent)
            {
                foreach (var msg in aggregator.OnWorkflowOutput())
                {
                    await PersistMessageAsync(msg, context);
                    yield return msg;
                }

                _logger.LogInformation("GroupChat工作流正常结束");
                yield break;
            }
            else if (evt is ExecutorFailedEvent failedEvent)
            {
                foreach (var msg in aggregator.OnExecutorFailed(failedEvent))
                {
                    await PersistMessageAsync(msg, context);
                    yield return msg;
                }
            }
            else if (evt is WorkflowErrorEvent errorEvent)
            {
                foreach (var msg in aggregator.OnWorkflowError(errorEvent))
                {
                    await PersistMessageAsync(msg, context);
                    yield return msg;
                }
                break;
            }
        }

        foreach (var msg in aggregator.FlushRemaining())
        {
            await PersistMessageAsync(msg, context);
            yield return msg;
        }
    }

    private async Task PersistMessageAsync(ChatMessageDto msg, WorkflowProcessingContext context)
    {
        if (msg.Role == "system" && msg.Metadata?.TryGetValue("type", out var type) == true
            && type.ToString() == "manager_thinking")
        {
            return;
        }

        if (context.AgentIdMap.TryGetValue(msg.Sender.TrimStart('_'), out var agentId) ||
            TryFindAgentIdByName(msg.Sender, context, out agentId))
        {
            var member = context.Members.FirstOrDefault(m => m.AgentId == agentId);
            await _messageRepository.CreateAsync(new Message
            {
                SessionId = context.SessionId,
                CollaborationId = context.CollaborationId,
                TaskId = context.TaskId,
                MessageType = msg.Role == "system" ? "error" : "coordination",
                RoundNumber = 0,
                FromAgentId = agentId,
                FromAgentName = msg.Sender,
                FromAgentRole = member?.Role,
                Content = msg.Content
            });
            await _sessionRepository.IncrementMessageCountAsync(context.SessionId);
        }
    }

    private bool TryFindAgentIdByName(string name, WorkflowProcessingContext context, out long agentId)
    {
        agentId = 0;
        var entry = context.AgentIdToNameMap.FirstOrDefault(kvp => kvp.Value == name);
        if (entry.Key != null)
        {
            return context.AgentIdMap.TryGetValue(entry.Key.TrimStart('_'), out agentId);
        }
        return false;
    }
}

internal class StreamingMessageAggregator
{
    private readonly WorkflowProcessingContext _context;
    private readonly ILogger _logger;
    private string? _currentAgentId;
    private readonly System.Text.StringBuilder _currentContent = new();
    private int _roundNumber;

    public StreamingMessageAggregator(WorkflowProcessingContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public IEnumerable<ChatMessageDto> OnAgentUpdate(AgentResponseUpdateEvent updateEvent)
    {
        var executorId = updateEvent.ExecutorId ?? "Agent";
        var updateText = updateEvent.Update?.Text ?? "";

        _logger.LogInformation("收到AgentResponseUpdateEvent: ExecutorId={ExecutorId}, TextLength={TextLength}",
            executorId, updateText.Length);

        var results = new List<ChatMessageDto>();

        if (executorId != _currentAgentId)
        {
            if (_currentAgentId != null && _currentContent.Length > 0)
            {
                results.Add(FlushCurrentAgent());
            }

            _currentAgentId = executorId;
            _currentContent.Clear();
            _logger.LogInformation("Agent切换: {ExecutorId}, 名称: {Name}", executorId, GetAgentName(executorId));
        }

        if (updateEvent.Update != null && !string.IsNullOrEmpty(updateEvent.Update.Text))
        {
            _currentContent.Append(updateEvent.Update.Text);
        }

        return results;
    }

    public IEnumerable<ChatMessageDto> OnWorkflowOutput()
    {
        var results = new List<ChatMessageDto>();
        if (_currentAgentId != null && _currentContent.Length > 0)
        {
            results.Add(FlushCurrentAgent());
        }
        return results;
    }

    public IEnumerable<ChatMessageDto> OnExecutorFailed(ExecutorFailedEvent failedEvent)
    {
        var results = new List<ChatMessageDto>();

        if (_currentAgentId != null && _currentContent.Length > 0)
        {
            results.Add(FlushCurrentAgent());
        }

        var failedExecutorId = failedEvent.ExecutorId ?? "Unknown";
        var failedAgentName = GetAgentName(failedExecutorId);
        var failedData = failedEvent.Data?.ToString() ?? "";

        _logger.LogWarning("执行器失败: ExecutorId={ExecutorId}, Agent={AgentName}",
            failedExecutorId, failedAgentName);

        var friendlyError = ParseModelErrorMessage(failedData);

        results.Add(new ChatMessageDto
        {
            Sender = failedAgentName,
            Content = friendlyError,
            Timestamp = DateTime.UtcNow,
            Role = "system",
            Metadata = new Dictionary<string, object>
            {
                ["type"] = "model_error",
                ["executorId"] = failedExecutorId
            }
        });

        return results;
    }

    public IEnumerable<ChatMessageDto> OnWorkflowError(WorkflowErrorEvent errorEvent)
    {
        var results = new List<ChatMessageDto>();

        if (_currentAgentId != null && _currentContent.Length > 0)
        {
            results.Add(FlushCurrentAgent());
        }

        var errorMsg = errorEvent.Exception?.Message ?? "未知错误";
        var innerMsg = errorEvent.Exception?.InnerException?.Message ?? "";

        _logger.LogError("工作流执行错误: {Error}\n内部异常: {InnerError}", errorMsg, innerMsg);

        var fullError = errorMsg + (string.IsNullOrEmpty(innerMsg) ? "" : $"\n内部异常: {innerMsg}");
        var friendlyError = ParseModelErrorMessage(fullError);

        results.Add(new ChatMessageDto
        {
            Sender = "System",
            Content = friendlyError,
            Timestamp = DateTime.UtcNow,
            Role = "system",
            Metadata = new Dictionary<string, object>
            {
                ["type"] = "workflow_error"
            }
        });

        return results;
    }

    public IEnumerable<ChatMessageDto> FlushThinkingMessages()
    {
        var results = new List<ChatMessageDto>();

        while (_context.ThinkingQueue.TryDequeue(out var thinkingArgs))
        {
            _logger.LogInformation("发送Manager思考过程: {Thinking}", thinkingArgs.Thinking);

            results.Add(new ChatMessageDto
            {
                Sender = thinkingArgs.ManagerName,
                Content = thinkingArgs.Thinking,
                Timestamp = DateTime.UtcNow,
                Role = "system",
                Metadata = new Dictionary<string, object>
                {
                    ["type"] = "manager_thinking",
                    ["selectedAgent"] = thinkingArgs.SelectedAgent ?? "",
                    ["iterationCount"] = thinkingArgs.IterationCount
                }
            });
        }

        return results;
    }

    public IEnumerable<ChatMessageDto> FlushRemaining()
    {
        var results = new List<ChatMessageDto>();
        if (_currentAgentId != null && _currentContent.Length > 0)
        {
            results.Add(FlushCurrentAgent());
        }
        return results;
    }

    private ChatMessageDto FlushCurrentAgent()
    {
        var agentName = GetAgentName(_currentAgentId!);
        var content = _currentContent.ToString();
        _roundNumber++;

        _currentAgentId = null;
        _currentContent.Clear();

        return new ChatMessageDto
        {
            Sender = agentName,
            Content = content,
            Timestamp = DateTime.UtcNow,
            Role = "assistant"
        };
    }

    private string GetAgentName(string agentId)
    {
        var normalizedId = agentId.TrimStart('_');
        if (_context.AgentIdToNameMap.TryGetValue(normalizedId, out var name))
        {
            return name;
        }
        if (_context.AgentIdToNameMap.TryGetValue(agentId, out name))
        {
            return name;
        }
        return agentId;
    }

    private static string ParseModelErrorMessage(string errorText)
    {
        if (string.IsNullOrEmpty(errorText))
            return "❌ 工作流执行失败: 未知错误";

        if (errorText.Contains("ModelCallFailedException", StringComparison.OrdinalIgnoreCase))
        {
            return "⚠️ 模型调用失败\n\n所有配置的模型均无法响应，请检查：\n  1. 免费额度是否已耗尽\n  2. API Key 是否有效\n  3. 模型服务是否可用\n\n请在管理后台检查模型配置后重试。";
        }

        if (errorText.Contains("FreeTierOnly", StringComparison.OrdinalIgnoreCase) ||
            errorText.Contains("free tier", StringComparison.OrdinalIgnoreCase) ||
            errorText.Contains("免费额度", StringComparison.Ordinal))
        {
            return "⚠️ 模型调用失败：免费额度已耗尽\n\n请在管理后台关闭\"仅使用免费额度\"模式，或配置有付费额度的模型后重试。";
        }

        if (errorText.Contains("401", StringComparison.OrdinalIgnoreCase) ||
            errorText.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return "⚠️ 模型调用失败：API Key 无效或已过期\n\n请在管理后台检查模型配置中的 API Key 是否正确。";
        }

        if (errorText.Contains("429", StringComparison.OrdinalIgnoreCase) ||
            errorText.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
        {
            return "⚠️ 模型调用失败：请求频率超限\n\n模型服务当前请求过多，请稍后重试。";
        }

        if (errorText.Contains("403", StringComparison.OrdinalIgnoreCase))
        {
            return "⚠️ 模型调用失败：访问被拒绝\n\n可能原因：额度不足或权限不够，请检查模型配置。";
        }

        if (errorText.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            errorText.Contains("timed out", StringComparison.OrdinalIgnoreCase))
        {
            return "⚠️ 模型调用失败：请求超时\n\n模型服务响应过慢，请稍后重试或更换模型。";
        }

        if (errorText.Contains("connection", StringComparison.OrdinalIgnoreCase))
        {
            return "⚠️ 模型调用失败：网络连接失败\n\n无法连接到模型服务，请检查网络和模型配置。";
        }

        var shortError = errorText.Length > 200 ? errorText.Substring(0, 200) + "..." : errorText;
        return $"❌ 工作流执行失败: {shortError}";
    }
}
