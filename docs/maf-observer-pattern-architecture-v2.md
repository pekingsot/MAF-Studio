# MAF观察者模式架构设计文档 v2.0（官方实现版）

## 📋 概述

Microsoft Agent Framework (MAF) 中的观察者模式主要通过**事件流（Streaming Events）**和**可观测性集成（OpenTelemetry）**来体现。它允许你在不干扰Agent执行逻辑的情况下，实时"监视"群聊或Magentic的每一步思考和动作。

---

## 🎯 MAF观察者模式的核心体现

### 1. 事件流（Streaming Events）

**最核心的观察者机制**，通过`WatchStreamAsync()`监听所有执行事件。

```csharp
// MAF官方示例：实时监听Agent执行过程
using var runtime = new InProcessRuntime();
await runtime.StartAsync();

var orchestration = new GroupChatOrchestration(manager, agents.ToArray());

// 启动工作流
var result = await orchestration.InvokeAsync(task, runtime);

// 核心：通过WatchStreamAsync监听事件流
await foreach (var @event in run.WatchStreamAsync())
{
    switch (@event)
    {
        case AgentResponseUpdateEvent responseEvent:
            // 流式回复事件
            Console.WriteLine($"[{responseEvent.AgentName}] {responseEvent.Content}");
            break;
            
        case AgentThoughtEvent thoughtEvent:
            // 推理思考事件
            Console.WriteLine($"[Thought] {thoughtEvent.Thought}");
            break;
            
        case ToolCallEvent toolEvent:
            // 工具调用事件
            Console.WriteLine($"[Tool] {toolEvent.ToolName}({toolEvent.Parameters})");
            break;
    }
}

var output = await result.GetValueAsync();
await runtime.RunUntilIdleAsync();
```

**观察者模式体现**：
- ✅ **被观察者**：Workflow Run（工作流运行实例）
- ✅ **观察者**：WatchStreamAsync() 循环
- ✅ **通知机制**：事件流

**核心事件类型**：

| 事件类型 | 说明 | 应用场景 |
|---------|------|---------|
| `AgentResponseUpdateEvent` | Agent流式回复 | 实时显示对话内容 |
| `AgentThoughtEvent` | Agent推理思考 | 显示思考过程 |
| `ToolCallEvent` | 工具调用 | 显示工具执行 |
| `PlanReviewEvent` | 计划审核 | 人在回路 |

---

### 2. 中间件拦截

**在Agent发言前后拦截和记录信息**。

```csharp
var builder = new ChatClientBuilder(baseClient);

// 自定义中间件（观察者）
builder.Use(async (messages, next, cancellationToken) =>
{
    // 前置观察：记录请求
    Console.WriteLine($"[Request] {messages.Last().Content}");
    
    var response = await next(messages, cancellationToken);
    
    // 后置观察：记录响应
    Console.WriteLine($"[Response] {response.Messages.Last().Text}");
    
    return response;
});

// 功能调用中间件（观察者）
builder.UseFunctionInvocation(loggerFactory, options =>
{
    options.MaximumIterationsPerRequest = 10;
});

var client = builder.Build();
```

**观察者模式体现**：
- ✅ **被观察者**：ChatClient
- ✅ **观察者**：Middleware
- ✅ **通知机制**：请求/响应拦截

---

### 3. 人工审核

**在关键决策点暂停并请求人工介入**。

```csharp
// 监听计划审核事件
await foreach (var @event in run.WatchStreamAsync())
{
    if (@event is PlanReviewEvent planEvent)
    {
        // 显示计划
        Console.WriteLine("计划审核：");
        Console.WriteLine(planEvent.Plan);
        
        // 请求人工审核
        Console.Write("是否批准？;
        var approval = Console.ReadLine();
        
        if (approval?.ToLower() != "yes")
        {
            // 拒绝计划，终止执行
            await run.CancelAsync();
            break;
        }
    }
}
```

**观察者模式体现**：
- ✅ **被观察者**：Orchestration
- ✅ **观察者**：PlanReviewEvent监听器
- ✅ **通知机制**：事件触发

---

## 🛠️ 工具层面的可视化观察

### 1. DevUI（本地Web界面）

**MAF内置的可视化工具**，直观显示群聊拓扑图、Magentic任务状态、Agent推理过程。

```bash
# 启动DevUI
dotnet run --project MyAgentApp.csproj --devui
```

**功能**：
- ✅ 群聊拓扑图可视化
- ✅ Magentic任务状态追踪
- ✅ Agent推理过程展示
- ✅ 模型耗时统计
- ✅ Token消耗统计

**界面示例**：

```
┌─────────────────────────────────────────────────────┐
│  DevUI Dashboard                                    │
└─────────────────────────────────────────────────────┘

┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ Manager      │  │ Architect    │  │ Coder        │
│              │  │              │  │              │
│ Status: ✓    │  │ Status: ✓    │  │ Status: ⏳   │
│ Messages: 5  │  │ Messages: 3  │  │ Messages: 2  │
│ Tokens: 1.2k │  │ Tokens: 800  │  │ Tokens: 500  │
└──────────────┘  └──────────────┘  └──────────────┘

┌─────────────────────────────────────────────────────┐
│  Workflow Topology                                  │
│                                                     │
│    Manager ──→ Architect ──→ Coder                 │
│       │              │            │                │
│       └──────────────┴────────────┘                │
│                  ↓                                  │
│              Reviewer                               │
└─────────────────────────────────────────────────────┘
```

---

### 2. .NET Aspire Dashboard（分布式追踪）

**结合OpenTelemetry，将Agent执行过程转化为标准的分布式链路追踪**。

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 添加OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Microsoft.Agents.*")  // MAF的追踪源
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());  // 导出到Aspire Dashboard

var app = builder.Build();
app.Run();
```

**Aspire Dashboard功能**：
- ✅ 分布式链路追踪
- ✅ 可视化任务流转路径（Manager → Architect → Coder）
- ✅ 性能分析
- ✅ 错误追踪
- ✅ 依赖关系图

**链路追踪示例**：

```
┌─────────────────────────────────────────────────────┐
│  Distributed Tracing                                │
└─────────────────────────────────────────────────────┘

Trace ID: abc123-def456-ghi789
Duration: 12.5s

┌─ Manager (2.3s)
│  └─ Planning (1.2s)
│     └─ SelectNextAgent (0.8s)
│        └─ LLM Call (0.6s)
│
├─ Architect (4.5s)
│  └─ Design (3.2s)
│     └─ Tool: DrawDiagram (1.5s)
│
├─ Coder (5.7s)
│  └─ Implementation (4.3s)
│     └─ Tool: WriteCode (2.8s)
│
└─ Reviewer (3.1s)
   └─ Review (2.5s)
      └─ Tool: CodeReview (1.8s)
```

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
│  │  Workflow Run                                 │ │
│  │  ├─ GroupChatOrchestration                    │ │
│  │  ├─ MagenticOrchestration                     │ │
│  │  └─ SequentialOrchestration                   │ │
│  └───────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────┐ │
│  │  ChatClient                                   │ │
│  │  └─ Middleware Chain                          │ │
│  └───────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
                        ↓ 事件流
┌─────────────────────────────────────────────────────┐
│  Observer（观察者）                                 │
│  ┌───────────────────────────────────────────────┐ │
│  │  WatchStreamAsync() 循环                      │ │
│  │  ├─ AgentResponseUpdateEvent                  │ │
│  │  ├─ AgentThoughtEvent                         │ │
│  │  ├─ ToolCallEvent                             │ │
│  │  └─ PlanReviewEvent                           │ │
│  └───────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────┐ │
│  │  Middleware                                   │ │
│  │  ├─ LoggingMiddleware                         │ │
│  │  ├─ FunctionInvocationMiddleware              │ │
│  │  └─ CustomMiddleware                          │ │
│  └───────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────┐ │
│  │  可视化工具                                    │ │
│  │  ├─ DevUI（本地Web界面）                      │ │
│  │  └─ .NET Aspire Dashboard（分布式追踪）       │ │
│  └───────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

---

### 2. 实现方案

#### 2.1 多层次事件监听

```csharp
public class AdvancedOrchestrationService
{
    private readonly ILogger<AdvancedOrchestrationService> _logger;
    private readonly IMessageRepository _messageRepository;
    private readonly IHubContext<WorkflowHub> _hubContext;
    private readonly IMetricsCollector _metrics;
    
    public async Task<CollaborationResult> ExecuteAsync(
        long collaborationId,
        string input,
        OrchestratorConfig config)
    {
        // 1. 创建Orchestration
        var orchestration = new GroupChatOrchestration(
            manager,
            agents.ToArray());
        
        // 2. 启动Runtime
        using var runtime = new InProcessRuntime();
        await runtime.StartAsync();
        
        // 3. 启动工作流
        var result = await orchestration.InvokeAsync(input, runtime);
        
        // 4. 核心：监听事件流（多层次观察者）
        var executionTask = Task.Run(async () =>
        {
            await foreach (var @event in result.WatchStreamAsync())
            {
                await HandleEventAsync(collaborationId, @event);
            }
        });
        
        // 5. 等待完成
        var output = await result.GetValueAsync();
        await runtime.RunUntilIdleAsync();
        
        await executionTask;
        
        return new CollaborationResult { Output = output };
    }
    
    /// <summary>
    /// 多层次事件处理
    /// </summary>
    private async Task HandleEventAsync(long collaborationId, object @event)
    {
        switch (@event)
        {
            case AgentResponseUpdateEvent responseEvent:
                await OnAgentResponseAsync(collaborationId, responseEvent);
                break;
                
            case AgentThoughtEvent thoughtEvent:
                await OnAgentThoughtAsync(collaborationId, thoughtEvent);
                break;
                
            case ToolCallEvent toolEvent:
                await OnToolCallAsync(collaborationId, toolEvent);
                break;
                
            case PlanReviewEvent planEvent:
                await OnPlanReviewAsync(collaborationId, planEvent);
                break;
        }
    }
    
    /// <summary>
    /// Agent响应事件处理
    /// </summary>
    private async Task OnAgentResponseAsync(
        long collaborationId,
        AgentResponseUpdateEvent responseEvent)
    {
        // 层次1：日志记录
        _logger.LogInformation(
            "[Collaboration {Id}] {Agent}: {Content}",
            collaborationId,
            responseEvent.AgentName,
            responseEvent.Content);
        
        // 层次2：数据持久化
        await _messageRepository.CreateAsync(new Message
        {
            CollaborationId = collaborationId,
            AgentName = responseEvent.AgentName,
            Content = responseEvent.Content,
            Timestamp = DateTime.UtcNow
        });
        
        // 层次3：实时推送
        await _hubContext.Clients
            .Group($"collaboration-{collaborationId}")
            .SendAsync("ReceiveMessage", new
            {
                agent = responseEvent.AgentName,
                content = responseEvent.Content,
                timestamp = DateTime.UtcNow
            });
        
        // 层次4：性能统计
        _metrics.RecordAgentResponse(
            responseEvent.AgentName,
            responseEvent.Duration);
    }
    
    /// <summary>
    /// Agent思考事件处理
    /// </summary>
    private async Task OnAgentThoughtAsync(
        long collaborationId,
        AgentThoughtEvent thoughtEvent)
    {
        // 记录思考过程（用于调试和分析）
        _logger.LogDebug(
            "[Thought] {Agent}: {Thought}",
            thoughtEvent.AgentName,
            thoughtEvent.Thought);
        
        // 如果显示级别为Detailed或Full，推送到前端
        if (_visibilityLevel >= VisibilityLevel.Detailed)
        {
            await _hubContext.Clients
                .Group($"collaboration-{collaborationId}")
                .SendAsync("AgentThought", new
                {
                    agent = thoughtEvent.AgentName,
                    thought = thoughtEvent.Thought,
                    timestamp = DateTime.UtcNow
                });
        }
    }
    
    /// <summary>
    /// 工具调用事件处理
    /// </summary>
    private async Task OnToolCallAsync(
        long collaborationId,
        ToolCallEvent toolEvent)
    {
        // 记录工具调用
        _logger.LogInformation(
            "[Tool] {ToolName}({Parameters})",
            toolEvent.ToolName,
            toolEvent.Parameters);
        
        // 推送到前端
        await _hubContext.Clients
            .Group($"collaboration-{collaborationId}")
            .SendAsync("ToolCall", new
            {
                tool = toolEvent.ToolName,
                parameters = toolEvent.Parameters,
                result = toolEvent.Result,
                duration = toolEvent.Duration,
                timestamp = DateTime.UtcNow
            });
        
        // 统计工具使用
        _metrics.RecordToolCall(
            toolEvent.ToolName,
            toolEvent.Duration,
            toolEvent.Success);
    }
    
    /// <summary>
    /// 计划审核事件处理（人在回路）
    /// </summary>
    private async Task OnPlanReviewAsync(
        long collaborationId,
        PlanReviewEvent planEvent)
    {
        // 推送审核请求到前端
        await _hubContext.Clients
            .Group($"collaboration-{collaborationId}")
            .SendAsync("PlanReviewRequired", new
            {
                plan = planEvent.Plan,
                timestamp = DateTime.UtcNow
            });
        
        // 等待用户审核
        var approval = await WaitForUserApprovalAsync(collaborationId);
        
        if (!approval)
        {
            // 用户拒绝，取消执行
            throw new OperationCanceledException("Plan rejected by user");
        }
    }
}
```

#### 2.2 OpenTelemetry集成

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 添加OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Microsoft.Agents.*")  // MAF的追踪源
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");  // Aspire Dashboard
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

// 添加MAF服务
builder.Services.AddAgents()
    .AddOpenAIChatClient(builder.Configuration);

var app = builder.Build();
app.Run();
```

#### 2.3 DevUI集成

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 启用DevUI（开发环境）
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDevUI();
}

var app = builder.Build();

// 启用DevUI中间件
if (app.Environment.IsDevelopment())
{
    app.UseDevUI();
}

app.Run();
```

**访问DevUI**：
```
http://localhost:5000/devui
```

---

### 3. 前端实时观察者

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
    
    public async Task ApprovePlan(long collaborationId, bool approved)
    {
        // 通知等待的审核流程
        await Clients
            .Group($"collaboration-{collaborationId}")
            .SendAsync("PlanApproved", approved);
    }
}
```

#### 3.2 前端观察者

```tsx
import { HubConnectionBuilder } from '@microsoft/signalr';

function WorkflowMonitor({ collaborationId }) {
  const [messages, setMessages] = useState([]);
  const [thoughts, setThoughts] = useState([]);
  const [toolCalls, setToolCalls] = useState([]);
  const [planReview, setPlanReview] = useState(null);
  
  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/workflow')
      .withAutomaticReconnect()
      .build();
    
    // 注册观察者：接收消息
    connection.on('ReceiveMessage', (message) => {
      setMessages(prev => [...prev, message]);
    });
    
    // 注册观察者：接收思考过程
    connection.on('AgentThought', (thought) => {
      setThoughts(prev => [...prev, thought]);
    });
    
    // 注册观察者：接收工具调用
    connection.on('ToolCall', (toolCall) => {
      setToolCalls(prev => [...prev, toolCall]);
    });
    
    // 注册观察者：接收计划审核请求
    connection.on('PlanReviewRequired', async (event) => {
      setPlanReview(event);
    });
    
    connection.start().then(() => {
      connection.send('JoinCollaboration', collaborationId);
    });
    
    return () => connection.stop();
  }, [collaborationId]);
  
  const handlePlanApproval = async (approved) => {
    await connection.send('ApprovePlan', collaborationId, approved);
    setPlanReview(null);
  };
  
  return (
    <div>
      {/* 实时对话 */}
      <MessageList messages={messages} />
      
      {/* 思考过程（可折叠） */}
      <Collapse>
        <Panel header="思考过程" key="thoughts">
          <ThoughtList thoughts={thoughts} />
        </Panel>
      </Collapse>
      
      {/* 工具调用 */}
      <Collapse>
        <Panel header="工具调用" key="tools">
          <ToolCallList toolCalls={toolCalls} />
        </Panel>
      </Collapse>
      
      {/* 计划审核（人在回路） */}
      {planReview && (
        <Modal
          title="计划审核"
          visible={true}
          onOk={() => handlePlanApproval(true)}
          onCancel={() => handlePlanApproval(false)}
        >
          <pre>{planReview.plan}</pre>
        </Modal>
      )}
    </div>
  );
}
```

---

## 📊 观察者模式的三个核心体现点

### 1. 解耦

**监控工具作为观察者不影响Agent实际运行**。

```csharp
// 被观察者：不知道观察者的存在
var orchestration = new GroupChatOrchestration(manager, agents);

// 观察者：独立监听，不影响执行
await foreach (var @event in run.WatchStreamAsync())
{
    // 处理事件，不影响主流程
    await HandleEventAsync(@event);
}
```

### 2. 中间件拦截

**通过自定义Middleware在Agent发言前拦截或记录信息**。

```csharp
builder.Use(async (messages, next, cancellationToken) =>
{
    // 拦截请求
    LogRequest(messages);
    
    var response = await next(messages, cancellationToken);
    
    // 拦截响应
    LogResponse(response);
    
    return response;
});
```

### 3. 人工审核

**在协调者抛出PlanReviewEvent时，观察者可暂停并介入计划审批**。

```csharp
await foreach (var @event in run.WatchStreamAsync())
{
    if (@event is PlanReviewEvent planEvent)
    {
        // 暂停执行，等待人工审核
        var approved = await WaitForUserApprovalAsync(planEvent.Plan);
        
        if (!approved)
        {
            await run.CancelAsync();
        }
    }
}
```

---

## ✅ 核心优势

### 1. **实时性**
- ✅ 事件流实时推送
- ✅ 无需轮询
- ✅ 低延迟

### 2. **解耦**
- ✅ 观察者不影响执行
- ✅ 可动态添加/移除观察者
- ✅ 支持多个观察者

### 3. **可视化**
- ✅ DevUI本地Web界面
- ✅ .NET Aspire Dashboard分布式追踪
- ✅ 完整的链路可视化

### 4. **标准化**
- ✅ 基于OpenTelemetry标准
- ✅ 支持多种后端（Jaeger、Zipkin等）
- ✅ 可扩展

---

## 🚀 实施建议

### 1. **分层观察**

```
Layer 1: WatchStreamAsync（核心层）
  └─ 实时事件流监听

Layer 2: Middleware（中间层）
  └─ 请求/响应拦截

Layer 3: SignalR（实时层）
  └─ 前端实时更新

Layer 4: OpenTelemetry（追踪层）
  └─ 分布式链路追踪

Layer 5: DevUI（可视化层）
  └─ 本地Web界面
```

### 2. **性能优化**

```csharp
// 异步处理事件，避免阻塞
await foreach (var @event in run.WatchStreamAsync())
{
    // 异步处理，不阻塞主流程
    _ = Task.Run(async () =>
    {
        await HandleEventAsync(@event);
    });
}
```

### 3. **错误隔离**

```csharp
// 观察者错误不影响主流程
try
{
    await HandleEventAsync(@event);
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
2. [OpenTelemetry官方文档](https://opentelemetry.io/)
3. [.NET Aspire官方文档](https://docs.microsoft.com/dotnet/aspire/)

---

## 🎯 总结

MAF的观察者模式主要体现在：

1. ✅ **WatchStreamAsync()**：核心事件流监听
2. ✅ **Middleware**：请求/响应拦截
3. ✅ **PlanReviewEvent**：人在回路
4. ✅ **DevUI**：本地Web可视化
5. ✅ **.NET Aspire Dashboard**：分布式追踪

**这些机制共同构成了MAF强大的可观测性和扩展性基础！** 🎯
