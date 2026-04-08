using System.Reflection;
using MAFStudio.Application.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;

namespace MAFStudio.Tests.Workflows;

public class SimpleGroupChatTests
{
    private readonly string _logFile = "D:/trae/test_simple_groupchat_log.txt";
    
    private void Log(string message)
    {
        Console.WriteLine(message);
        File.AppendAllText(_logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
    }
    
    [Fact]
    public async Task SimpleGroupChatTest()
    {
        File.Delete(_logFile);
        Log("========== 简单群聊测试 ==========");
        
        var managerAgent = CreateMockAgent("光哥-协调者", "你是协调者，负责分配任务。");
        var workerAgent1 = CreateMockAgent("小明-架构师", "你是架构师。");
        var workerAgent2 = CreateMockAgent("志龙-项目经理", "你是项目经理。");
        
        var allAgents = new AIAgent[] { managerAgent, workerAgent1, workerAgent2 };
        
        var manager = new ManagerGroupChatManager(
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
                    Log($"  堆栈: {errorEvent.Exception?.StackTrace}");
                }
                else if (evt is ExecutorFailedEvent failedEvent)
                {
                    var executorId = failedEvent.ExecutorId ?? "Unknown";
                    Log($"  ExecutorId: {executorId}");
                    
                    var props = failedEvent.GetType().GetProperties();
                    foreach (var prop in props)
                    {
                        var value = prop.GetValue(failedEvent);
                        Log($"  {prop.Name}: {value}");
                    }
                }
                
                if (eventCount > 20)
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
        
        Log("========== 测试完成 ==========");
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
}
