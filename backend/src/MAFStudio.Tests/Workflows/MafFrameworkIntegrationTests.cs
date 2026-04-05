using System.Reflection;
using MAFStudio.Application.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;

namespace MAFStudio.Tests.Workflows;

public class MafFrameworkIntegrationTests
{
    private readonly string _logFile = "D:/trae/test_maf_integration_log.txt";
    
    private void Log(string message)
    {
        Console.WriteLine(message);
        File.AppendAllText(_logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
    }
    
    [Fact]
    public async Task TestMafFrameworkCallsUpdateHistory()
    {
        File.Delete(_logFile);
        Log("========== 测试 MAF 框架是否调用 UpdateHistoryAsync ==========");
        
        var managerAgent = CreateMockAgent("光哥-协调者", "你是协调者，负责分配任务。");
        var workerAgent1 = CreateMockAgent("产品经理", "你是产品经理。");
        var workerAgent2 = CreateMockAgent("测试工程师", "你是测试工程师。");
        
        var allAgents = new AIAgent[] { managerAgent, workerAgent1, workerAgent2 };
        
        var manager = new TestableManagerGroupChatManager(
            managerAgent,
            allAgents,
            5,
            LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ManagerGroupChatManager>());
        
        var workflow = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents => manager)
            .AddParticipants(allAgents)
            .Build();
        
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "大家好，请开始讨论。")
        };
        
        Log("开始执行工作流...");
        
        try
        {
            await using var run = await InProcessExecution.RunStreamingAsync(workflow, messages);
            Log("工作流启动成功");
            
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
            Log("发送初始消息成功");
            
            var eventCount = 0;
            await foreach (var evt in run.WatchStreamAsync().ConfigureAwait(false))
            {
                eventCount++;
                Log($"事件 #{eventCount}: {evt.GetType().Name}");
                
                if (evt is AgentResponseUpdateEvent updateEvent)
                {
                    var executorId = updateEvent.ExecutorId ?? "Unknown";
                    var text = updateEvent.Update?.Text ?? "";
                    Log($"  ExecutorId: {executorId}");
                    Log($"  Text: {text.Substring(0, Math.Min(100, text.Length))}");
                }
                else if (evt is WorkflowOutputEvent output)
                {
                    Log("  工作流输出事件");
                    break;
                }
                else if (evt is WorkflowErrorEvent errorEvent)
                {
                    Log($"  错误: {errorEvent.Exception?.Message}");
                }
                
                if (eventCount > 30)
                {
                    Log("达到最大事件数，停止监听");
                    break;
                }
            }
            
            Log($"总共收到 {eventCount} 个事件");
        }
        catch (Exception ex)
        {
            Log($"错误: {ex.Message}");
            Log($"堆栈: {ex.StackTrace}");
        }
        
        Log("\n方法调用统计:");
        Log($"  UpdateHistoryAsync 调用次数: {manager.UpdateHistoryCallCount}");
        Log($"  SelectNextAgentAsync 调用次数: {manager.SelectNextAgentCallCount}");
        
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
    
    private class TestableManagerGroupChatManager : ManagerGroupChatManager
    {
        public int UpdateHistoryCallCount { get; private set; } = 0;
        public int SelectNextAgentCallCount { get; private set; } = 0;
        
        public TestableManagerGroupChatManager(
            AIAgent managerAgent,
            IReadOnlyList<AIAgent> allAgents,
            int maximumIterationCount = 10,
            ILogger<ManagerGroupChatManager>? logger = null)
            : base(managerAgent, allAgents, maximumIterationCount, logger)
        {
        }
        
        protected override ValueTask<IEnumerable<ChatMessage>?> UpdateHistoryAsync(
            IReadOnlyList<ChatMessage> history,
            CancellationToken cancellationToken = default)
        {
            UpdateHistoryCallCount++;
            Console.WriteLine($"[TestableManager] UpdateHistoryAsync 被调用，次数: {UpdateHistoryCallCount}");
            return base.UpdateHistoryAsync(history, cancellationToken);
        }
        
        protected override ValueTask<AIAgent?> SelectNextAgentAsync(
            IReadOnlyList<ChatMessage> history,
            CancellationToken cancellationToken = default)
        {
            SelectNextAgentCallCount++;
            Console.WriteLine($"[TestableManager] SelectNextAgentAsync 被调用，次数: {SelectNextAgentCallCount}");
            return base.SelectNextAgentAsync(history, cancellationToken);
        }
    }
}
