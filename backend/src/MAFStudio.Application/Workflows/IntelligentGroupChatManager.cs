using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Workflows;

/// <summary>
/// AI智能选择模式的群聊管理器
/// 使用AI模型选择下一个发言者
/// </summary>
public class IntelligentGroupChatManager : GroupChatManager
{
    private readonly AIAgent _orchestratorAgent;
    private readonly List<AIAgent> _allAgents;
    private readonly IChatClient _chatClient;
    private readonly List<string> _validAgentNames;
    private readonly ILogger<IntelligentGroupChatManager>? _logger;
    private int _lastAgentIndex = 0;

    public IntelligentGroupChatManager(
        AIAgent orchestratorAgent,
        IReadOnlyList<AIAgent> allAgents,
        IChatClient chatClient,
        int maximumIterationCount = 10,
        ILogger<IntelligentGroupChatManager>? logger = null)
    {
        _orchestratorAgent = orchestratorAgent;
        _allAgents = allAgents.ToList();
        _chatClient = chatClient;
        _logger = logger;
        _validAgentNames = allAgents
            .Where(a => !string.IsNullOrEmpty(a.Name))
            .Select(a => a.Name!)
            .ToList();
        MaximumIterationCount = maximumIterationCount;
        
        _logger?.LogInformation("[IntelligentGroupChatManager] 可用Agent: {Agents}", string.Join(", ", _validAgentNames));
    }

    protected override async ValueTask<AIAgent?> SelectNextAgentAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (IterationCount == 0)
        {
            _logger?.LogInformation("[IntelligentGroupChatManager] 第一轮，选择协调者: {Orchestrator}", _orchestratorAgent.Name);
            return _orchestratorAgent;
        }

        var agentDescriptions = string.Join("\n", _allAgents
            .Where(a => !string.IsNullOrEmpty(a.Name))
            .Select(a => $"- {a.Name}: {a.Description ?? "团队成员"}"));

        var validNamesStr = string.Join("、", _validAgentNames);

        var historyText = string.Join("\n", history.TakeLast(3).Select(m => 
        {
            var text = m.Text ?? "";
            var preview = text.Length > 150 ? text.Substring(0, 150) + "..." : text;
            return $"{m.Role}: {preview}";
        }));

        var prompt = $@"你是一个协调者，负责从团队成员中选择下一个发言的人。

【重要规则】
1. 你只能从以下团队成员中选择，绝对不能选择团队以外的人
2. 团队成员列表：{validNamesStr}
3. 只需返回一个成员的名字，不要返回其他任何内容
4. 如果消息中@了某个成员，优先选择那个成员

团队成员详情：
{agentDescriptions}

最近的对话：
{historyText}

请从团队成员【{validNamesStr}】中选择最适合下一个发言的人。
只返回名字，不要其他内容：";

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, prompt)
            };
            
            var response = await _chatClient.GetResponseAsync(
                messages,
                cancellationToken: cancellationToken);

            var selectedName = response.Messages.LastOrDefault()?.Text?.Trim() ?? "";
            _logger?.LogInformation("[IntelligentGroupChatManager] AI选择: {SelectedName}", selectedName);
            
            var selectedAgent = _allAgents.FirstOrDefault(a => 
                !string.IsNullOrEmpty(a.Name) && 
                (a.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase) ||
                 selectedName.Contains(a.Name, StringComparison.OrdinalIgnoreCase) ||
                 a.Name.Contains(selectedName, StringComparison.OrdinalIgnoreCase)));

            if (selectedAgent == null)
            {
                _logger?.LogInformation("[IntelligentGroupChatManager] AI返回的名字'{SelectedName}'不在团队中，使用轮询方式选择", selectedName);
                selectedAgent = _allAgents[_lastAgentIndex % _allAgents.Count];
                _lastAgentIndex++;
            }

            _logger?.LogInformation("[IntelligentGroupChatManager] 最终选择: {AgentName}", selectedAgent?.Name);
            return selectedAgent ?? _orchestratorAgent;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "[IntelligentGroupChatManager] 选择失败，使用轮询方式");
            var fallbackAgent = _allAgents[_lastAgentIndex % _allAgents.Count];
            _lastAgentIndex++;
            return fallbackAgent;
        }
    }

    protected override ValueTask<bool> ShouldTerminateAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (IterationCount >= MaximumIterationCount)
        {
            _logger?.LogInformation("[IntelligentGroupChatManager] 达到最大迭代次数: {IterationCount}", IterationCount);
            return new ValueTask<bool>(true);
        }

        if (history.Count > 0)
        {
            var lastMessage = history[history.Count - 1];
            var content = lastMessage.Text ?? string.Empty;
            
            var terminateKeywords = new[] { "任务完成", "会议结束", "讨论结束", "TASK_COMPLETE", "最终结论", "总结完毕" };
            if (terminateKeywords.Any(k => content.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                _logger?.LogInformation("[IntelligentGroupChatManager] 检测到结束关键词");
                return new ValueTask<bool>(true);
            }
        }

        return new ValueTask<bool>(false);
    }

    protected override void Reset()
    {
        base.Reset();
        _lastAgentIndex = 0;
    }
}
