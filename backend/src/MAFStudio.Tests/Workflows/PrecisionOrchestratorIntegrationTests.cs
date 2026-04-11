using MAFStudio.Application.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;

namespace MAFStudio.Tests.Workflows;

public class PrecisionOrchestratorIntegrationTests
{
    private readonly string _logFile = Path.Combine(Path.GetTempPath(), "precision_integration_log.txt");

    private void Log(string message)
    {
        Console.WriteLine(message);
        File.AppendAllText(_logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
    }

    private ChatClientAgent CreateMockAgent(string name, string responseText)
    {
        var mockChatClient = new Mock<IChatClient>();

        mockChatClient
            .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText)));

        mockChatClient
            .Setup(x => x.GetStreamingResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                var updates = new List<ChatResponseUpdate>
                {
                    new(ChatRole.Assistant, responseText)
                    {
                        ResponseId = "mock-response",
                        ModelId = "mock-model"
                    }
                };
                return updates.ToAsyncEnumerable();
            });

        return new ChatClientAgent(mockChatClient.Object, $"你是{name}", name, $"专业{name}");
    }

    private Mock<IChatClient> CreateManagerChatClientMock(params string[] responses)
    {
        var mock = new Mock<IChatClient>();
        var responseQueue = new Queue<string>(responses);

        mock
            .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ChatResponse(new ChatMessage(ChatRole.Assistant, responseQueue.Dequeue())));

        mock
            .Setup(x => x.GetStreamingResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                var text = responseQueue.Dequeue();
                var updates = new List<ChatResponseUpdate>
                {
                    new(ChatRole.Assistant, text)
                    {
                        ResponseId = "mock-response",
                        ModelId = "mock-model"
                    }
                };
                return updates.ToAsyncEnumerable();
            });

        return mock;
    }

    [Fact]
    public async Task PrecisionOrchestrator_WorkersExecuteCorrectly()
    {
        File.Delete(_logFile);
        Log("========== PrecisionOrchestrator 集成测试 ==========");

        var managerAgent = CreateMockAgent("光哥-协调者", "协调者回复");
        var worker1 = CreateMockAgent("产品经理", "我是产品经理，我认为应该先做需求分析。");
        var worker2 = CreateMockAgent("测试工程师", "我是测试工程师，我建议先写测试用例。");

        Log($"Manager ID: {managerAgent.Id}, Name: {managerAgent.Name}");
        Log($"Worker1 ID: {worker1.Id}, Name: {worker1.Name}");
        Log($"Worker2 ID: {worker2.Id}, Name: {worker2.Name}");

        var managerChatClient = CreateManagerChatClientMock("接下来请 @测试工程师 继续发言。");

        var workers = new List<AIAgent> { worker1, worker2 };
        var orchestrator = new PrecisionOrchestrator(
            managerAgent,
            workers,
            managerChatClient.Object,
            5,
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<PrecisionOrchestrator>());

        var thinkingEvents = new List<ManagerThinkingEventArgs>();
        orchestrator.ManagerThinking += (_, args) =>
        {
            thinkingEvents.Add(args);
            Log($"[ManagerThinking] {args.ManagerName}: {args.Thinking}, SelectedAgent: {args.SelectedAgent}");
        };

        var workflow = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents =>
            {
                Log($"managerFactory 被调用，agents 数量: {agents.Count}");
                foreach (var a in agents)
                {
                    Log($"  Agent: Id={a.Id}, Name={a.Name}");
                }
                return orchestrator;
            })
            .AddParticipants(workers.ToArray())
            .Build();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "讨论一下项目方案")
        };

        Log("开始执行工作流...");

        var agentResponses = new List<(string AgentId, string Text)>();
        var eventTypes = new List<string>();
        string? errorMessage = null;

        try
        {
            await using var run = await InProcessExecution.RunStreamingAsync(workflow, messages);
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

            await foreach (var evt in run.WatchStreamAsync().ConfigureAwait(false))
            {
                eventTypes.Add(evt.GetType().Name);
                Log($"事件: {evt.GetType().Name}");

                if (evt is AgentResponseUpdateEvent updateEvent)
                {
                    var executorId = updateEvent.ExecutorId ?? "Unknown";
                    var text = updateEvent.Update?.Text ?? "";
                    Log($"  AgentResponseUpdate: ExecutorId={executorId}, Text={text.Substring(0, Math.Min(100, text.Length))}");
                    agentResponses.Add((executorId, text));
                }
                else if (evt is WorkflowOutputEvent)
                {
                    Log("  工作流输出");
                    break;
                }
                else if (evt is WorkflowErrorEvent errorEvent)
                {
                    errorMessage = errorEvent.Exception?.Message ?? "未知错误";
                    var innerEx = errorEvent.Exception?.InnerException;
                    Log($"  工作流错误: {errorMessage}");
                    Log($"  内部异常: {innerEx?.Message}");
                    break;
                }
                else if (evt is ExecutorFailedEvent failedEvent)
                {
                    Log($"  执行器失败: ExecutorId={failedEvent.ExecutorId}, Data={failedEvent.Data}");
                }

                if (eventTypes.Count > 50) break;
            }
        }
        catch (Exception ex)
        {
            Log($"外部异常: {ex.Message}\n{ex.StackTrace}");
        }

        Log($"\n========== 结果 ==========");
        Log($"总事件数: {eventTypes.Count}");
        Log($"Agent回复数: {agentResponses.Count}");
        Log($"ManagerThinking事件数: {thinkingEvents.Count}");
        Log($"错误消息: {errorMessage}");
        Log($"事件类型: {string.Join(", ", eventTypes.Distinct())}");

        foreach (var resp in agentResponses)
        {
            Log($"  Agent回复: [{resp.AgentId}] {resp.Text}");
        }

        Assert.NotEmpty(thinkingEvents);
        Assert.Equal("产品经理", thinkingEvents[0].SelectedAgent);
        Assert.Null(errorMessage);
        Assert.NotEmpty(agentResponses);
    }
}
