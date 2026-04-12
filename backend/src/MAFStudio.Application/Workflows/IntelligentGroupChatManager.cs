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
                var mentionedAgent = ExtractMentionedAgent(lastMessageText);
                if (mentionedAgent != null)
                {
                    _logger?.LogInformation("[IntelligentGroupChatManager] 协调者@指定: {Agent}", mentionedAgent.Name);
                    return mentionedAgent;
                }
                
                _logger?.LogInformation("[IntelligentGroupChatManager] 协调者未@指定，使用AI选择");
                return await SelectAgentByAIAsync(history, cancellationToken);
            }
            else
            {
                _logger?.LogInformation("[IntelligentGroupChatManager] 非协调者发言，检查是否有@提及");
                var mentionedAgent = ExtractMentionedAgent(lastMessageText);
                if (mentionedAgent != null)
                {
                    _logger?.LogInformation("[IntelligentGroupChatManager] 发现@指定: {Agent}", mentionedAgent.Name);
                    return mentionedAgent;
                }
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
        
        foreach (Match match in matches)
        {
            var mentionedName = match.Groups[1].Value.Trim();
            var agent = _allAgents.FirstOrDefault(a => 
                !string.IsNullOrEmpty(a.Name) && 
                a.Name.Equals(mentionedName, StringComparison.OrdinalIgnoreCase));
            
            if (agent != null)
            {
                return agent;
            }
        }
        
        return null;
    }

    private async ValueTask<AIAgent?> SelectAgentByAIAsync(IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken)
    {
        var agentDescriptions = string.Join("\n", _allAgents
            .Where(a => !string.IsNullOrEmpty(a.Name))
            .Select(a => $"- {a.Name}: {a.Description ?? "团队成员"}"));

        var validNamesStr = string.Join("、", _validAgentNames);

        var historyText = string.Join("\n", history.TakeLast(3).Select(m => 
        {
            var text = m.Text ?? "";
            var preview = text.Length > 150 ? text.Substring(0, 150) + "..." : text;
            return $"{m.AuthorName ?? m.Role.ToString()}: {preview}";
        }));

        var prompt = $@"{_managerCustomPrompt}

---

当前对话历史：
{historyText}

团队成员列表：
{agentDescriptions}

请根据以上信息，从团队成员【{validNamesStr}】中选择最适合下一个发言的人。
只返回名字，不要其他内容：";

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, prompt)
            };
            
            var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
            var selectedName = response.Messages.LastOrDefault()?.Text?.Trim() ?? "";
            _logger?.LogInformation("[IntelligentGroupChatManager] AI选择: {SelectedName}", selectedName);
            
            var selectedAgent = _allAgents.FirstOrDefault(a => 
                !string.IsNullOrEmpty(a.Name) && 
                a.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase));

            if (selectedAgent == null)
            {
                _logger?.LogInformation("[IntelligentGroupChatManager] AI返回的名字'{SelectedName}'不在团队中，使用轮询方式", selectedName);
                selectedAgent = GetNextAgentByRoundRobin();
            }

            _logger?.LogInformation("[IntelligentGroupChatManager] 最终选择: {AgentName}", selectedAgent?.Name);
            return selectedAgent ?? _orchestratorAgent;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "[IntelligentGroupChatManager] 选择失败，使用轮询方式");
            return GetNextAgentByRoundRobin();
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
            
            var terminateKeywords = new[] { "任务完成", "会议结束", "讨论结束", "TASK_COMPLETE", "最终结论", "总结完毕", "TERMINATE", "结束" };
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
