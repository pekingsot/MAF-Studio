using System.Reflection;
using MAFStudio.Application.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;

namespace MAFStudio.Tests.Workflows;

public class ManagerGroupChatManagerHistoryTests
{
    private readonly string _logFile = Path.Combine(Path.GetTempPath(), "test_history_debug_log.txt");
    
    private void Log(string message)
    {
        Console.WriteLine(message);
        File.AppendAllText(_logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
    }
    
    [Fact]
    public async Task DebugUpdateHistoryAsync()
    {
        File.Delete(_logFile);
        Log("========== 调试 UpdateHistoryAsync 方法 ==========");
        
        var managerAgent = CreateMockAgent("光哥-协调者", "你是协调者，负责分配任务。");
        var workerAgent1 = CreateMockAgent("产品经理", "你是产品经理。");
        var workerAgent2 = CreateMockAgent("测试工程师", "你是测试工程师。");
        var workerAgent3 = CreateMockAgent("志龙-项目经理", "你是项目经理。");
        var workerAgent4 = CreateMockAgent("小明-架构师", "你是架构师。");
        
        var allAgents = new AIAgent[] { managerAgent, workerAgent1, workerAgent2, workerAgent3, workerAgent4 };
        
        var manager = new ManagerGroupChatManager(
            managerAgent,
            allAgents,
            10,
            LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ManagerGroupChatManager>());
        
        var testableManager = new TestableManagerGroupChatManager(manager);
        
        Log("测试 UpdateHistoryAsync 方法:");
        
        var history = new List<ChatMessage>
        {
            new(ChatRole.User, "大家好，请开始讨论。"),
            new(ChatRole.Assistant, "好的，我们开始讨论。请@产品经理发言。") { AuthorName = "光哥-协调者" },
            new(ChatRole.Assistant, "我是产品经理，我来发言。") { AuthorName = "产品经理" },
            new(ChatRole.Assistant, "感谢产品经理。请@测试工程师发言。") { AuthorName = "光哥-协调者" },
            new(ChatRole.Assistant, "我是测试工程师，我来发言。") { AuthorName = "测试工程师" },
            new(ChatRole.Assistant, "感谢测试工程师。请@小明-架构师评估后端架构如何支撑高频变化的反爬策略和弹性扩容需求。") { AuthorName = "光哥-协调者" }
        };
        
        Log("原始消息历史:");
        foreach (var msg in history)
        {
            var author = msg.AuthorName ?? "User";
            var text = msg.Text ?? "";
            Log($"  [{author}]: {text.Substring(0, Math.Min(50, text.Length))}...");
        }
        
        var updatedHistory = await testableManager.TestUpdateHistoryAsync(history);
        
        Log("\n更新后的消息历史:");
        foreach (var msg in updatedHistory)
        {
            var author = msg.AuthorName ?? "User";
            var text = msg.Text ?? "";
            Log($"  [{author}]: {text.Substring(0, Math.Min(50, text.Length))}...");
        }
        
        Log("\n========== 测试完成 ==========");
    }
    
    private ChatClientAgent CreateMockAgent(string name, string description = "测试Agent")
    {
        var mockChatClient = new Mock<IChatClient>();
        mockChatClient.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, $"我是{name}，我收到了消息。")));
        
        var agent = new ChatClientAgent(
            mockChatClient.Object,
            description,
            name);
        
        return agent;
    }
    
    private class TestableManagerGroupChatManager
    {
        private readonly ManagerGroupChatManager _manager;
        private readonly MethodInfo _updateHistoryMethod;
        
        public TestableManagerGroupChatManager(ManagerGroupChatManager manager)
        {
            _manager = manager;
            _updateHistoryMethod = typeof(ManagerGroupChatManager).GetMethod("UpdateHistoryAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        }
        
        public async Task<IEnumerable<ChatMessage>> TestUpdateHistoryAsync(IReadOnlyList<ChatMessage> history)
        {
            var result = _updateHistoryMethod.Invoke(_manager, new object[] { history, CancellationToken.None });
            var valueTask = (ValueTask<IEnumerable<ChatMessage>?>)result;
            var updatedHistory = await valueTask;
            return updatedHistory ?? history;
        }
    }
}
