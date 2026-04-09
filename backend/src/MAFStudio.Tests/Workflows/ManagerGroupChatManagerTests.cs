using MAFStudio.Application.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MAFStudio.Tests.Workflows;

public class ManagerGroupChatManagerTests
{
    private readonly string _logFile = Path.Combine(Path.GetTempPath(), "test_mention_log.txt");

    private void Log(string message)
    {
        Console.WriteLine(message);
        File.AppendAllText(_logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
    }

    private ChatClientAgent CreateMockAgent(string name, string description = "测试Agent")
    {
        var mockChatClient = new Mock<IChatClient>();
        mockChatClient.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, $"我是{name}")));
        
        var agent = new ChatClientAgent(
            mockChatClient.Object,
            $"你是{name}",
            name,
            description);
        return agent;
    }

    private TestableManagerGroupChatManager CreateTestableManager(
        ChatClientAgent managerAgent,
        List<ChatClientAgent> workerAgents,
        ILogger<ManagerGroupChatManager>? logger = null)
    {
        var allAgents = new List<AIAgent> { managerAgent };
        allAgents.AddRange(workerAgents);
        
        return new TestableManagerGroupChatManager(
            managerAgent,
            allAgents,
            10,
            logger);
    }

    [Fact]
    public async Task SelectNextAgentAsync_WithMention_ShouldSelectMentionedAgent()
    {
        File.Delete(_logFile);
        Log("========== 测试 @提及功能 ==========");

        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var productManagerAgent = CreateMockAgent("产品经理", "产品经理");
        var testEngineerAgent = CreateMockAgent("测试工程师", "测试工程师");
        var architectAgent = CreateMockAgent("小明-架构师", "架构师");
        var projectManagerAgent = CreateMockAgent("志龙-项目经理", "项目经理");

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ManagerGroupChatManager>();

        var manager = CreateTestableManager(
            managerAgent,
            new List<ChatClientAgent> { productManagerAgent, testEngineerAgent, architectAgent, projectManagerAgent },
            logger);

        Log($"Manager: 光哥-协调者");
        Log($"Workers: 产品经理, 测试工程师, 小明-架构师, 志龙-项目经理");

        var history = new List<ChatMessage>();

        var firstAgent = await manager.TestSelectNextAgentAsync(history);
        Log($"第一轮选择: {firstAgent?.Name}");
        Assert.Equal("光哥-协调者", firstAgent?.Name);

        manager.IncrementIteration();
        history.Add(new ChatMessage(ChatRole.Assistant, "大家好，我是协调者光哥。请@小明-架构师 分享一下技术方案。")
        {
            AuthorName = "光哥-协调者"
        });

        Log($"添加消息: 【光哥-协调者】大家好，我是协调者光哥。请@小明-架构师 分享一下技术方案。");

        var secondAgent = await manager.TestSelectNextAgentAsync(history);
        Log($"第二轮选择（@小明-架构师）: {secondAgent?.Name}");
        Assert.Equal("小明-架构师", secondAgent?.Name);

        manager.IncrementIteration();
        history.Add(new ChatMessage(ChatRole.Assistant, "我是架构师小明，我来分享技术方案...")
        {
            AuthorName = "小明-架构师"
        });

        var thirdAgent = await manager.TestSelectNextAgentAsync(history);
        Log($"第三轮选择（Worker发言后）: {thirdAgent?.Name}");
        Assert.Equal("光哥-协调者", thirdAgent?.Name);

        manager.IncrementIteration();
        history.Add(new ChatMessage(ChatRole.Assistant, "感谢@小明-架构师的分享。接下来请@测试工程师 准备测试用例。")
        {
            AuthorName = "光哥-协调者"
        });

        Log($"添加消息: 【光哥-协调者】感谢@小明-架构师的分享。接下来请@测试工程师 准备测试用例。");

        var fourthAgent = await manager.TestSelectNextAgentAsync(history);
        Log($"第四轮选择（@测试工程师）: {fourthAgent?.Name}");
        Assert.Equal("测试工程师", fourthAgent?.Name);

        Log("========== 测试通过 ==========");
    }

    [Fact]
    public async Task SelectNextAgentAsync_WithPartialMention_ShouldSelectMentionedAgent()
    {
        File.Delete(_logFile);
        Log("========== 测试 部分@提及功能 ==========");

        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var productManagerAgent = CreateMockAgent("产品经理", "产品经理");
        var testEngineerAgent = CreateMockAgent("测试工程师", "测试工程师");
        var architectAgent = CreateMockAgent("小明-架构师", "架构师");

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ManagerGroupChatManager>();

        var manager = CreateTestableManager(
            managerAgent,
            new List<ChatClientAgent> { productManagerAgent, testEngineerAgent, architectAgent },
            logger);

        var history = new List<ChatMessage>();

        await manager.TestSelectNextAgentAsync(history);
        manager.IncrementIteration();

        history.Add(new ChatMessage(ChatRole.Assistant, "请@小明 分享一下技术方案。")
        {
            AuthorName = "光哥-协调者"
        });

        Log($"添加消息: 【光哥-协调者】请@小明 分享一下技术方案。");

        var secondAgent = await manager.TestSelectNextAgentAsync(history);
        Log($"第二轮选择（@小明）: {secondAgent?.Name}");
        Assert.Equal("小明-架构师", secondAgent?.Name);

        manager.IncrementIteration();
        history.Add(new ChatMessage(ChatRole.Assistant, "我是架构师小明...")
        {
            AuthorName = "小明-架构师"
        });

        await manager.TestSelectNextAgentAsync(history);
        manager.IncrementIteration();

        history.Add(new ChatMessage(ChatRole.Assistant, "请@架构师 再补充一下。")
        {
            AuthorName = "光哥-协调者"
        });

        Log($"添加消息: 【光哥-协调者】请@架构师 再补充一下。");

        var fourthAgent = await manager.TestSelectNextAgentAsync(history);
        Log($"第四轮选择（@架构师）: {fourthAgent?.Name}");
        Assert.Equal("小明-架构师", fourthAgent?.Name);

        Log("========== 测试通过 ==========");
    }

    [Fact]
    public async Task SelectNextAgentAsync_WithoutMention_ShouldSelectByRoundRobin()
    {
        File.Delete(_logFile);
        Log("========== 测试 无@提及时的轮询功能 ==========");

        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var productManagerAgent = CreateMockAgent("产品经理", "产品经理");
        var testEngineerAgent = CreateMockAgent("测试工程师", "测试工程师");

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ManagerGroupChatManager>();

        var manager = CreateTestableManager(
            managerAgent,
            new List<ChatClientAgent> { productManagerAgent, testEngineerAgent },
            logger);

        var history = new List<ChatMessage>();

        await manager.TestSelectNextAgentAsync(history);
        manager.IncrementIteration();

        history.Add(new ChatMessage(ChatRole.Assistant, "大家好，请轮流发言。")
        {
            AuthorName = "光哥-协调者"
        });

        Log($"添加消息（无@提及）: 【光哥-协调者】大家好，请轮流发言。");

        var secondAgent = await manager.TestSelectNextAgentAsync(history);
        Log($"第二轮选择（轮询）: {secondAgent?.Name}");
        Assert.Equal("产品经理", secondAgent?.Name);

        manager.IncrementIteration();
        history.Add(new ChatMessage(ChatRole.Assistant, "我是产品经理...")
        {
            AuthorName = "产品经理"
        });

        await manager.TestSelectNextAgentAsync(history);
        manager.IncrementIteration();

        history.Add(new ChatMessage(ChatRole.Assistant, "继续。")
        {
            AuthorName = "光哥-协调者"
        });

        var fourthAgent = await manager.TestSelectNextAgentAsync(history);
        Log($"第四轮选择（轮询）: {fourthAgent?.Name}");
        Assert.Equal("测试工程师", fourthAgent?.Name);

        Log("========== 测试通过 ==========");
    }

    [Fact]
    public async Task SelectNextAgentAsync_WithTestEngineerMention_ShouldSelectTestEngineer()
    {
        File.Delete(_logFile);
        Log("========== 测试 @测试工程师 提及功能 ==========");

        var managerAgent = CreateMockAgent("光哥-协调者", "协调者");
        var productManagerAgent = CreateMockAgent("产品经理", "产品经理");
        var testEngineerAgent = CreateMockAgent("测试工程师", "测试工程师");
        var architectAgent = CreateMockAgent("小明-架构师", "架构师");

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ManagerGroupChatManager>();

        var manager = CreateTestableManager(
            managerAgent,
            new List<ChatClientAgent> { productManagerAgent, testEngineerAgent, architectAgent },
            logger);

        var history = new List<ChatMessage>();

        await manager.TestSelectNextAgentAsync(history);
        manager.IncrementIteration();

        history.Add(new ChatMessage(ChatRole.Assistant, "请@测试工程师 分享测试计划。")
        {
            AuthorName = "光哥-协调者"
        });

        Log($"添加消息: 【光哥-协调者】请@测试工程师 分享测试计划。");

        var secondAgent = await manager.TestSelectNextAgentAsync(history);
        Log($"第二轮选择（@测试工程师）: {secondAgent?.Name}");
        
        Assert.Equal("测试工程师", secondAgent?.Name);

        Log("========== 测试通过 ==========");
    }
}

public class TestableManagerGroupChatManager : ManagerGroupChatManager
{
    public TestableManagerGroupChatManager(
        AIAgent managerAgent,
        IReadOnlyList<AIAgent> allAgents,
        int maximumIterationCount = 10,
        ILogger<ManagerGroupChatManager>? logger = null)
        : base(managerAgent, allAgents, maximumIterationCount, logger)
    {
    }

    public async Task<AIAgent?> TestSelectNextAgentAsync(IReadOnlyList<ChatMessage> history)
    {
        return await SelectNextAgentAsync(history);
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
