using System.Reflection;
using MAFStudio.Application.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;

namespace MAFStudio.Tests.Workflows;

public class ManagerGroupChatManagerDebugTests
{
    private readonly string _logFile = Path.Combine(Path.GetTempPath(), "test_manager_debug_log.txt");
    
    private void Log(string message)
    {
        Console.WriteLine(message);
        File.AppendAllText(_logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
    }
    
    [Fact]
    public void DebugFindMentionedAgent()
    {
        File.Delete(_logFile);
        Log("========== 调试 FindMentionedAgent 方法 ==========");
        
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
        
        Log("Worker Agent 列表:");
        var workerNames = testableManager.GetWorkerAgentNames();
        foreach (var name in workerNames)
        {
            Log($"  - {name}");
        }
        
        Log("\n测试 FindMentionedAgent 方法:");
        
        var testMessages = new[]
        {
            "请@产品经理发言。",
            "请@测试工程师发言。",
            "请@小明-架构师评估后端架构。",
            "请@志龙-项目经理制定计划。",
            "感谢@产品经理和@测试工程师的发言。",
            "没有@提及的消息。",
            "@小明 请发言。",
            "@架构师 请发言。"
        };
        
        foreach (var msg in testMessages)
        {
            var mentionedAgent = testableManager.TestFindMentionedAgent(msg);
            Log($"消息: \"{msg}\"");
            Log($"  匹配结果: {mentionedAgent ?? "无"}");
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
        private readonly FieldInfo _workerAgentNamesField;
        private readonly MethodInfo _findMentionedAgentMethod;
        
        public TestableManagerGroupChatManager(ManagerGroupChatManager manager)
        {
            _manager = manager;
            _workerAgentNamesField = typeof(ManagerGroupChatManager).GetField("_workerAgentNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _findMentionedAgentMethod = typeof(ManagerGroupChatManager).GetMethod("FindMentionedAgent", BindingFlags.NonPublic | BindingFlags.Instance);
        }
        
        public List<string> GetWorkerAgentNames()
        {
            return (List<string>)_workerAgentNamesField.GetValue(_manager);
        }
        
        public string? TestFindMentionedAgent(string message)
        {
            return (string?)_findMentionedAgentMethod.Invoke(_manager, new object[] { message });
        }
    }
}
