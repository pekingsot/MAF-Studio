using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace MAFStudio.Application.Workflows;

public class ManagerGroupChatManager : GroupChatManager
{
    private readonly string _managerAgentName;
    private readonly List<string> _workerAgentNames;
    private readonly Dictionary<string, AIAgent> _agentMap;
    private int _currentWorkerIndex = 0;
    private bool _managerJustSpoke = false;

    public ManagerGroupChatManager(
        AIAgent managerAgent,
        IReadOnlyList<AIAgent> allAgents,
        int maximumIterationCount = 10)
    {
        _managerAgentName = managerAgent.Name ?? "Manager";
        _agentMap = allAgents
            .Where(a => a.Name != null)
            .ToDictionary(a => a.Name!, a => a);
        _workerAgentNames = allAgents
            .Where(a => a.Name != managerAgent.Name && a.Name != null)
            .Select(a => a.Name!)
            .ToList();
        
        MaximumIterationCount = maximumIterationCount;
        
        Console.WriteLine($"[ManagerGroupChatManager] Manager: {_managerAgentName}");
        Console.WriteLine($"[ManagerGroupChatManager] Workers: {string.Join(", ", _workerAgentNames)}");
    }

    protected override ValueTask<AIAgent?> SelectNextAgentAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[ManagerGroupChatManager] IterationCount: {IterationCount}, ManagerJustSpoke: {_managerJustSpoke}");

        if (IterationCount == 0)
        {
            _managerJustSpoke = true;
            Console.WriteLine($"[ManagerGroupChatManager] 选择 Manager: {_managerAgentName}");
            return new ValueTask<AIAgent?>(_agentMap[_managerAgentName]);
        }

        if (_managerJustSpoke)
        {
            _managerJustSpoke = false;
            
            if (_workerAgentNames.Count == 0)
            {
                Console.WriteLine($"[ManagerGroupChatManager] 没有Worker，选择 Manager: {_managerAgentName}");
                return new ValueTask<AIAgent?>(_agentMap[_managerAgentName]);
            }

            var nextWorkerName = _workerAgentNames[_currentWorkerIndex % _workerAgentNames.Count];
            _currentWorkerIndex++;
            Console.WriteLine($"[ManagerGroupChatManager] 选择 Worker: {nextWorkerName}");
            return new ValueTask<AIAgent?>(_agentMap[nextWorkerName]);
        }

        _managerJustSpoke = true;
        Console.WriteLine($"[ManagerGroupChatManager] Worker发言后，选择 Manager: {_managerAgentName}");
        return new ValueTask<AIAgent?>(_agentMap[_managerAgentName]);
    }

    protected override ValueTask<bool> ShouldTerminateAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (IterationCount >= MaximumIterationCount)
        {
            Console.WriteLine($"[ManagerGroupChatManager] 达到最大迭代次数: {IterationCount}");
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
                Console.WriteLine($"[ManagerGroupChatManager] 检测到结束关键词");
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
