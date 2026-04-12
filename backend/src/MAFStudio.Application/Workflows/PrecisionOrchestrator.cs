using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Workflows;

public class PrecisionOrchestrator : GroupChatManager
{
    private readonly AIAgent _managerAgent;
    private readonly Dictionary<string, AIAgent> _workerMap;
    private readonly List<string> _workerNames;
    private readonly IChatClient _managerChatClient;
    private readonly ILogger<PrecisionOrchestrator>? _logger;

    public event EventHandler<ManagerThinkingEventArgs>? ManagerThinking;

    public IReadOnlyList<string> WorkerNames => _workerNames.AsReadOnly();

    public PrecisionOrchestrator(
        AIAgent managerAgent,
        IEnumerable<AIAgent> workers,
        IChatClient managerChatClient,
        int maximumIterationCount = 10,
        ILogger<PrecisionOrchestrator>? logger = null)
    {
        _managerAgent = managerAgent;
        _managerChatClient = managerChatClient;

        var workerList = workers.Where(a => a.Name != managerAgent.Name).ToList();
        _workerMap = workerList.ToDictionary(a => a.Name!, a => a, StringComparer.OrdinalIgnoreCase);
        _workerNames = workerList.Select(a => a.Name!).ToList();
        _logger = logger;

        MaximumIterationCount = maximumIterationCount;

        _logger?.LogInformation("[PrecisionOrchestrator] 初始化。Manager: {Manager}, Workers: {Workers}",
            _managerAgent.Name, string.Join(", ", _workerNames));
    }

    protected override async ValueTask<AIAgent?> SelectNextAgentAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("[PrecisionOrchestrator] SelectNextAgentAsync Iteration={Iteration}, HistoryCount={Count}",
            IterationCount, history.Count);

        if (IterationCount == 0)
        {
            return await HandleFirstRoundAsync(history, cancellationToken);
        }

        return await HandleSubsequentRoundAsync(history, cancellationToken);
    }

    private ValueTask<AIAgent?> HandleFirstRoundAsync(IReadOnlyList<ChatMessage> history, CancellationToken ct)
    {
        var firstWorkerName = _workerNames.FirstOrDefault();
        if (firstWorkerName == null)
        {
            var thinking = "【任务启动】没有可用的团队成员。";
            OnManagerThinking(thinking, null);
            return new ValueTask<AIAgent?>((AIAgent?)null);
        }

        var thinkingText = $"【任务启动】我是{_managerAgent.Name}，现在开始协调团队工作。\n\n" +
                           $"团队成员：{string.Join("、", _workerNames)}\n\n" +
                           $"首先请 @{firstWorkerName} 发言，分享你的观点。";

        OnManagerThinking(thinkingText, firstWorkerName);
        _logger?.LogInformation("[PrecisionOrchestrator] 第1轮: 协调者先发言点名 {Worker}", firstWorkerName);

        return new ValueTask<AIAgent?>(_managerAgent);
    }

    private async ValueTask<AIAgent?> HandleSubsequentRoundAsync(IReadOnlyList<ChatMessage> history, CancellationToken ct)
    {
        var lastMessage = history.LastOrDefault();
        if (lastMessage == null)
        {
            _logger?.LogWarning("[PrecisionOrchestrator] 无历史消息");
            return _workerMap.Values.FirstOrDefault();
        }

        var lastSender = lastMessage.AuthorName ?? "未知";
        var lastText = lastMessage.Text ?? "";

        _logger?.LogInformation("[PrecisionOrchestrator] 最后发言: [{Sender}] {Text}",
            lastSender, lastText.Substring(0, Math.Min(200, lastText.Length)));

        var lastSpeaker = FindWorkerName(lastSender);
        var workersWhoSpoke = GetWorkersWhoSpoke(history);

        _logger?.LogInformation("[PrecisionOrchestrator] 上一个发言者: {LastSpeaker}, 已发言的Worker: [{Workers}]",
            lastSpeaker, string.Join(", ", workersWhoSpoke));

        if (lastSender.Equals(_managerAgent.Name, StringComparison.OrdinalIgnoreCase))
        {
            var namedWorker = ParseNamedAgent(lastText);
            if (namedWorker != null && _workerMap.ContainsKey(namedWorker))
            {
                _logger?.LogInformation("[PrecisionOrchestrator] 协调者点名了 {Worker}，让他发言", namedWorker);
                var thinking = $"【协调决策】根据协调者指示，请 @{namedWorker} 发言。";
                OnManagerThinking(thinking, namedWorker);
                return _workerMap[namedWorker];
            }
            else
            {
                var fallbackName = SelectNextWorker(null, workersWhoSpoke);
                _logger?.LogInformation("[PrecisionOrchestrator] 协调者没有点名任何人，选择默认Worker: {Agent}", fallbackName);
                var thinking = $"【协调决策】接下来请 @{fallbackName} 继续发言。";
                OnManagerThinking(thinking, fallbackName);
                return _workerMap[fallbackName];
            }
        }
        else
        {
            _logger?.LogInformation("[PrecisionOrchestrator] 上一个发言者是Worker: {LastSpeaker}，让协调者总结并选择下一个发言者", lastSpeaker);
            return _managerAgent;
        }
    }

    private string SelectNextWorker(string? lastSpeaker, HashSet<string> workersWhoSpoke)
    {
        var notYetSpoke = _workerNames.Where(n => !workersWhoSpoke.Contains(n)).ToList();

        if (notYetSpoke.Count > 0)
        {
            var selected = notYetSpoke[0];
            _logger?.LogInformation("[PrecisionOrchestrator] 优先选择未发言的Worker: {Agent}", selected);
            return selected;
        }

        if (lastSpeaker != null && _workerNames.Count > 1)
        {
            var others = _workerNames.Where(n => n != lastSpeaker).ToList();
            var selected = others[IterationCount % others.Count];
            _logger?.LogInformation("[PrecisionOrchestrator] 避免重复点名 {LastSpeaker}，选择: {Agent}", lastSpeaker, selected);
            return selected;
        }

        var fallback = _workerNames[IterationCount % _workerNames.Count];
        return fallback;
    }

    private string? FindWorkerName(string sender)
    {
        return _workerNames.FirstOrDefault(n =>
            n.Equals(sender, StringComparison.OrdinalIgnoreCase));
    }

    private HashSet<string> GetWorkersWhoSpoke(IReadOnlyList<ChatMessage> history)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var msg in history)
        {
            var sender = msg.AuthorName;
            if (sender != null && FindWorkerName(sender) != null)
            {
                result.Add(sender);
            }
        }
        return result;
    }

    public async Task<string> CallManagerLLMAsync(IReadOnlyList<ChatMessage> history, string? lastSpeaker, HashSet<string> workersWhoSpoke, CancellationToken ct)
    {
        var historySummary = BuildHistorySummary(history);
        var notYetSpoke = _workerNames.Where(n => !workersWhoSpoke.Contains(n)).ToList();

        var systemPrompt = $@"你是协调者{_managerAgent.Name}，负责主持团队讨论。

团队成员：{string.Join("、", _workerNames)}

你的唯一任务是：根据讨论进展，决定下一个应该发言的团队成员。

规则：
1. 你必须用 @成员名字 的方式点名下一个发言的人
2. 你不要发表任何观点，只负责协调
3. 回复格式：接下来请 @成员名字 继续发言。
4. 不要点名刚刚发言的人（{lastSpeaker}），要让其他人也有发言机会
5. 优先点名还没发言过的成员：{string.Join("、", notYetSpoke)}";

        var userPrompt = $@"以下是团队讨论的历史记录：
{historySummary}

刚刚发言的是：{lastSpeaker ?? "未知"}
已经发言过的成员：{string.Join("、", workersWhoSpoke)}
还没发言的成员：{string.Join("、", notYetSpoke)}

请决定下一个应该发言的团队成员，用 @成员名字 的方式点名。注意不要重复点名刚刚发言的人。";

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        };

        var response = await _managerChatClient.GetResponseAsync(messages, cancellationToken: ct);
        return response.Text ?? "";
    }

    public string? ParseNamedAgent(string managerResponse)
    {
        if (string.IsNullOrEmpty(managerResponse))
            return null;

        var match = Regex.Match(managerResponse, @"@(\S+)");
        if (!match.Success)
            return null;

        var targetName = match.Groups[1].Value.Trim();

        if (_workerMap.ContainsKey(targetName))
            return targetName;

        var fuzzyMatch = _workerNames.FirstOrDefault(n =>
            n.Contains(targetName, StringComparison.OrdinalIgnoreCase) ||
            targetName.Contains(n, StringComparison.OrdinalIgnoreCase));

        return fuzzyMatch;
    }

    public string BuildHistorySummary(IReadOnlyList<ChatMessage> history)
    {
        var sb = new System.Text.StringBuilder();
        var recentMessages = history.Skip(Math.Max(0, history.Count - 3)).ToList();

        foreach (var msg in recentMessages)
        {
            var sender = msg.AuthorName ?? "未知";
            var text = msg.Text ?? "";
            if (string.IsNullOrEmpty(text)) continue;
            var truncatedText = text.Length > 100 ? text.Substring(0, 100) + "..." : text;
            sb.AppendLine($"[{sender}]: {truncatedText}");
            sb.AppendLine();
        }

        var result = sb.ToString();
        if (result.Length > 1500)
        {
            result = result.Substring(0, 1500) + "...";
        }

        return result;
    }

    protected override ValueTask<bool> ShouldTerminateAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (IterationCount >= MaximumIterationCount)
        {
            _logger?.LogInformation("[PrecisionOrchestrator] 达到最大迭代次数: {IterationCount}", IterationCount);
            return new ValueTask<bool>(true);
        }

        if (history.Count > 0)
        {
            var lastMessage = history[history.Count - 1];
            var content = lastMessage.Text ?? string.Empty;

            var terminateKeywords = new[] { "任务完成", "会议结束", "讨论结束", "TASK_COMPLETE" };
            if (terminateKeywords.Any(k => content.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                _logger?.LogInformation("[PrecisionOrchestrator] 检测到结束关键词");
                return new ValueTask<bool>(true);
            }
        }

        return new ValueTask<bool>(false);
    }

    protected virtual void OnManagerThinking(string thinking, string? selectedAgent)
    {
        ManagerThinking?.Invoke(this, new ManagerThinkingEventArgs(_managerAgent.Name!, thinking, selectedAgent, IterationCount));
    }
}
