using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Workflows;

public class IntelligentGroupChatManager : GroupChatManager
{
    private readonly AIAgent _orchestratorAgent;
    private readonly List<AIAgent> _allAgents;
    private readonly IChatClient _chatClient;
    private readonly List<string> _validAgentNames;
    private readonly ILogger<IntelligentGroupChatManager>? _logger;
    private readonly string? _managerCustomPrompt;
    private int _lastAgentIndex = 0;
    private string? _lastOrchestratorRawText;
    private static readonly Regex MentionPattern = new(@"@([^\s@、，。！？,\n]+)", RegexOptions.Compiled);

    public IntelligentGroupChatManager(
        AIAgent orchestratorAgent,
        IReadOnlyList<AIAgent> allAgents,
        IChatClient chatClient,
        int maximumIterationCount = 10,
        string? managerCustomPrompt = null,
        ILogger<IntelligentGroupChatManager>? logger = null)
    {
        _orchestratorAgent = orchestratorAgent;
        _allAgents = allAgents.ToList();
        _chatClient = chatClient;
        _logger = logger;
        _managerCustomPrompt = managerCustomPrompt;
        _validAgentNames = allAgents
            .Where(a => !string.IsNullOrEmpty(a.Name))
            .Select(a => a.Name!)
            .ToList();
        MaximumIterationCount = maximumIterationCount;
        
        _logger?.LogInformation("[IntelligentGroupChatManager] 可用Agent: {Agents}, 有协调者提示词: {HasPrompt}", 
            string.Join(", ", _validAgentNames), !string.IsNullOrEmpty(_managerCustomPrompt));
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

        if (string.IsNullOrEmpty(_managerCustomPrompt))
        {
            _logger?.LogInformation("[IntelligentGroupChatManager] 无协调者提示词，使用轮询方式");
            return GetNextAgentByRoundRobin();
        }

        if (history.Count > 0)
        {
            var lastMessage = history[history.Count - 1];
            var lastMessageText = lastMessage.Text ?? "";
            
            _logger?.LogInformation("[IntelligentGroupChatManager] 最后发言者: {Author}, 协调者: {Orchestrator}", 
                lastMessage.AuthorName ?? "null", _orchestratorAgent.Name);
            
            var isOrchestratorMessage = !string.IsNullOrEmpty(lastMessage.AuthorName) && 
                lastMessage.AuthorName == _orchestratorAgent.Name;
            
            if (isOrchestratorMessage)
            {
                var rawText = _lastOrchestratorRawText ?? lastMessageText;
                var mentionedAgent = ExtractMentionedAgent(rawText);
                if (mentionedAgent != null)
                {
                    _logger?.LogInformation("[IntelligentGroupChatManager] 协调者@指定Worker: {Agent}", mentionedAgent.Name);
                    return mentionedAgent;
                }
                
                _logger?.LogInformation("[IntelligentGroupChatManager] 协调者未@指定，使用AI选择Worker");
                return await SelectAgentByAIAsync(history, cancellationToken);
            }
            else
            {
                _logger?.LogInformation("[IntelligentGroupChatManager] Worker发言完毕，回到协调者进行总结并选择下一个人");
                return _orchestratorAgent;
            }
        }

        return await SelectAgentByAIAsync(history, cancellationToken);
    }

    private AIAgent GetNextAgentByRoundRobin()
    {
        var agent = _allAgents[_lastAgentIndex % _allAgents.Count];
        _lastAgentIndex++;
        return agent;
    }

    private AIAgent? ExtractMentionedAgent(string text)
    {
        var matches = MentionPattern.Matches(text);
        
        AIAgent? lastMentionedAgent = null;
        
        foreach (Match match in matches)
        {
            var mentionedName = match.Groups[1].Value.Trim();
            var agent = _allAgents.FirstOrDefault(a => 
                !string.IsNullOrEmpty(a.Name) && 
                a.Name.Equals(mentionedName, StringComparison.OrdinalIgnoreCase));
            
            if (agent != null)
            {
                lastMentionedAgent = agent;
            }
        }
        
        return lastMentionedAgent;
    }

    private async ValueTask<AIAgent?> SelectAgentByAIAsync(IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken)
    {
        var workerAgents = _allAgents.Where(a => a.Name != _orchestratorAgent.Name).ToList();
        var agentDescriptions = string.Join("\n", workerAgents
            .Where(a => !string.IsNullOrEmpty(a.Name))
            .Select(a => $"- {a.Name}: {a.Description ?? "团队成员"}"));

        var validNamesStr = string.Join("、", workerAgents.Where(a => !string.IsNullOrEmpty(a.Name)).Select(a => a.Name!));

        var historyText = string.Join("\n", history.TakeLast(5).Select(m => 
        {
            var text = m.Text ?? "";
            var preview = text.Length > 200 ? text.Substring(0, 200) + "..." : text;
            return $"{m.AuthorName ?? m.Role.ToString()}: {preview}";
        }));

        var prompt = $@"当前对话历史：
{historyText}

团队成员列表：
{agentDescriptions}

请根据以上信息，从团队成员【{validNamesStr}】中选择最适合下一个发言的人。
只返回名字，不要其他内容：";

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, "你是一个团队协调助手，根据对话历史选择下一个最适合发言的团队成员。只返回名字，不要其他内容。"),
                new(ChatRole.User, prompt)
            };
            
            var chatOptions = new ChatOptions
            {
                Temperature = 0.1f
            };
            
            var response = await _chatClient.GetResponseAsync(messages, chatOptions, cancellationToken);
            var selectedName = response.Messages.LastOrDefault()?.Text?.Trim() ?? "";
            _logger?.LogInformation("[IntelligentGroupChatManager] AI选择: {SelectedName}", selectedName);
            
            var selectedAgent = workerAgents.FirstOrDefault(a => 
                !string.IsNullOrEmpty(a.Name) && 
                a.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase));

            if (selectedAgent == null)
            {
                foreach (var agent in workerAgents)
                {
                    if (!string.IsNullOrEmpty(agent.Name) && selectedName.Contains(agent.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedAgent = agent;
                        break;
                    }
                }
            }

            if (selectedAgent == null)
            {
                _logger?.LogInformation("[IntelligentGroupChatManager] AI返回的名字'{SelectedName}'不在团队中，使用轮询方式", selectedName);
                selectedAgent = GetNextWorkerByRoundRobin();
            }

            _logger?.LogInformation("[IntelligentGroupChatManager] 最终选择: {AgentName}", selectedAgent?.Name);
            return selectedAgent ?? _orchestratorAgent;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "[IntelligentGroupChatManager] 选择失败，使用轮询方式");
            return GetNextWorkerByRoundRobin();
        }
    }

    private AIAgent GetNextWorkerByRoundRobin()
    {
        var workers = _allAgents.Where(a => a.Name != _orchestratorAgent.Name).ToList();
        if (workers.Count == 0) return _orchestratorAgent;
        var agent = workers[_lastAgentIndex % workers.Count];
        _lastAgentIndex++;
        return agent;
    }

    protected override ValueTask<IEnumerable<ChatMessage>?> UpdateHistoryAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (history.Count > 0)
        {
            var lastMsg = history[history.Count - 1];
            var isOrchestratorMessage = !string.IsNullOrEmpty(lastMsg.AuthorName) &&
                lastMsg.AuthorName == _orchestratorAgent.Name;

            if (isOrchestratorMessage)
            {
                _lastOrchestratorRawText = lastMsg.Text;
            }
        }

        var updatedHistory = new List<ChatMessage>();

        foreach (var msg in history)
        {
            var isOrchestratorMessage = !string.IsNullOrEmpty(msg.AuthorName) &&
                msg.AuthorName == _orchestratorAgent.Name;

            if (isOrchestratorMessage)
            {
                var annotated = AnnotateOrchestratorMessage(msg);
                updatedHistory.Add(annotated);
            }
            else
            {
                updatedHistory.Add(msg);
            }
        }

        return new ValueTask<IEnumerable<ChatMessage>?>(updatedHistory);
    }

    private ChatMessage AnnotateOrchestratorMessage(ChatMessage msg)
    {
        var text = msg.Text ?? "";
        if (string.IsNullOrEmpty(text)) return msg;

        var annotatedText = $"[以上是协调者的发言，你是专家成员，请根据协调者指示发表专业观点，不要模仿协调者的格式和角色]\n{text}";

        return new ChatMessage(msg.Role, annotatedText)
        {
            AuthorName = msg.AuthorName
        };
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
            
            var isOrchestratorMessage = !string.IsNullOrEmpty(lastMessage.AuthorName) && 
                lastMessage.AuthorName == _orchestratorAgent.Name;
            
            if (isOrchestratorMessage)
            {
                var terminateKeywords = new[] { "TERMINATE", "任务完成", "会议结束", "讨论结束", "TASK_COMPLETE", "最终结论", "总结完毕" };
                if (terminateKeywords.Any(k => content.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger?.LogInformation("[IntelligentGroupChatManager] 协调者发出结束信号: {Content}", content.Substring(0, Math.Min(100, content.Length)));
                    return new ValueTask<bool>(true);
                }
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
