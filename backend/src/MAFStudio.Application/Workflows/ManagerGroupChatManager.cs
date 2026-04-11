using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Workflows;

public class ManagerGroupChatManager : GroupChatManager
{
    private readonly AIAgent _managerAgent;
    private readonly string _managerAgentName;
    private readonly List<string> _workerAgentNames;
    private readonly Dictionary<string, AIAgent> _agentMap;
    private readonly ILogger<ManagerGroupChatManager>? _logger;
    private int _currentWorkerIndex = 0;
    
    public event EventHandler<ManagerThinkingEventArgs>? ManagerThinking;

    public ManagerGroupChatManager(
        AIAgent managerAgent,
        IReadOnlyList<AIAgent> allAgents,
        int maximumIterationCount = 10,
        ILogger<ManagerGroupChatManager>? logger = null)
    {
        _managerAgent = managerAgent;
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
        _logger?.LogInformation("[ManagerGroupChatManager] IterationCount: {IterationCount}", IterationCount);

        string thinking;
        string? selectedWorkerName = null;

        if (IterationCount == 0)
        {
            thinking = $"【任务启动】我是{_managerAgentName}，现在开始协调团队工作。\n\n" +
                      $"团队成员：{string.Join("、", _workerAgentNames)}\n\n" +
                      $"首先请 {_workerAgentNames.FirstOrDefault()} 发言，分享你的观点。";
            
            if (_workerAgentNames.Count > 0)
            {
                selectedWorkerName = _workerAgentNames[0];
                var firstWorker = _agentMap[selectedWorkerName];
                
                OnManagerThinking(thinking, selectedWorkerName);
                
                return new ValueTask<AIAgent?>(firstWorker!);
            }
            
            OnManagerThinking(thinking, null);
            return new ValueTask<AIAgent?>((AIAgent?)null);
        }

        var lastMessage = history.LastOrDefault();
        var lastMessageText = lastMessage?.Text ?? "";
        
        _logger?.LogInformation("[ManagerGroupChatManager] 最后一条消息: {Message}", 
            lastMessageText.Substring(0, Math.Min(200, lastMessageText.Length)));
        
        var mentionedAgent = FindMentionedAgent(lastMessageText);
        if (mentionedAgent != null)
        {
            thinking = $"【协调决策】检测到 @{mentionedAgent} 被提及，现在请 {mentionedAgent} 继续发言。";
            selectedWorkerName = mentionedAgent;
            
            OnManagerThinking(thinking, selectedWorkerName);
            
            var agent = _agentMap[mentionedAgent];
            return new ValueTask<AIAgent?>(agent!);
        }

        var nextWorkerName = _workerAgentNames[_currentWorkerIndex % _workerAgentNames.Count];
        _currentWorkerIndex++;
        
        thinking = $"【协调决策】根据轮询规则，现在请 {nextWorkerName} 发言。";
        selectedWorkerName = nextWorkerName;
        
        OnManagerThinking(thinking, selectedWorkerName);
        
        var nextWorker = _agentMap[nextWorkerName];
        return new ValueTask<AIAgent?>(nextWorker!);
    }

    protected virtual void OnManagerThinking(string thinking, string? selectedAgent)
    {
        ManagerThinking?.Invoke(this, new ManagerThinkingEventArgs(_managerAgentName, thinking, selectedAgent, IterationCount));
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
    }
}
