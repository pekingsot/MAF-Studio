using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Workflows;

/// <summary>
/// 协调者模式的群聊管理器
/// Manager负责协调Worker之间的对话
/// </summary>
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
        
        _logger?.LogInformation("[ManagerGroupChatManager] Manager: {Manager}, Workers: {Workers}", 
            _managerAgentName, string.Join(", ", _workerAgentNames));
    }

    protected override ValueTask<AIAgent?> SelectNextAgentAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("[ManagerGroupChatManager] IterationCount: {IterationCount}, ManagerJustSpoke: {ManagerJustSpoke}", 
            IterationCount, _managerJustSpoke);

        if (IterationCount == 0)
        {
            _managerJustSpoke = true;
            _logger?.LogInformation("[ManagerGroupChatManager] 第一轮，选择 Manager: {Manager}", _managerAgentName);
            return new ValueTask<AIAgent?>(_agentMap[_managerAgentName]);
        }

        if (_managerJustSpoke)
        {
            _managerJustSpoke = false;
            
            if (_workerAgentNames.Count == 0)
            {
                _logger?.LogInformation("[ManagerGroupChatManager] 没有Worker，选择 Manager: {Manager}", _managerAgentName);
                return new ValueTask<AIAgent?>(_agentMap[_managerAgentName]);
            }

            var lastMessage = history.LastOrDefault();
            var lastMessageText = lastMessage?.Text ?? "";
            
            _logger?.LogInformation("[ManagerGroupChatManager] 最后一条消息: {Message}", 
                lastMessageText.Substring(0, Math.Min(200, lastMessageText.Length)));
            
            var mentionedAgent = FindMentionedAgent(lastMessageText);
            if (mentionedAgent != null)
            {
                _logger?.LogInformation("[ManagerGroupChatManager] 检测到@提及，选择: {Agent}", mentionedAgent);
                return new ValueTask<AIAgent?>(_agentMap[mentionedAgent]);
            }

            var nextWorkerName = _workerAgentNames[_currentWorkerIndex % _workerAgentNames.Count];
            _currentWorkerIndex++;
            _logger?.LogInformation("[ManagerGroupChatManager] 无@提及，轮询选择 Worker: {Worker}", nextWorkerName);
            return new ValueTask<AIAgent?>(_agentMap[nextWorkerName]);
        }

        _managerJustSpoke = true;
        _logger?.LogInformation("[ManagerGroupChatManager] Worker发言后，选择 Manager: {Manager}", _managerAgentName);
        return new ValueTask<AIAgent?>(_agentMap[_managerAgentName]);
    }

    private string? FindMentionedAgent(string message)
    {
        _logger?.LogInformation("[ManagerGroupChatManager] 查找@提及，可用Workers: {Workers}", 
            string.Join(", ", _workerAgentNames));
        
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
                    _logger?.LogInformation("[ManagerGroupChatManager] 发现@提及: pattern={Pattern}, agentName={AgentName}, position={Index}", 
                        pattern, agentName, index);
                }
            }
        }
        
        return lastMentionedAgent;
    }

    protected override ValueTask<bool> ShouldTerminateAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (IterationCount >= MaximumIterationCount)
        {
            _logger?.LogInformation("[ManagerGroupChatManager] 达到最大迭代次数: {IterationCount}", IterationCount);
            return new ValueTask<bool>(true);
        }

        if (history.Count > 0)
        {
            var lastMessage = history[history.Count - 1];
            var content = lastMessage.Text ?? string.Empty;
            
            var terminateKeywords = new[] { "任务完成", "会议结束", "讨论结束", "TASK_COMPLETE" };
            if (terminateKeywords.Any(k => content.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                _logger?.LogInformation("[ManagerGroupChatManager] 检测到结束关键词");
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
    }
}
