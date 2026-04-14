using MAFStudio.Application.Workflows.Selection;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Workflows;

public class OrchestratedGroupChatManager : GroupChatManager
{
    private readonly AIAgent _orchestratorAgent;
    private readonly IReadOnlyList<AIAgent> _workerAgents;
    private readonly IAgentSelectionStrategy _selectionStrategy;
    private readonly ILogger? _logger;
    private string? _lastOrchestratorRawText;

    private static readonly string[] TerminateKeywords =
        ["TERMINATE", "TASK_COMPLETE", "任务完成", "会议结束", "讨论结束", "最终结论", "总结完毕"];

    public event EventHandler<ManagerThinkingEventArgs>? ManagerThinking;

    public OrchestratedGroupChatManager(
        AIAgent orchestratorAgent,
        IReadOnlyList<AIAgent> workerAgents,
        IAgentSelectionStrategy selectionStrategy,
        int maximumIterationCount = 10,
        ILogger? logger = null)
    {
        _orchestratorAgent = orchestratorAgent;
        _workerAgents = workerAgents;
        _selectionStrategy = selectionStrategy;
        _logger = logger;
        MaximumIterationCount = maximumIterationCount;

        _logger?.LogInformation(
            "[OrchestratedGroupChat] 协调者: {Orchestrator}, Workers: {Workers}, 策略: {Strategy}",
            _orchestratorAgent.Name,
            string.Join(", ", _workerAgents.Select(a => a.Name)),
            _selectionStrategy.Name);
    }

    protected override async ValueTask<AIAgent> SelectNextAgentAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (IterationCount == 0)
        {
            return OnFirstIteration();
        }

        var lastMessage = history.LastOrDefault();
        var isOrchestratorMessage = IsOrchestratorMessage(lastMessage);

        if (isOrchestratorMessage)
        {
            return await OnOrchestratorJustSpokeAsync(history, cancellationToken);
        }

        return OnWorkerJustSpoke();
    }

    protected override ValueTask<IEnumerable<ChatMessage>?> UpdateHistoryAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (history.Count > 0)
        {
            var lastMsg = history[history.Count - 1];
            if (IsOrchestratorMessage(lastMsg))
            {
                _lastOrchestratorRawText = lastMsg.Text;
            }
        }

        return new ValueTask<IEnumerable<ChatMessage>?>(history);
    }

    protected override ValueTask<bool> ShouldTerminateAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (IterationCount >= MaximumIterationCount)
        {
            _logger?.LogInformation("[OrchestratedGroupChat] 达到最大迭代: {Count}", IterationCount);
            return new ValueTask<bool>(true);
        }

        if (history.Count > 0)
        {
            var lastMessage = history[history.Count - 1];
            if (IsOrchestratorMessage(lastMessage))
            {
                var content = lastMessage.Text ?? string.Empty;
                if (TerminateKeywords.Any(k => content.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger?.LogInformation("[OrchestratedGroupChat] 协调者发出终止信号");
                    return new ValueTask<bool>(true);
                }
            }
        }

        return new ValueTask<bool>(false);
    }

    protected override void Reset()
    {
        base.Reset();
        _lastOrchestratorRawText = null;
        if (_selectionStrategy is RoundRobinSelectionStrategy rr)
        {
            rr.Reset();
        }
    }

    private AIAgent OnFirstIteration()
    {
        _logger?.LogInformation("[OrchestratedGroupChat] 第1轮，协调者先发言");
        EmitManagerThinking($"【任务启动】我是{_orchestratorAgent.Name}，开始协调团队工作。", null);
        return _orchestratorAgent;
    }

    private async ValueTask<AIAgent> OnOrchestratorJustSpokeAsync(
        IReadOnlyList<ChatMessage> history, CancellationToken ct)
    {
        var context = new AgentSelectionContext
        {
            History = history,
            OrchestratorAgent = _orchestratorAgent,
            WorkerAgents = _workerAgents,
            AllAgents = [_orchestratorAgent, .. _workerAgents],
            LastOrchestratorRawText = _lastOrchestratorRawText,
            IterationCount = IterationCount
        };

        var selected = await _selectionStrategy.SelectAsync(context, ct);

        if (selected != null)
        {
            _logger?.LogInformation("[OrchestratedGroupChat] 策略选中Worker: {Agent}", selected.Name);
            EmitManagerThinking($"【协调决策】请 @{selected.Name} 发言。", selected.Name);
            return selected;
        }

        _logger?.LogWarning("[OrchestratedGroupChat] 所有策略未选中，回退到协调者");
        return _orchestratorAgent;
    }

    private AIAgent OnWorkerJustSpoke()
    {
        _logger?.LogInformation("[OrchestratedGroupChat] Worker发言完毕，回到协调者");
        return _orchestratorAgent;
    }

    private bool IsOrchestratorMessage(ChatMessage? message)
    {
        return message != null
            && !string.IsNullOrEmpty(message.AuthorName)
            && message.AuthorName == _orchestratorAgent.Name;
    }

    private void EmitManagerThinking(string thinking, string? selectedAgent)
    {
        ManagerThinking?.Invoke(this,
            new ManagerThinkingEventArgs(_orchestratorAgent.Name!, thinking, selectedAgent, IterationCount));
    }
}
