using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace MAFStudio.Application.Workflows;

public class IntelligentGroupChatManager : GroupChatManager
{
    private readonly AIAgent _orchestratorAgent;
    private readonly List<AIAgent> _allAgents;
    private readonly IChatClient _chatClient;
    private readonly List<string> _validAgentNames;
    private int _lastAgentIndex = 0;

    public IntelligentGroupChatManager(
        AIAgent orchestratorAgent,
        IReadOnlyList<AIAgent> allAgents,
        IChatClient chatClient,
        int maximumIterationCount = 10)
    {
        _orchestratorAgent = orchestratorAgent;
        _allAgents = allAgents.ToList();
        _chatClient = chatClient;
        _validAgentNames = allAgents
            .Where(a => !string.IsNullOrEmpty(a.Name))
            .Select(a => a.Name!)
            .ToList();
        MaximumIterationCount = maximumIterationCount;
        
        Console.WriteLine($"[IntelligentGroupChatManager] 可用Agent: {string.Join(", ", _validAgentNames)}");
    }

    private string? _lastSpeakerName;

    protected override ValueTask<IEnumerable<ChatMessage>?> UpdateHistoryAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        var updatedHistory = new List<ChatMessage>();
        
        Console.WriteLine($"[UpdateHistoryAsync] 原始消息历史，共 {history.Count} 条消息");
        
        foreach (var message in history)
        {
            var authorName = message.AuthorName;
            var text = message.Text ?? "";
            
            Console.WriteLine($"[UpdateHistoryAsync] 消息: Role={message.Role}, AuthorName={authorName ?? "NULL"}, Text={text.Substring(0, Math.Min(50, text.Length))}...");
            
            if (!string.IsNullOrEmpty(authorName))
            {
                var prefix = $"【{authorName}】";
                var updatedText = text.StartsWith(prefix) ? text : $"{prefix}{text}";
                var updatedMessage = new ChatMessage(message.Role, updatedText)
                {
                    AuthorName = authorName
                };
                updatedHistory.Add(updatedMessage);
            }
            else
            {
                var updatedText = message.Role == ChatRole.Assistant 
                    ? $"【未知发言者】{text}" 
                    : text;
                var updatedMessage = new ChatMessage(message.Role, updatedText);
                updatedHistory.Add(updatedMessage);
            }
        }
        
        Console.WriteLine($"[UpdateHistoryAsync] 更新后消息历史，共 {updatedHistory.Count} 条消息");
        return new ValueTask<IEnumerable<ChatMessage>>(updatedHistory);
    }

    protected override async ValueTask<AIAgent?> SelectNextAgentAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (IterationCount == 0)
        {
            Console.WriteLine($"[IntelligentGroupChatManager] 第一轮，选择协调者: {_orchestratorAgent.Name}");
            _lastSpeakerName = _orchestratorAgent.Name;
            return _orchestratorAgent;
        }

        var lastMessage = history.LastOrDefault();
        if (lastMessage != null && !string.IsNullOrEmpty(lastMessage.AuthorName))
        {
            _lastSpeakerName = lastMessage.AuthorName;
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

        Console.WriteLine($"[SelectNextAgentAsync] 选择提示词: {prompt}");

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
            Console.WriteLine($"[IntelligentGroupChatManager] AI选择: {selectedName}");
            
            var selectedAgent = _allAgents.FirstOrDefault(a => 
                !string.IsNullOrEmpty(a.Name) && 
                (a.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase) ||
                 selectedName.Contains(a.Name, StringComparison.OrdinalIgnoreCase) ||
                 a.Name.Contains(selectedName, StringComparison.OrdinalIgnoreCase)));

            if (selectedAgent == null)
            {
                Console.WriteLine($"[IntelligentGroupChatManager] AI返回的名字'{selectedName}'不在团队中，使用轮询方式选择");
                selectedAgent = _allAgents[_lastAgentIndex % _allAgents.Count];
                _lastAgentIndex++;
            }

            Console.WriteLine($"[IntelligentGroupChatManager] 最终选择: {selectedAgent?.Name}");
            return selectedAgent ?? _orchestratorAgent;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IntelligentGroupChatManager] 选择失败: {ex.Message}，使用轮询方式");
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
            Console.WriteLine($"[IntelligentGroupChatManager] 达到最大迭代次数: {IterationCount}");
            return new ValueTask<bool>(true);
        }

        if (history.Count > 0)
        {
            var lastMessage = history[history.Count - 1];
            var content = lastMessage.Text ?? string.Empty;
            
            var terminateKeywords = new[] { "任务完成", "会议结束", "讨论结束", "TASK_COMPLETE", "最终结论", "总结完毕" };
            if (terminateKeywords.Any(k => content.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"[IntelligentGroupChatManager] 检测到结束关键词");
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
