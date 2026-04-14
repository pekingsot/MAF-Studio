using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Workflows.Selection;

public interface IAgentSelectionStrategy
{
    string Name { get; }

    ValueTask<AIAgent?> SelectAsync(AgentSelectionContext context, CancellationToken ct = default);
}

public class AgentSelectionContext
{
    public required IReadOnlyList<ChatMessage> History { get; init; }
    public required AIAgent OrchestratorAgent { get; init; }
    public required IReadOnlyList<AIAgent> WorkerAgents { get; init; }
    public required IReadOnlyList<AIAgent> AllAgents { get; init; }
    public string? LastOrchestratorRawText { get; init; }
    public int IterationCount { get; init; }
}

public class MentionSelectionStrategy : IAgentSelectionStrategy
{
    private static readonly Regex MentionPattern = new(@"@([^\s@、，。！？,\n]+)", RegexOptions.Compiled);

    public string Name => "Mention";

    public ValueTask<AIAgent?> SelectAsync(AgentSelectionContext context, CancellationToken ct = default)
    {
        var text = context.LastOrchestratorRawText;
        if (string.IsNullOrEmpty(text))
        {
            var lastMsg = context.History.LastOrDefault();
            if (lastMsg?.AuthorName == context.OrchestratorAgent.Name)
            {
                text = lastMsg.Text;
            }
        }

        if (string.IsNullOrEmpty(text))
        {
            return new ValueTask<AIAgent?>((AIAgent?)null);
        }

        AIAgent? lastMentionedAgent = null;
        var matches = MentionPattern.Matches(text);

        foreach (Match match in matches)
        {
            var mentionedName = match.Groups[1].Value.Trim();
            var agent = context.WorkerAgents.FirstOrDefault(a =>
                !string.IsNullOrEmpty(a.Name) &&
                a.Name.Equals(mentionedName, StringComparison.OrdinalIgnoreCase));

            if (agent != null)
            {
                lastMentionedAgent = agent;
            }
        }

        return new ValueTask<AIAgent?>(lastMentionedAgent);
    }
}

public class RoundRobinSelectionStrategy : IAgentSelectionStrategy
{
    private int _currentIndex;

    public string Name => "RoundRobin";

    public ValueTask<AIAgent?> SelectAsync(AgentSelectionContext context, CancellationToken ct = default)
    {
        if (context.WorkerAgents.Count == 0)
        {
            return new ValueTask<AIAgent?>((AIAgent?)null);
        }

        var agent = context.WorkerAgents[_currentIndex % context.WorkerAgents.Count];
        _currentIndex++;
        return new ValueTask<AIAgent?>(agent);
    }

    public void Reset() => _currentIndex = 0;
}

public class CompositeSelectionStrategy : IAgentSelectionStrategy
{
    private readonly List<IAgentSelectionStrategy> _strategies;
    private readonly ILogger? _logger;

    public string Name => "Composite";

    public CompositeSelectionStrategy(IEnumerable<IAgentSelectionStrategy> strategies, ILogger? logger = null)
    {
        _strategies = strategies.ToList();
        _logger = logger;
    }

    public async ValueTask<AIAgent?> SelectAsync(AgentSelectionContext context, CancellationToken ct = default)
    {
        foreach (var strategy in _strategies)
        {
            var agent = await strategy.SelectAsync(context, ct);
            if (agent != null)
            {
                _logger?.LogInformation("[CompositeSelection] 策略 {Strategy} 选中: {Agent}", strategy.Name, agent.Name);
                return agent;
            }
            _logger?.LogInformation("[CompositeSelection] 策略 {Strategy} 未选中，尝试下一个", strategy.Name);
        }

        _logger?.LogWarning("[CompositeSelection] 所有策略均未选中，返回null");
        return null;
    }
}

public static class AgentSelectionStrategyFactory
{
    public static IAgentSelectionStrategy CreateOrchestratedStrategy(ILogger? logger = null)
    {
        return new CompositeSelectionStrategy(
        [
            new MentionSelectionStrategy(),
            new RoundRobinSelectionStrategy()
        ], logger);
    }
}
