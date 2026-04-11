using MAFStudio.Application.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MAFStudio.Tests.Workflows;

public class PrecisionOrchestratorTests
{
    private ChatClientAgent CreateMockAgent(string name, string description = "测试Agent")
    {
        var mockChatClient = new Mock<IChatClient>();
        mockChatClient
            .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, $"我是{name}")));

        return new ChatClientAgent(mockChatClient.Object, $"你是{name}", name, description);
    }

    private Mock<IChatClient> CreateManagerChatClientMock(string llmResponse)
    {
        var mock = new Mock<IChatClient>();
        mock
            .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, llmResponse)));
        return mock;
    }

    private PrecisionOrchestrator CreateOrchestrator(
        ChatClientAgent managerAgent,
        List<ChatClientAgent> workers,
        Mock<IChatClient>? chatClientMock = null,
        int maxIterations = 10)
    {
        var allAgents = new List<AIAgent> { managerAgent };
        allAgents.AddRange(workers);

        var chatClient = chatClientMock?.Object ?? CreateManagerChatClientMock("接下来请 @产品经理 继续发言。").Object;

        return new PrecisionOrchestrator(
            managerAgent,
            allAgents,
            chatClient,
            maxIterations);
    }

    private TestablePrecisionOrchestrator CreateTestableOrchestrator(
        ChatClientAgent managerAgent,
        List<ChatClientAgent> workers,
        Mock<IChatClient>? chatClientMock = null,
        int maxIterations = 10)
    {
        var allAgents = new List<AIAgent> { managerAgent };
        allAgents.AddRange(workers);

        var chatClient = chatClientMock?.Object ?? CreateManagerChatClientMock("接下来请 @产品经理 继续发言。").Object;

        return new TestablePrecisionOrchestrator(
            managerAgent,
            allAgents,
            chatClient,
            maxIterations);
    }

    [Fact]
    public void Constructor_ManagerNotInWorkerList()
    {
        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var worker1 = CreateMockAgent("产品经理", "产品经理");
        var worker2 = CreateMockAgent("测试工程师", "测试工程师");

        var orchestrator = CreateOrchestrator(managerAgent, new List<ChatClientAgent> { worker1, worker2 });

        Assert.Equal(2, orchestrator.WorkerNames.Count);
        Assert.DoesNotContain("光哥-协调者", orchestrator.WorkerNames);
        Assert.Contains("产品经理", orchestrator.WorkerNames);
        Assert.Contains("测试工程师", orchestrator.WorkerNames);
    }

    [Fact]
    public async Task FirstRound_SelectsFirstWorker_AndFiresManagerThinking()
    {
        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var worker1 = CreateMockAgent("产品经理", "产品经理");
        var worker2 = CreateMockAgent("测试工程师", "测试工程师");

        var orchestrator = CreateTestableOrchestrator(managerAgent, new List<ChatClientAgent> { worker1, worker2 });

        ManagerThinkingEventArgs? capturedArgs = null;
        orchestrator.ManagerThinking += (_, args) => capturedArgs = args;

        var history = new List<ChatMessage>();
        var selected = await orchestrator.TestSelectNextAgentAsync(history);

        Assert.Equal("产品经理", selected?.Name);
        Assert.NotNull(capturedArgs);
        Assert.Equal("光哥-协调者", capturedArgs.ManagerName);
        Assert.Contains("@产品经理", capturedArgs.Thinking);
        Assert.Equal("产品经理", capturedArgs.SelectedAgent);
        Assert.Equal(0, capturedArgs.IterationCount);
    }

    [Fact]
    public async Task SubsequentRound_LLMReturnsMention_SelectsNamedAgent()
    {
        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var worker1 = CreateMockAgent("产品经理", "产品经理");
        var worker2 = CreateMockAgent("测试工程师", "测试工程师");
        var worker3 = CreateMockAgent("小明-架构师", "架构师");

        var chatClientMock = CreateManagerChatClientMock("接下来请 @测试工程师 继续发言。");

        var orchestrator = CreateTestableOrchestrator(
            managerAgent,
            new List<ChatClientAgent> { worker1, worker2, worker3 },
            chatClientMock);

        ManagerThinkingEventArgs? capturedArgs = null;
        orchestrator.ManagerThinking += (_, args) => capturedArgs = args;

        var history = new List<ChatMessage>
        {
            new(ChatRole.User, "讨论一下项目方案"),
            new(ChatRole.Assistant, "我认为应该先做需求分析") { AuthorName = "产品经理" }
        };

        orchestrator.IncrementIteration();
        var selected = await orchestrator.TestSelectNextAgentAsync(history);

        Assert.Equal("测试工程师", selected?.Name);
        Assert.NotNull(capturedArgs);
        Assert.Contains("@测试工程师", capturedArgs.Thinking);
        Assert.Equal("测试工程师", capturedArgs.SelectedAgent);
    }

    [Fact]
    public async Task SubsequentRound_LLMReturnsNoMention_FallsBackToAvoidRepeat()
    {
        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var worker1 = CreateMockAgent("产品经理", "产品经理");
        var worker2 = CreateMockAgent("测试工程师", "测试工程师");

        var chatClientMock = CreateManagerChatClientMock("好的，继续讨论吧。");

        var orchestrator = CreateTestableOrchestrator(
            managerAgent,
            new List<ChatClientAgent> { worker1, worker2 },
            chatClientMock);

        ManagerThinkingEventArgs? capturedArgs = null;
        orchestrator.ManagerThinking += (_, args) => capturedArgs = args;

        var history = new List<ChatMessage>
        {
            new(ChatRole.User, "讨论一下项目方案"),
            new(ChatRole.Assistant, "我认为应该先做需求分析") { AuthorName = "产品经理" }
        };

        orchestrator.IncrementIteration();
        var selected = await orchestrator.TestSelectNextAgentAsync(history);

        Assert.NotNull(selected);
        Assert.NotNull(capturedArgs);
        Assert.NotEqual("产品经理", selected?.Name);
    }

    [Fact]
    public async Task SubsequentRound_LLMThrows_FallsBackToAvoidRepeat()
    {
        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var worker1 = CreateMockAgent("产品经理", "产品经理");
        var worker2 = CreateMockAgent("测试工程师", "测试工程师");

        var chatClientMock = new Mock<IChatClient>();
        chatClientMock
            .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("模型不可用"));

        var orchestrator = CreateTestableOrchestrator(
            managerAgent,
            new List<ChatClientAgent> { worker1, worker2 },
            chatClientMock);

        ManagerThinkingEventArgs? capturedArgs = null;
        orchestrator.ManagerThinking += (_, args) => capturedArgs = args;

        var history = new List<ChatMessage>
        {
            new(ChatRole.Assistant, "我认为应该先做需求分析") { AuthorName = "产品经理" }
        };

        orchestrator.IncrementIteration();
        var selected = await orchestrator.TestSelectNextAgentAsync(history);

        Assert.NotNull(selected);
        Assert.NotEqual("产品经理", selected?.Name);
        Assert.NotNull(capturedArgs);
    }

    [Fact]
    public async Task SubsequentRound_LLMRepeatsLastSpeaker_AutoSwitches()
    {
        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var worker1 = CreateMockAgent("产品经理", "产品经理");
        var worker2 = CreateMockAgent("测试工程师", "测试工程师");

        var chatClientMock = CreateManagerChatClientMock("接下来请 @产品经理 继续发言。");

        var orchestrator = CreateTestableOrchestrator(
            managerAgent,
            new List<ChatClientAgent> { worker1, worker2 },
            chatClientMock);

        ManagerThinkingEventArgs? capturedArgs = null;
        orchestrator.ManagerThinking += (_, args) => capturedArgs = args;

        var history = new List<ChatMessage>
        {
            new(ChatRole.Assistant, "我认为应该先做需求分析") { AuthorName = "产品经理" }
        };

        orchestrator.IncrementIteration();
        var selected = await orchestrator.TestSelectNextAgentAsync(history);

        Assert.NotNull(selected);
        Assert.NotEqual("产品经理", selected?.Name);
        Assert.Equal("测试工程师", selected?.Name);
    }

    [Fact]
    public async Task SubsequentRound_PrioritizesUnspokenWorkers()
    {
        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var worker1 = CreateMockAgent("产品经理", "产品经理");
        var worker2 = CreateMockAgent("测试工程师", "测试工程师");
        var worker3 = CreateMockAgent("小明-架构师", "架构师");

        var chatClientMock = new Mock<IChatClient>();
        chatClientMock
            .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("模型不可用"));

        var orchestrator = CreateTestableOrchestrator(
            managerAgent,
            new List<ChatClientAgent> { worker1, worker2, worker3 },
            chatClientMock);

        var history = new List<ChatMessage>
        {
            new(ChatRole.Assistant, "我认为应该先做需求分析") { AuthorName = "产品经理" }
        };

        orchestrator.IncrementIteration();
        var selected = await orchestrator.TestSelectNextAgentAsync(history);

        Assert.NotNull(selected);
        Assert.NotEqual("产品经理", selected?.Name);
        Assert.True(selected?.Name == "测试工程师" || selected?.Name == "小明-架构师");
    }

    [Fact]
    public async Task FullFlow_ThreeRounds_ManagerAlwaysNamesWorkers()
    {
        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var worker1 = CreateMockAgent("产品经理", "产品经理");
        var worker2 = CreateMockAgent("测试工程师", "测试工程师");
        var worker3 = CreateMockAgent("小明-架构师", "架构师");

        var llmResponses = new Queue<string>();
        llmResponses.Enqueue("接下来请 @测试工程师 继续发言。");
        llmResponses.Enqueue("接下来请 @小明-架构师 继续发言。");

        var chatClientMock = new Mock<IChatClient>();
        chatClientMock
            .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ChatResponse(new ChatMessage(ChatRole.Assistant, llmResponses.Dequeue())));

        var orchestrator = CreateTestableOrchestrator(
            managerAgent,
            new List<ChatClientAgent> { worker1, worker2, worker3 },
            chatClientMock);

        var thinkingEvents = new List<ManagerThinkingEventArgs>();
        orchestrator.ManagerThinking += (_, args) => thinkingEvents.Add(args);

        var history = new List<ChatMessage>();

        var round1 = await orchestrator.TestSelectNextAgentAsync(history);
        Assert.Equal("产品经理", round1?.Name);
        Assert.Contains("@产品经理", thinkingEvents[0].Thinking);
        Assert.Equal("产品经理", thinkingEvents[0].SelectedAgent);

        history.Add(new ChatMessage(ChatRole.Assistant, "我是产品经理，我认为...") { AuthorName = "产品经理" });
        orchestrator.IncrementIteration();

        var round2 = await orchestrator.TestSelectNextAgentAsync(history);
        Assert.Equal("测试工程师", round2?.Name);
        Assert.Contains("@测试工程师", thinkingEvents[1].Thinking);
        Assert.Equal("测试工程师", thinkingEvents[1].SelectedAgent);

        history.Add(new ChatMessage(ChatRole.Assistant, "我是测试工程师，我建议...") { AuthorName = "测试工程师" });
        orchestrator.IncrementIteration();

        var round3 = await orchestrator.TestSelectNextAgentAsync(history);
        Assert.Equal("小明-架构师", round3?.Name);
        Assert.Contains("@小明-架构师", thinkingEvents[2].Thinking);
        Assert.Equal("小明-架构师", thinkingEvents[2].SelectedAgent);
    }

    [Fact]
    public async Task FullFlow_NeverRepeatsLastSpeaker()
    {
        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var worker1 = CreateMockAgent("产品经理", "产品经理");
        var worker2 = CreateMockAgent("测试工程师", "测试工程师");

        var chatClientMock = CreateManagerChatClientMock("接下来请 @产品经理 继续发言。");

        var orchestrator = CreateTestableOrchestrator(
            managerAgent,
            new List<ChatClientAgent> { worker1, worker2 },
            chatClientMock);

        var thinkingEvents = new List<ManagerThinkingEventArgs>();
        orchestrator.ManagerThinking += (_, args) => thinkingEvents.Add(args);

        var history = new List<ChatMessage>
        {
            new(ChatRole.Assistant, "我是产品经理") { AuthorName = "产品经理" }
        };

        orchestrator.IncrementIteration();
        var selected = await orchestrator.TestSelectNextAgentAsync(history);

        Assert.NotEqual("产品经理", selected?.Name);
        Assert.Equal("测试工程师", selected?.Name);
    }

    [Fact]
    public void ParseNamedAgent_ExactMatch_ReturnsAgent()
    {
        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var worker1 = CreateMockAgent("产品经理", "产品经理");
        var worker2 = CreateMockAgent("小明-架构师", "架构师");

        var orchestrator = CreateOrchestrator(managerAgent, new List<ChatClientAgent> { worker1, worker2 });

        var result = orchestrator.ParseNamedAgent("接下来请 @产品经理 继续发言。");
        Assert.Equal("产品经理", result);
    }

    [Fact]
    public void ParseNamedAgent_FuzzyMatch_ReturnsAgent()
    {
        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var worker1 = CreateMockAgent("产品经理", "产品经理");
        var worker2 = CreateMockAgent("小明-架构师", "架构师");

        var orchestrator = CreateOrchestrator(managerAgent, new List<ChatClientAgent> { worker1, worker2 });

        var result = orchestrator.ParseNamedAgent("接下来请 @架构师 继续发言。");
        Assert.Equal("小明-架构师", result);
    }

    [Fact]
    public void ParseNamedAgent_NoMatch_ReturnsNull()
    {
        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var worker1 = CreateMockAgent("产品经理", "产品经理");

        var orchestrator = CreateOrchestrator(managerAgent, new List<ChatClientAgent> { worker1 });

        var result = orchestrator.ParseNamedAgent("好的，继续讨论吧。");
        Assert.Null(result);
    }

    [Fact]
    public void ParseNamedAgent_EmptyResponse_ReturnsNull()
    {
        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var worker1 = CreateMockAgent("产品经理", "产品经理");

        var orchestrator = CreateOrchestrator(managerAgent, new List<ChatClientAgent> { worker1 });

        Assert.Null(orchestrator.ParseNamedAgent(""));
        Assert.Null(orchestrator.ParseNamedAgent(null!));
    }

    [Fact]
    public async Task ShouldTerminate_ReachesMaxIterations_ReturnsTrue()
    {
        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var worker1 = CreateMockAgent("产品经理", "产品经理");

        var orchestrator = CreateTestableOrchestrator(managerAgent, new List<ChatClientAgent> { worker1 }, maxIterations: 2);

        var history = new List<ChatMessage>
        {
            new(ChatRole.Assistant, "讨论内容") { AuthorName = "产品经理" }
        };

        var result1 = await orchestrator.TestShouldTerminateAsync(history);
        Assert.False(result1);

        orchestrator.IncrementIteration();
        orchestrator.IncrementIteration();

        var result2 = await orchestrator.TestShouldTerminateAsync(history);
        Assert.True(result2);
    }

    [Fact]
    public async Task ShouldTerminate_DetectsEndKeyword_ReturnsTrue()
    {
        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var worker1 = CreateMockAgent("产品经理", "产品经理");

        var orchestrator = CreateTestableOrchestrator(managerAgent, new List<ChatClientAgent> { worker1 });

        var history = new List<ChatMessage>
        {
            new(ChatRole.Assistant, "讨论结束，感谢大家") { AuthorName = "产品经理" }
        };

        var result = await orchestrator.TestShouldTerminateAsync(history);
        Assert.True(result);
    }

    [Fact]
    public void BuildHistorySummary_TruncatesLongMessages()
    {
        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var worker1 = CreateMockAgent("产品经理", "产品经理");

        var orchestrator = CreateOrchestrator(managerAgent, new List<ChatClientAgent> { worker1 });

        var longText = new string('A', 500);
        var history = new List<ChatMessage>
        {
            new(ChatRole.Assistant, longText) { AuthorName = "产品经理" }
        };

        var summary = orchestrator.BuildHistorySummary(history);
        Assert.True(summary.Length < 500);
    }
}

public class TestablePrecisionOrchestrator : PrecisionOrchestrator
{
    public TestablePrecisionOrchestrator(
        AIAgent managerAgent,
        IEnumerable<AIAgent> workers,
        IChatClient managerChatClient,
        int maximumIterationCount = 10)
        : base(managerAgent, workers, managerChatClient, maximumIterationCount)
    {
    }

    public async Task<AIAgent?> TestSelectNextAgentAsync(IReadOnlyList<ChatMessage> history)
    {
        return await SelectNextAgentAsync(history);
    }

    public async Task<bool> TestShouldTerminateAsync(IReadOnlyList<ChatMessage> history)
    {
        return await ShouldTerminateAsync(history);
    }

    public void IncrementIteration()
    {
        var iterationCountProperty = typeof(GroupChatManager).GetProperty("IterationCount");
        if (iterationCountProperty != null)
        {
            var currentValue = (int)(iterationCountProperty.GetValue(this) ?? 0);
            iterationCountProperty.SetValue(this, currentValue + 1);
        }
    }
}
