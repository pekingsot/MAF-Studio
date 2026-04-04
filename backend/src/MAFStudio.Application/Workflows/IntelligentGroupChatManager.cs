using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace MAFStudio.Application.Workflows;

public class IntelligentGroupChatManager : GroupChatManager
{
    private readonly AIAgent _orchestratorAgent;
    private readonly List<AIAgent> _allAgents;
    private readonly IChatClient _chatClient;

    public IntelligentGroupChatManager(
        AIAgent orchestratorAgent,
        IReadOnlyList<AIAgent> allAgents,
        IChatClient chatClient,
        int maximumIterationCount = 10)
    {
        _orchestratorAgent = orchestratorAgent;
        _allAgents = allAgents.ToList();
        _chatClient = chatClient;
        MaximumIterationCount = maximumIterationCount;
    }

    protected override async ValueTask<AIAgent?> SelectNextAgentAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (IterationCount == 0)
        {
            return _orchestratorAgent;
        }

        var agentDescriptions = string.Join("\n", _allAgents.Select(a => 
            $"- {a.Name}: {a.Description ?? "No description"}"));

        var historyText = string.Join("\n", history.TakeLast(5).Select(m => 
            $"{m.Role}: {m.Text?.Substring(0, Math.Min(200, m.Text?.Length ?? 0))}"));

        var prompt = $@"你是一个协调者，负责选择下一个发言的智能体。

可用的智能体：
{agentDescriptions}

最近的对话历史：
{historyText}

请分析当前对话状态，选择最适合下一个发言的智能体。
只需返回智能体的名称，不要返回其他内容。

下一个发言者：";

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
            
            var selectedAgent = _allAgents.FirstOrDefault(a => 
                a.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase) ||
                selectedName.Contains(a.Name, StringComparison.OrdinalIgnoreCase));

            return selectedAgent ?? _orchestratorAgent;
        }
        catch
        {
            return _orchestratorAgent;
        }
    }

    protected override ValueTask<bool> ShouldTerminateAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (IterationCount >= MaximumIterationCount)
        {
            return new ValueTask<bool>(true);
        }

        if (history.Count > 0)
        {
            var lastMessage = history[history.Count - 1];
            var content = lastMessage.Text ?? string.Empty;
            
            var terminateKeywords = new[] { "任务完成", "会议结束", "讨论结束", "TASK_COMPLETE", "完成", "结束" };
            if (terminateKeywords.Any(k => content.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                return new ValueTask<bool>(true);
            }
        }

        return new ValueTask<bool>(false);
    }

    protected override void Reset()
    {
        base.Reset();
    }
}
