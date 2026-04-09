# MAF观察者模式架构设计文档

## 📋 概述

Microsoft Agent Framework (MAF) 中大量使用了**观察者模式（Observer Pattern）**，允许外部系统监听和响应Agent的内部事件。这是实现**实时监控、日志记录、人在回路、可视化看板**等高级功能的核心机制。

---

## 🎯 MAF中的观察者模式体现

### 1. ResponseCallback（响应回调）

**最基础的观察者模式**，用于监听Agent的每次响应。

```csharp
// MAF官方示例
ChatHistory history = [];

ValueTask responseCallback(ChatMessageContent response)
{
    history.Add(response);
    Console.WriteLine($"# {response.AuthorName}\n{response.Content}");
    return ValueTask.CompletedTask;
}

GroupChatOrchestration orchestration = new GroupChatOrchestration(
    manager,
    writer,
    editor)
{
    ResponseCallback = responseCallback,  // 注册观察者
};
```

**观察者模式体现**：
- ✅ **被观察者（Subject）**：Orchestration（工作流编排器）
- ✅ **观察者（Observer）**：ResponseCallback（回调函数）
- ✅ **通知机制**：每次Agent响应时，自动调用回调函数

**应用场景**：
1. **实时日志记录**：记录所有Agent的发言
2. **UI实时更新**：前端实时显示对话内容
3. **数据持久化**：保存对话历史到数据库
4. **性能监控**：统计Agent响应时间和Token消耗

---

### 2. InteractiveCallback（交互回调）

**人在回路的观察者模式**，用于监听需要人工干预的事件。

```csharp
HandoffOrchestration orchestration = new HandoffOrchestration(...)
{
    InteractiveCallback = () =>
    {
        Console.Write("User: ");
        string input = Console.ReadLine();
        return new ChatMessageContent(AuthorRole.User, input);
    }
};
```

**观察者模式体现**：
- ✅ **被观察者**：Orchestration
- ✅ **观察者**：InteractiveCallback
- ✅ **通知机制**：当需要人工输入时，触发回调

**应用场景**：
1. **关键决策点**：是否批准部署
2. **异常处理**：Agent遇到无法解决的问题
3. **用户确认**：需要用户确认操作
4. **数据补充**：需要用户提供额外信息

---

### 3. 中间件机制（Middleware Pattern）

**责任链模式 + 观察者模式**的组合，用于监听和拦截Agent的请求和响应。

```csharp
var builder = new ChatClientBuilder(baseClient);

// 添加日志中间件（观察者）
builder.UseLogging(logger);

// 添加功能调用中间件（观察者）
builder.UseFunctionInvocation(loggerFactory, options =>
{
    options.MaximumIterationsPerRequest = 10;
});

// 添加自定义中间件（观察者）
builder.Use(async (chatMessages, next, cancellationToken) =>
{
    // 前置观察：记录请求
    Console.WriteLine($"[Request] {chatMessages.Last().Content}");
    
    // 执行下一个中间件
    var response = await next(chatMessages, cancellationToken);
    
    // 后置观察：记录响应
    Console.WriteLine($"[Response] {response.Messages.Last().Text}");
    
    return response;
});

var client = builder.Build();
```

**观察者模式体现**：
- ✅ **被观察者**：ChatClient
- ✅ **观察者**：Middleware（中间件）
- ✅ **通知机制**：请求/响应经过中间件链时触发

**应用场景**：
1. **日志记录**：记录所有请求和响应
2. **性能监控**：统计响应时间
3. **错误处理**：捕获异常并记录
4. **安全审计**：记录敏感操作
5. **Token统计**：计算Token消耗

---

### 4. 事件总线（Event Bus）

**发布-订阅模式**，用于跨系统的事件通知。

```csharp
public interface IEventBus
{
    /// <summary>
    /// 订阅事件
    /// </summary>
    void Subscribe<TEvent>(Action<TEvent> handler);
    
    /// <summary>
    /// 发布事件
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event);
}

// 定义事件
public class AgentResponseEvent
{
    public long CollaborationId { get; set; }
    public string AgentName { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
}

public class TaskLedgerUpdatedEvent
{
    public long CollaborationId { get; set; }
    public TaskLedger Ledger { get; set; }
}

// 使用事件总线
public class WorkflowExecutionService
{
    private readonly IEventBus _eventBus;
    
    public async Task ExecuteAsync(long taskId)
    {
        // ...
        
        // 发布事件
        await _eventBus.PublishAsync(new AgentResponseEvent
        {
            CollaborationId = collaborationId,
            AgentName = agentName,
            Content = content,
            Timestamp = DateTime.UtcNow
        });
    }
}

// 订阅事件
public class DashboardService
{
    public DashboardService(IEventBus eventBus)
    {
        // 订阅Agent响应事件
        eventBus.Subscribe<AgentResponseEvent>(async @event =>
        {
            // 更新Dashboard
            await UpdateDashboardAsync(@event);
        });
        
        // 订阅任务账本更新事件
        eventBus.Subscribe<TaskLedgerUpdatedEvent>(async @event =>
        {
            // 更新任务账本显示
            await UpdateTaskLedgerAsync(@event);
        });
    }
}
```

**观察者模式体现**：
- ✅ **被观察者（发布者）**：WorkflowExecutionService
- ✅ **观察者（订阅者）**：DashboardService、LoggingService等
- ✅ **通知机制**：事件总线

**应用场景**：
1. **实时Dashboard**：更新可视化看板
2. **推送通知**：发送邮件、短信、Web Push
3. **数据同步**：同步到其他系统
4. **审计日志**：记录所有重要事件

---

## 🏗️ 完整架构设计

### 1. 观察者模式架构图

```
┌─────────────────────────────────────────────────────┐
│              MAF观察者模式架构                       │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│  Subject（被观察者）                                │
│  ┌───────────────────────────────────────────────┐ │
│  │  Orchestration                                │ │
│  │  ├─ GroupChatOrchestration                    │ │
│  │  ├─ MagenticOrchestration                     │ │
│  │  └─ SequentialOrchestration                   │ │
│  └───────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────┐ │
│  │  ChatClient                                   │ │
│  │  └─ Middleware Chain                          │ │
│  └───────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────┐ │
│  │  WorkflowExecutionService                     │ │
│  │  └─ EventBus Publisher                        │ │
│  └───────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
                        ↓ 通知
┌─────────────────────────────────────────────────────┐
│  Observer（观察者）                                 │
│  ┌───────────────────────────────────────────────┐ │
│  │  ResponseCallback                             │ │
│  │  ├─ 日志记录                                  │ │
│  │  ├─ UI更新                                    │ │
│  │  ├─ 数据持久化                                │ │
│  │  └─ 性能监控                                  │ │
│  └───────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────┐ │
│  │  InteractiveCallback                          │ │
│  │  ├─ 人工干预                                  │ │
│  │  ├─ 用户确认                                  │ │
│  │  └─ 异常处理                                  │ │
│  └───────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────┐ │
│  │  Middleware                                   │ │
│  │  ├─ LoggingMiddleware                         │ │
│  │  ├─ FunctionInvocationMiddleware              │ │
│  │  └─ CustomMiddleware                          │ │
│  └───────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────┐ │
│  │  EventBus Subscribers                         │ │
│  │  ├─ DashboardService                          │ │
│  │  ├─ NotificationService                       │ │
│  │  ├─ AuditService                              │ │
│  │  └─ AnalyticsService                          │ │
│  └───────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

---

### 2. 实现方案

#### 2.1 多层次观察者注册

```csharp
public class AdvancedOrchestrationService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<AdvancedOrchestrationService> _logger;
    private readonly IMessageRepository _messageRepository;
    private readonly IHubContext<WorkflowHub> _hubContext;
    
    public async Task<CollaborationResult> ExecuteAsync(
        long collaborationId,
        string input,
        OrchestratorConfig config)
    {
        // 1. 创建Orchestration
        var orchestration = new GroupChatOrchestration(
            manager,
            agents.ToArray())
        {
            // 2. 注册ResponseCallback观察者
            ResponseCallback = async (response) =>
            {
                // 多层次观察者处理
                await OnAgentResponseAsync(collaborationId, response);
            },
            
            // 3. 注册InteractiveCallback观察者（可选）
            InteractiveCallback = config.EnableHITL 
                ? () => OnHumanInputRequiredAsync(collaborationId) 
                : null
        };
        
        // 4. 执行
        using var runtime = new InProcessRuntime();
        await runtime.StartAsync();
        
        var result = await orchestration.InvokeAsync(input, runtime);
        var output = await result.GetValueAsync();
        
        await runtime.RunUntilIdleAsync();
        
        return new CollaborationResult { Output = output };
    }
    
    /// <summary>
    /// Agent响应观察者（多层次处理）
    /// </summary>
    private async Task OnAgentResponseAsync(
        long collaborationId, 
        ChatMessageContent response)
    {
        // 层次1：日志记录
        _logger.LogInformation(
            "[Collaboration {Id}] {Agent}: {Content}",
            collaborationId, response.AuthorName, response.Content);
        
        // 层次2：数据持久化
        await _messageRepository.CreateAsync(new Message
        {
            CollaborationId = collaborationId,
            AgentName = response.AuthorName,
            Content = response.Content,
            Timestamp = DateTime.UtcNow
        });
        
        // 层次3：实时推送（SignalR）
        await _hubContext.Clients
            .Group($"collaboration-{collaborationId}")
            .SendAsync("ReceiveMessage", new
            {
                agent = response.AuthorName,
                content = response.Content,
                timestamp = DateTime.UtcNow
            });
        
        // 层次4：事件总线发布
        await _eventBus.PublishAsync(new AgentResponseEvent
        {
            CollaborationId = collaborationId,
            AgentName = response.AuthorName,
            Content = response.Content,
            Timestamp = DateTime.UtcNow
        });
    }
    
    /// <summary>
    /// 人工输入观察者
    /// </summary>
    private async Task<ChatMessageContent> OnHumanInputRequiredAsync(
        long collaborationId)
    {
        // 推送通知
        await _eventBus.PublishAsync(new HumanInputRequiredEvent
        {
            CollaborationId = collaborationId,
            Message = "需要人工干预"
        });
        
        // 等待用户输入（通过SignalR）
        var userInput = await WaitForUserInputAsync(collaborationId);
        
        return new ChatMessageContent(AuthorRole.User, userInput);
    }
}
```

#### 2.2 中间件观察者链

```csharp
public class ObservabilityMiddleware
{
    public static ChatClientBuilder AddObservability(
        this ChatClientBuilder builder,
        ILogger logger,
        IEventBus eventBus,
        IMetricsCollector metrics)
    {
        // 1. 日志中间件（观察者）
        builder.UseLogging(logger);
        
        // 2. 性能监控中间件（观察者）
        builder.Use(async (messages, next, cancellationToken) =>
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var response = await next(messages, cancellationToken);
                
                // 记录性能指标
                stopwatch.Stop();
                metrics.RecordResponseTime(stopwatch.ElapsedMilliseconds);
                
                return response;
            }
            catch (Exception ex)
            {
                // 记录错误
                await eventBus.PublishAsync(new AgentErrorEvent
                {
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
                
                throw;
            }
        });
        
        // 3. Token统计中间件（观察者）
        builder.Use(async (messages, next, cancellationToken) =>
        {
            var response = await next(messages, cancellationToken);
            
            // 统计Token使用量
            var tokenCount = response.Messages.Sum(m => m.Text.Length / 4);  // 粗略估算
            metrics.RecordTokenUsage(tokenCount);
            
            await eventBus.PublishAsync(new TokenUsageEvent
            {
                TokenCount = tokenCount,
                Timestamp = DateTime.UtcNow
            });
            
            return response;
        });
        
        // 4. 功能调用中间件（观察者）
        builder.UseFunctionInvocation(loggerFactory, options =>
        {
            options.MaximumIterationsPerRequest = 10;
        });
        
        return builder;
    }
}

// 使用
var builder = new ChatClientBuilder(baseClient);
builder.AddObservability(logger, eventBus, metrics);
var client = builder.Build();
```

#### 2.3 事件总线实现

```csharp
public interface IEventBus
{
    void Subscribe<TEvent>(Action<TEvent> handler);
    void Subscribe<TEvent>(Func<TEvent, Task> handler);
    Task PublishAsync<TEvent>(TEvent @event);
}

public class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<object>> _handlers = new();
    private readonly ILogger<InMemoryEventBus> _logger;
    
    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        var handlers = _handlers.GetOrAdd(typeof(TEvent), _ => new List<object>());
        lock (handlers)
        {
            handlers.Add(handler);
        }
    }
    
    public void Subscribe<TEvent>(Func<TEvent, Task> handler)
    {
        var handlers = _handlers.GetOrAdd(typeof(TEvent), _ => new List<object>());
        lock (handlers)
        {
            handlers.Add(handler);
        }
    }
    
    public async Task PublishAsync<TEvent>(TEvent @event)
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            return;
        }
        
        foreach (var handler in handlers)
        {
            try
            {
                if (handler is Action<TEvent> syncHandler)
                {
                    syncHandler(@event);
                }
                else if (handler is Func<TEvent, Task> asyncHandler)
                {
                    await asyncHandler(@event);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling event {EventType}", typeof(TEvent).Name);
            }
        }
    }
}

// 注册到DI
services.AddSingleton<IEventBus, InMemoryEventBus>();
```

---

### 3. 前端实时观察者（SignalR）

#### 3.1 SignalR Hub

```csharp
public class WorkflowHub : Hub
{
    public async Task JoinCollaboration(long collaborationId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId, 
            $"collaboration-{collaborationId}");
    }
    
    public async Task LeaveCollaboration(long collaborationId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId, 
            $"collaboration-{collaborationId}");
    }
    
    public async Task SendHumanInput(long collaborationId, string input)
    {
        // 通知等待的用户输入
        await Clients
            .Group($"collaboration-{collaborationId}")
            .SendAsync("HumanInputReceived", input);
    }
}
```

#### 3.2 前端观察者

```tsx
import { HubConnectionBuilder } from '@microsoft/signalr';

function WorkflowMonitor({ collaborationId }) {
  const [messages, setMessages] = useState([]);
  const [taskLedger, setTaskLedger] = useState(null);
  const [progressLedger, setProgressLedger] = useState(null);
  
  useEffect(() => {
    // 创建SignalR连接
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/workflow')
      .withAutomaticReconnect()
      .build();
    
    // 注册观察者：接收消息
    connection.on('ReceiveMessage', (message) => {
      setMessages(prev => [...prev, message]);
    });
    
    // 注册观察者：接收任务账本更新
    connection.on('TaskLedgerUpdated', (ledger) => {
      setTaskLedger(ledger);
    });
    
    // 注册观察者：接收进度账本更新
    connection.on('ProgressLedgerUpdated', (ledger) => {
      setProgressLedger(ledger);
    });
    
    // 注册观察者：需要人工输入
    connection.on('HumanInputRequired', async (event) => {
      const userInput = await showInputDialog(event.message);
      await connection.send('SendHumanInput', collaborationId, userInput);
    });
    
    // 启动连接
    connection.start().then(() => {
      connection.send('JoinCollaboration', collaborationId);
    });
    
    return () => {
      connection.stop();
    };
  }, [collaborationId]);
  
  return (
    <div>
      {/* 实时显示消息 */}
      <MessageList messages={messages} />
      
      {/* 显示任务账本 */}
      {taskLedger && <TaskLedgerViewer ledger={taskLedger} />}
      
      {/* 显示进度账本 */}
      {progressLedger && <ProgressLedgerViewer ledger={progressLedger} />}
    </div>
  );
}
```

---

## 📊 观察者模式应用场景

### 1. 实时监控Dashboard

```
┌─────────────────────────────────────────────────────┐
│  实时监控Dashboard                                  │
└─────────────────────────────────────────────────────┘

┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ Agent状态    │  │ 性能指标     │  │ Token消耗    │
│              │  │              │  │              │
│ • 运行中     │  │ • 响应时间   │  │ • 已用Token  │
│ • 空闲       │  │ • 吞吐量     │  │ • 剩余Token  │
│ • 错误       │  │ • 错误率     │  │ • 成本估算   │
└──────────────┘  └──────────────┘  └──────────────┘

┌─────────────────────────────────────────────────────┐
│  实时对话流                                         │
│  ┌───────────────────────────────────────────────┐ │
│  │ [14:23:45] Manager: 请程序员发言              │ │
│  │ [14:23:52] 程序员: 我开始编写代码...          │ │
│  │ [14:24:01] 安全专家: 发现SQL注入风险...       │ │
│  └───────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

### 2. 审计日志

```csharp
public class AuditService
{
    public AuditService(IEventBus eventBus)
    {
        // 订阅所有重要事件
        eventBus.Subscribe<AgentResponseEvent>(OnAgentResponse);
        eventBus.Subscribe<HumanInputRequiredEvent>(OnHumanInputRequired);
        eventBus.Subscribe<ToolInvocationEvent>(OnToolInvocation);
        eventBus.Subscribe<AgentErrorEvent>(OnAgentError);
    }
    
    private async Task OnAgentResponse(AgentResponseEvent @event)
    {
        await _auditRepository.CreateAsync(new AuditLog
        {
            EventType = "AgentResponse",
            CollaborationId = @event.CollaborationId,
            AgentName = @event.AgentName,
            Content = @event.Content,
            Timestamp = @event.Timestamp
        });
    }
    
    private async Task OnToolInvocation(ToolInvocationEvent @event)
    {
        await _auditRepository.CreateAsync(new AuditLog
        {
            EventType = "ToolInvocation",
            ToolName = @event.ToolName,
            Parameters = @event.Parameters,
            Result = @event.Result,
            Timestamp = @event.Timestamp
        });
    }
}
```

### 3. 推送通知

```csharp
public class NotificationService
{
    public NotificationService(IEventBus eventBus)
    {
        // 订阅关键决策事件
        eventBus.Subscribe<HumanInputRequiredEvent>(OnHumanInputRequired);
        eventBus.Subscribe<TaskCompletedEvent>(OnTaskCompleted);
        eventBus.Subscribe<AgentErrorEvent>(OnAgentError);
    }
    
    private async Task OnHumanInputRequired(HumanInputRequiredEvent @event)
    {
        // 发送推送通知
        await _pushNotificationService.SendAsync(new PushNotification
        {
            Title = "需要人工干预",
            Message = @event.Message,
            CollaborationId = @event.CollaborationId
        });
    }
    
    private async Task OnTaskCompleted(TaskCompletedEvent @event)
    {
        // 发送邮件通知
        await _emailService.SendAsync(new Email
        {
            Subject = "任务已完成",
            Body = $"任务 {@event.TaskId} 已完成",
            To = @event.UserEmail
        });
    }
}
```

---

## ✅ 核心优势

### 1. **解耦**
- ✅ 被观察者和观察者完全解耦
- ✅ 可以动态添加/移除观察者
- ✅ 支持多个观察者同时监听

### 2. **扩展性**
- ✅ 新增观察者不影响现有代码
- ✅ 支持多层次观察者
- ✅ 支持异步观察者

### 3. **实时性**
- ✅ 事件立即通知
- ✅ 支持实时推送
- ✅ 支持WebSocket

### 4. **可观测性**
- ✅ 完整的审计日志
- ✅ 性能监控
- ✅ 错误追踪

---

## 🚀 实施建议

### 1. **分层观察者**

```
Layer 1: ResponseCallback（基础层）
  └─ 日志记录、数据持久化

Layer 2: Middleware（中间层）
  └─ 性能监控、Token统计、错误处理

Layer 3: EventBus（高级层）
  └─ 跨系统通知、推送通知、审计日志

Layer 4: SignalR（实时层）
  └─ 实时UI更新、人在回路
```

### 2. **性能优化**

```csharp
// 使用异步观察者，避免阻塞主流程
ResponseCallback = async (response) =>
{
    // 异步处理，不阻塞
    _ = Task.Run(async () =>
    {
        await ProcessResponseAsync(response);
    });
    
    return ValueTask.CompletedTask;
}
```

### 3. **错误隔离**

```csharp
// 观察者错误不影响主流程
try
{
    await _eventBus.PublishAsync(@event);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Observer error");
    // 继续执行，不抛出异常
}
```

---

## 📚 参考资料

1. [MAF官方文档 - 高级主题](https://learn.microsoft.com/zh-cn/semantic-kernel/frameworks/agent/agent-orchestration/advanced-topics)
2. [观察者模式（Observer Pattern）](https://en.wikipedia.org/wiki/Observer_pattern)
3. [SignalR官方文档](https://docs.microsoft.com/aspnet/core/signalr/)

---

## 🎯 总结

MAF的观察者模式主要体现在：

1. ✅ **ResponseCallback**：监听Agent响应
2. ✅ **InteractiveCallback**：监听人工输入需求
3. ✅ **Middleware**：监听请求/响应
4. ✅ **EventBus**：跨系统事件通知
5. ✅ **SignalR**：实时UI更新

**这些机制共同构成了MAF强大的可观测性和扩展性基础！** 🎯
