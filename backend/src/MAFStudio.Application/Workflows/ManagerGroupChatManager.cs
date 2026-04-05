using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Workflows;

public class ManagerGroupChatManager : GroupChatManager
{
    private readonly string _managerAgentName;
    private readonly List<string> _workerAgentNames;
    private readonly Dictionary<string, AIAgent> _agentMap;
    private readonly ILogger<ManagerGroupChatManager>? _logger;
    private int _currentWorkerIndex = 0;
    private bool _managerJustSpoke = false;

    public ManagerGroupChatManager(
        AIAgent managerAgent,
        IReadOnlyList<AIAgent> allAgents,
        int maximumIterationCount = 10,
        ILogger<ManagerGroupChatManager>? logger = null)
    {
        _managerAgentName = managerAgent.Name ?? "Manager";
        _agentMap = allAgents
            .Where(a => a.Name != null)
            .ToDictionary(a => a.Name!, a => a);
        _workerAgentNames = allAgents
            .Where(a => a.Name != managerAgent.Name && a.Name != null)
            .Select(a => a.Name!)
            .ToList();
        _logger = logger;
        
        MaximumIterationCount = maximumIterationCount;
        
        _logger?.LogInformation("[ManagerGroupChatManager] Manager: {Manager}", _managerAgentName);
        _logger?.LogInformation("[ManagerGroupChatManager] Workers: {Workers}", string.Join(", ", _workerAgentNames));
        
        Console.WriteLine($"[ManagerGroupChatManager] 初始化完成");
        Console.WriteLine($"  Manager: {_managerAgentName}");
        Console.WriteLine($"  Workers: {string.Join(", ", _workerAgentNames)}");
    }

    protected override ValueTask<IEnumerable<ChatMessage>?> UpdateHistoryAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[UpdateHistoryAsync] 收到 {history.Count} 条消息");
        
        var updatedHistory = new List<ChatMessage>();
        
        foreach (var message in history)
        {
            var authorName = message.AuthorName;
            var text = message.Text ?? "";
            
            Console.WriteLine($"  [{authorName ?? "Unknown"}]: {text.Substring(0, Math.Min(100, text.Length))}...");
            
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
                updatedHistory.Add(message);
            }
        }
        
        Console.WriteLine($"[UpdateHistoryAsync] 返回 {updatedHistory.Count} 条更新后的消息");
        return new ValueTask<IEnumerable<ChatMessage>>(updatedHistory);
    }

    protected override ValueTask<AIAgent?> SelectNextAgentAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"========== SelectNextAgentAsync ==========");
        Console.WriteLine($"IterationCount: {IterationCount}, ManagerJustSpoke: {_managerJustSpoke}");
        Console.WriteLine($"history.Count: {history.Count}");
        
        _logger?.LogInformation("[ManagerGroupChatManager] IterationCount: {IterationCount}, ManagerJustSpoke: {ManagerJustSpoke}", IterationCount, _managerJustSpoke);

        if (IterationCount == 0)
        {
            _managerJustSpoke = true;
            Console.WriteLine($"第一轮，选择 Manager: {_managerAgentName}");
            _logger?.LogInformation("[ManagerGroupChatManager] 第一轮，选择 Manager: {Manager}", _managerAgentName);
            return new ValueTask<AIAgent?>(_agentMap[_managerAgentName]);
        }

        if (_managerJustSpoke)
        {
            _managerJustSpoke = false;
            
            if (_workerAgentNames.Count == 0)
            {
                Console.WriteLine($"没有Worker，选择 Manager: {_managerAgentName}");
                _logger?.LogInformation("[ManagerGroupChatManager] 没有Worker，选择 Manager: {Manager}", _managerAgentName);
                return new ValueTask<AIAgent?>(_agentMap[_managerAgentName]);
            }

            var lastMessage = history.LastOrDefault();
            var lastMessageText = lastMessage?.Text ?? "";
            
            Console.WriteLine($"最后一条消息长度: {lastMessageText.Length}");
            Console.WriteLine($"最后一条消息内容: {lastMessageText}");
            
            _logger?.LogInformation("[ManagerGroupChatManager] 最后一条消息: {Message}", lastMessageText.Substring(0, Math.Min(200, lastMessageText.Length)));
            
            var mentionedAgent = FindMentionedAgent(lastMessageText);
            if (mentionedAgent != null)
            {
                Console.WriteLine($"检测到@提及，选择: {mentionedAgent}");
                _logger?.LogInformation("[ManagerGroupChatManager] 检测到@提及，选择: {Agent}", mentionedAgent);
                return new ValueTask<AIAgent?>(_agentMap[mentionedAgent]);
            }

            var nextWorkerName = _workerAgentNames[_currentWorkerIndex % _workerAgentNames.Count];
            _currentWorkerIndex++;
            Console.WriteLine($"无@提及，轮询选择 Worker: {nextWorkerName}");
            _logger?.LogInformation("[ManagerGroupChatManager] 无@提及，轮询选择 Worker: {Worker}", nextWorkerName);
            return new ValueTask<AIAgent?>(_agentMap[nextWorkerName]);
        }

        _managerJustSpoke = true;
        Console.WriteLine($"Worker发言后，选择 Manager: {_managerAgentName}");
        _logger?.LogInformation("[ManagerGroupChatManager] Worker发言后，选择 Manager: {Manager}", _managerAgentName);
        return new ValueTask<AIAgent?>(_agentMap[_managerAgentName]);
    }

    private string? FindMentionedAgent(string message)
    {
        Console.WriteLine($"查找@提及，可用Workers: {string.Join(", ", _workerAgentNames)}");
        _logger?.LogInformation("[ManagerGroupChatManager] 查找@提及，可用Workers: {Workers}", string.Join(", ", _workerAgentNames));
        
        string? lastMentionedAgent = null;
        int lastMentionIndex = -1;
        
        foreach (var agentName in _workerAgentNames)
        {
            var patterns = new[]
            {
                $"@{agentName}",
                $"@{agentName.Split('-').Last()}",
                $"@{agentName.Split('-').First()}"
            };
            
            foreach (var pattern in patterns)
            {
                var index = message.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (index >= 0 && index > lastMentionIndex)
                {
                    lastMentionIndex = index;
                    lastMentionedAgent = agentName;
                    Console.WriteLine($"发现@提及: pattern={pattern}, agentName={agentName}, position={index}");
                    _logger?.LogInformation("[ManagerGroupChatManager] 发现@提及: pattern={Pattern}, agentName={AgentName}, position={Index}", pattern, agentName, index);
                }
            }
        }
        
        if (lastMentionedAgent != null)
        {
            Console.WriteLine($"选择最后@提及的Agent: {lastMentionedAgent}");
            _logger?.LogInformation("[ManagerGroupChatManager] 选择最后@提及的Agent: {Agent}", lastMentionedAgent);
        }
        else
        {
            Console.WriteLine($"未找到@提及");
        }
        
        return lastMentionedAgent;
    }

    protected override ValueTask<bool> ShouldTerminateAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (IterationCount >= MaximumIterationCount)
        {
            Console.WriteLine($"达到最大迭代次数: {IterationCount}");
            return new ValueTask<bool>(true);
        }

        if (history.Count > 0)
        {
            var lastMessage = history[history.Count - 1];
            var content = lastMessage.Text ?? string.Empty;
            
            if (content.Contains("任务完成", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("会议结束", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("讨论结束", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("TASK_COMPLETE", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"检测到结束关键词");
                return new ValueTask<bool>(true);
            }
        }

        return new ValueTask<bool>(false);
    }

    protected override void Reset()
    {
        base.Reset();
        _currentWorkerIndex = 0;
        _managerJustSpoke = false;
        Console.WriteLine($"Reset 被调用");
    }
}
