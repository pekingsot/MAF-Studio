# MAF观察者模式架构设计文档 v3.0（完整实现版）

## 📋 概述

在.NET环境下，实现MAF观察者模式最优雅且专业的方法是利用**Streaming（流式输出）**结合**Middleware（中间件）**。本文档提供三种观察方案：代码级（适合做自己的UI）、系统级（适合调试）、中间件级（适合拦截和增强）。

---

## 🎯 三种观察方案对比

| 方案 | 适用场景 | 难度 | 功能 |
|------|---------|------|------|
| **方案一：代码级观察** | 自定义UI、实时监控 | ⭐⭐ | 实时捕获内部对话 |
| **方案二：系统级观察** | 调试、性能分析 | ⭐⭐⭐ | 分布式追踪、可视化 |
| **方案三：中间件观察** | 拦截、增强、过滤 | ⭐⭐⭐⭐ | 发言前后拦截 |

---

## 方案一：代码级观察（实时捕获内部对话）

### 1. 核心原理

通过监听`ExecuteStreamingAsync`产生的事件流，就像在群聊里装了一个"窃听器"，能实时看到协调者在点谁的名，以及Agent正在写什么。

### 2. 完整实现

```csharp
public class StreamingObserverService
{
    private readonly ILogger<StreamingObserverService> _logger;
    private readonly IMessageRepository _messageRepository;
    private readonly IHubContext<WorkflowHub> _hubContext;
    
    public async Task<CollaborationResult> ExecuteWithObservationAsync(
        long collaborationId,
        string userRequest,
        IOrchestrator orchestrator)
    {
        var messages = new List<Message>();
        var thoughts = new List<AgentThought>();
        var toolCalls = new List<ToolCall>();
        
        // 启动流式执行
        var responseStream = orchestrator.ExecuteStreamingAsync(userRequest);
        
        // 实时监听事件流
        await foreach (var message in responseStream)
        {
            // 观察：谁在说话？
            if (message is AgentResponseUpdateEvent update)
            {
                await OnAgentResponseAsync(collaborationId, update, messages);
            }
            
            // 观察：协调者的思考过程（只有Magentic模式有）
            if (message is OrchestratorThoughtEvent thought)
            {
                await OnOrchestratorThoughtAsync(collaborationId, thought, thoughts);
            }
            
            // 观察：Agent是否在调用工具（比如查数据库）
            if (message is ToolCallEvent toolCall)
            {
                await OnToolCallAsync(collaborationId, toolCall, toolCalls);
            }
            
            // 观察：工具调用结果
            if (message is ToolCallResultEvent toolResult)
            {
                await OnToolCallResultAsync(collaborationId, toolResult);
            }
        }
        
        return new CollaborationResult
        {
            Messages = messages,
            Thoughts = thoughts,
            ToolCalls = toolCalls
        };
    }
    
    /// <summary>
    /// Agent响应事件处理
    /// </summary>
    private async Task OnAgentResponseAsync(
        long collaborationId,
        AgentResponseUpdateEvent update,
        List<Message> messages)
    {
        // 控制台输出（调试用）
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"[{update.AgentName}]: ");
        Console.ResetColor();
        Console.WriteLine(update.Content);
        
        // 持久化到数据库
        var msg = new Message
        {
            CollaborationId = collaborationId,
            AgentName = update.AgentName,
            Content = update.Content,
            Timestamp = DateTime.UtcNow
        };
        
        await _messageRepository.CreateAsync(msg);
        messages.Add(msg);
        
        // 实时推送到前端
        await _hubContext.Clients
            .Group($"collaboration-{collaborationId}")
            .SendAsync("ReceiveMessage", new
            {
                agent = update.AgentName,
                content = update.Content,
                timestamp = DateTime.UtcNow
            });
    }
    
    /// <summary>
    /// 协调者思考事件处理
    /// </summary>
    private async Task OnOrchestratorThoughtAsync(
        long collaborationId,
        OrchestratorThoughtEvent thought,
        List<AgentThought> thoughts)
    {
        // 控制台输出（调试用）
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n[思考中...]: {thought.Content}\n");
        Console.ResetColor();
        
        // 记录思考过程
        var thoughtRecord = new AgentThought
        {
            CollaborationId = collaborationId,
            Content = thought.Content,
            Timestamp = DateTime.UtcNow
        };
        
        thoughts.Add(thoughtRecord);
        
        // 推送到前端（如果显示级别允许）
        await _hubContext.Clients
            .Group($"collaboration-{collaborationId}")
            .SendAsync("OrchestratorThought", new
            {
                content = thought.Content,
                timestamp = DateTime.UtcNow
            });
    }
    
    /// <summary>
    /// 工具调用事件处理
    /// </summary>
    private async Task OnToolCallAsync(
        long collaborationId,
        ToolCallEvent toolCall,
        List<ToolCall> toolCalls)
    {
        // 控制台输出
        Console.WriteLine($"[动作]: 正在调用工具 {toolCall.FunctionName}...");
        
        // 记录工具调用
        var toolRecord = new ToolCall
        {
            CollaborationId = collaborationId,
            ToolName = toolCall.FunctionName,
            Parameters = toolCall.Arguments,
            Timestamp = DateTime.UtcNow
        };
        
        toolCalls.Add(toolRecord);
        
        // 推送到前端
        await _hubContext.Clients
            .Group($"collaboration-{collaborationId}")
            .SendAsync("ToolCall", new
            {
                tool = toolCall.FunctionName,
                parameters = toolCall.Arguments,
                timestamp = DateTime.UtcNow
            });
    }
    
    /// <summary>
    /// 工具调用结果事件处理
    /// </summary>
    private async Task OnToolCallResultAsync(
        long collaborationId,
        ToolCallResultEvent toolResult)
    {
        Console.WriteLine($"[结果]: {toolResult.Result}");
        
        // 推送到前端
        await _hubContext.Clients
            .Group($"collaboration-{collaborationId}")
            .SendAsync("ToolCallResult", new
            {
                result = toolResult.Result,
                timestamp = DateTime.UtcNow
            });
    }
}
```

### 3. 事件类型详解

| 事件类型 | 说明 | 包含信息 |
|---------|------|---------|
| `AgentResponseUpdateEvent` | Agent流式回复 | AgentName, Content |
| `OrchestratorThoughtEvent` | 协调者思考 | Content（仅Magentic模式） |
| `ToolCallEvent` | 工具调用 | FunctionName, Arguments |
| `ToolCallResultEvent` | 工具调用结果 | Result |

### 4. 前端集成

```tsx
import { HubConnectionBuilder } from '@microsoft/signalr';

function WorkflowMonitor({ collaborationId }) {
  const [messages, setMessages] = useState([]);
  const [thoughts, setThoughts] = useState([]);
  const [toolCalls, setToolCalls] = useState([]);
  
  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/workflow')
      .withAutomaticReconnect()
      .build();
    
    // 监听Agent响应
    connection.on('ReceiveMessage', (message) => {
      setMessages(prev => [...prev, message]);
    });
    
    // 监听协调者思考
    connection.on('OrchestratorThought', (thought) => {
      setThoughts(prev => [...prev, thought]);
    });
    
    // 监听工具调用
    connection.on('ToolCall', (toolCall) => {
      setToolCalls(prev => [...prev, toolCall]);
    });
    
    connection.start().then(() => {
      connection.send('JoinCollaboration', collaborationId);
    });
    
    return () => connection.stop();
  }, [collaborationId]);
  
  return (
    <div>
      {/* 实时对话 */}
      <div className="messages">
        {messages.map((msg, i) => (
          <div key={i} className="message">
            <strong>{msg.agent}:</strong> {msg.content}
          </div>
        ))}
      </div>
      
      {/* 协调者思考（可折叠） */}
      <Collapse>
        <Panel header="协调者思考过程" key="thoughts">
          {thoughts.map((thought, i) => (
            <div key={i} className="thought">
              {thought.content}
            </div>
          ))}
        </Panel>
      </Collapse>
      
      {/* 工具调用 */}
      <Collapse>
        <Panel header="工具调用" key="tools">
          {toolCalls.map((tool, i) => (
            <div key={i} className="tool-call">
              <strong>{tool.tool}</strong>
              <pre>{JSON.stringify(tool.parameters, null, 2)}</pre>
            </div>
          ))}
        </Panel>
      </Collapse>
    </div>
  );
}
```

---

## 方案二：系统级观察（.NET Aspire可视化）

### 1. 核心原理

这是微软官方目前力推的观察方式。MAF内置了对OpenTelemetry的支持，通过配置.NET Aspire Dashboard，你可以得到一个Web仪表盘。

### 2. 安装.NET Aspire

```bash
# 安装.NET Aspire工作负载
dotnet workload install aspire

# 验证安装
dotnet workload list
```

### 3. 配置OpenTelemetry

```csharp
// Program.cs
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

// 配置OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "MAFStudio", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddSource("Microsoft.Agents.*")  // MAF的追踪源
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            // 导出到Aspire Dashboard
            options.Endpoint = new Uri("http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

// 添加MAF服务
builder.Services.AddAgents()
    .AddOpenAIChatClient(builder.Configuration);

var app = builder.Build();
app.Run();
```

### 4. 启动Aspire Dashboard

```bash
# 方式1：使用Aspire CLI
aspire run

# 方式2：使用Docker
docker run -d --name aspire-dashboard \
  -p 18888:18888 \
  -p 4317:18889 \
  mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

### 5. 访问Dashboard

```
http://localhost:18888
```

### 6. Dashboard功能

#### Trace（追踪）

```
┌─────────────────────────────────────────────────────┐
│  Distributed Tracing                                │
└─────────────────────────────────────────────────────┘

Trace ID: abc123-def456-ghi789
Duration: 12.5s
Status: ✓ Success

┌─ Manager (2.3s)
│  └─ Planning (1.2s)
│     └─ SelectNextAgent (0.8s)
│        └─ LLM Call (0.6s)
│           └─ Prompt: "你是一个协调者..."
│           └─ Response: "请架构师发言"
│
├─ Architect (4.5s)
│  └─ Design (3.2s)
│     └─ Tool: DrawDiagram (1.5s)
│        └─ Input: {"type": "flowchart"}
│        └─ Output: "diagram.png"
│
├─ Coder (5.7s)
│  └─ Implementation (4.3s)
│     └─ Tool: WriteCode (2.8s)
│        └─ Input: {"language": "csharp"}
│        └─ Output: "Program.cs"
│
└─ Reviewer (3.1s)
   └─ Review (2.5s)
      └─ Tool: CodeReview (1.8s)
         └─ Result: "Approved"
```

**甘特图视图**：

```
Time:  0s    2s    4s    6s    8s    10s   12s
       |-----|-----|-----|-----|-----|-----|
Manager:  ████████
Architect:      █████████████████
Coder:                  █████████████████████
Reviewer:                            ███████████
```

#### Log（日志）

```
┌─────────────────────────────────────────────────────┐
│  Structured Logs                                    │
└─────────────────────────────────────────────────────┘

[14:23:45.123] INFO  Manager - Planning started
  └─ Prompt: "你是一个协调者，负责引导讨论..."
  
[14:23:45.856] INFO  Manager - Selected next agent
  └─ Agent: Architect
  └─ Reason: "需要设计架构方案"
  
[14:23:46.234] INFO  Architect - Processing request
  └─ Prompt: "你是一个架构师，负责设计系统架构..."
  └─ Model: gpt-4o
  └─ Tokens: 1,234
  
[14:23:48.567] INFO  Architect - Tool invoked
  └─ Tool: DrawDiagram
  └─ Parameters: {"type": "flowchart"}
  └─ Duration: 1.5s
  
[14:23:50.123] INFO  Architect - Response completed
  └─ Content: "架构图已生成..."
  └─ Total Tokens: 2,456
```

**优势**：
- ✅ 能看到像甘特图一样的流程
- ✅ 所有的Prompt和大模型返回的原始JSON都会被记录
- ✅ 方便分析为什么Agent会"胡言乱语"
- ✅ 性能分析：每个步骤的耗时
- ✅ 错误追踪：快速定位问题

---

## 方案三：中间件观察者

### 1. 核心原理

实现`IAgentMiddleware`接口，在Agent发言前后拦截，适合做敏感词过滤、自动记录到数据库等。

### 2. 完整实现

```csharp
/// <summary>
/// 自定义观察者中间件
/// </summary>
public class ObservabilityMiddleware : IAgentMiddleware
{
    private readonly ILogger<ObservabilityMiddleware> _logger;
    private readonly IMessageRepository _messageRepository;
    private readonly IHubContext<WorkflowHub> _hubContext;
    private readonly IMetricsCollector _metrics;
    private readonly ISensitiveWordFilter _filter;
    
    public ObservabilityMiddleware(
        ILogger<ObservabilityMiddleware> logger,
        IMessageRepository messageRepository,
        IHubContext<WorkflowHub> hubContext,
        IMetricsCollector metrics,
        ISensitiveWordFilter filter)
    {
        _logger = logger;
        _messageRepository = messageRepository;
        _hubContext = hubContext;
        _metrics = metrics;
        _filter = filter;
    }
    
    public async Task InvokeAsync(AgentContext context, NextDelegate next)
    {
        var agentName = context.Agent.Name;
        var collaborationId = context.GetCollaborationId();
        
        // ========== 发言前拦截 ==========
        
        // 1. 记录准备发言
        _logger.LogInformation(
            "[Collaboration {Id}] {Agent} 准备发言...",
            collaborationId, agentName);
        
        // 2. 性能监控开始
        var stopwatch = Stopwatch.StartNew();
        
        // 3. 推送到前端（状态：正在思考）
        await _hubContext.Clients
            .Group($"collaboration-{collaborationId}")
            .SendAsync("AgentStatusChanged", new
            {
                agent = agentName,
                status = "thinking",
                timestamp = DateTime.UtcNow
            });
        
        // ========== 执行Agent逻辑 ==========
        
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // 错误处理
            _logger.LogError(ex,
                "[Collaboration {Id}] {Agent} 发言失败",
                collaborationId, agentName);
            
            // 推送错误状态
            await _hubContext.Clients
                .Group($"collaboration-{collaborationId}")
                .SendAsync("AgentError", new
                {
                    agent = agentName,
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            
            throw;
        }
        
        // ========== 发言后拦截 ==========
        
        stopwatch.Stop();
        
        // 1. 获取发言内容
        var lastMessage = context.Messages.LastOrDefault();
        if (lastMessage != null)
        {
            var content = lastMessage.Content;
            
            // 2. 敏感词过滤
            if (_filter.ContainsSensitiveWord(content))
            {
                _logger.LogWarning(
                    "[Collaboration {Id}] {Agent} 发言包含敏感词，已过滤",
                    collaborationId, agentName);
                
                content = _filter.Filter(content);
                lastMessage.Content = content;
            }
            
            // 3. 持久化到数据库
            await _messageRepository.CreateAsync(new Message
            {
                CollaborationId = collaborationId,
                AgentName = agentName,
                Content = content,
                Timestamp = DateTime.UtcNow,
                Duration = stopwatch.ElapsedMilliseconds
            });
            
            // 4. 实时推送到前端
            await _hubContext.Clients
                .Group($"collaboration-{collaborationId}")
                .SendAsync("ReceiveMessage", new
                {
                    agent = agentName,
                    content = content,
                    duration = stopwatch.ElapsedMilliseconds,
                    timestamp = DateTime.UtcNow
                });
        }
        
        // 5. 性能统计
        _metrics.RecordAgentResponse(
            agentName,
            stopwatch.ElapsedMilliseconds);
        
        // 6. 记录完成
        _logger.LogInformation(
            "[Collaboration {Id}] {Agent} 发言完成，耗时 {Duration}ms",
            collaborationId, agentName, stopwatch.ElapsedMilliseconds);
    }
}
```

### 3. 注册中间件

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 注册服务
builder.Services.AddSingleton<IAgentMiddleware, ObservabilityMiddleware>();
builder.Services.AddSingleton<IMetricsCollector, MetricsCollector>();
builder.Services.AddSingleton<ISensitiveWordFilter, SensitiveWordFilter>();

// 配置Agent
builder.Services.AddAgents()
    .AddOpenAIChatClient(builder.Configuration)
    .UseMiddleware<ObservabilityMiddleware>();  // 注册中间件

var app = builder.Build();
app.Run();
```

### 4. 中间件链（责任链模式）

```csharp
// 多个中间件形成责任链
builder.Services.AddAgents()
    .AddOpenAIChatClient(builder.Configuration)
    .UseMiddleware<LoggingMiddleware>()           // 1. 日志记录
    .UseMiddleware<PerformanceMiddleware>()       // 2. 性能监控
    .UseMiddleware<SensitiveWordMiddleware>()     // 3. 敏感词过滤
    .UseMiddleware<PersistenceMiddleware>()       // 4. 数据持久化
    .UseMiddleware<NotificationMiddleware>();     // 5. 推送通知
```

**执行顺序**：

```
Request → Logging → Performance → SensitiveWord → Persistence → Notification → Agent
                                                                                     ↓
Response ← Logging ← Performance ← SensitiveWord ← Persistence ← Notification ← Agent
```

---

## 🏗️ 完整架构设计

### 架构图

```
┌─────────────────────────────────────────────────────┐
│              MAF观察者模式完整架构                   │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│  Subject（被观察者）                                │
│  ├─ Orchestrator（编排器）                          │
│  │  ├─ ExecuteStreamingAsync()                     │
│  │  └─ Event Stream                                │
│  └─ Agent（智能体）                                 │
│     └─ Middleware Chain                            │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│  Observer（观察者）                                 │
│  ┌───────────────────────────────────────────────┐ │
│  │  方案一：代码级观察                            │ │
│  │  ├─ ExecuteStreamingAsync()                   │ │
│  │  ├─ AgentResponseUpdateEvent                  │ │
│  │  ├─ OrchestratorThoughtEvent                  │ │
│  │  └─ ToolCallEvent                             │ │
│  └───────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────┐ │
│  │  方案二：系统级观察                            │ │
│  │  ├─ OpenTelemetry                             │ │
│  │  ├─ .NET Aspire Dashboard                     │ │
│  │  ├─ Distributed Tracing                       │ │
│  │  └─ Structured Logs                           │ │
│  └───────────────────────────────────────────────┘ │
│  ┌───────────────────────────────────────────────┐ │
│  │  方案三：中间件观察                            │ │
│  │  ├─ IAgentMiddleware                          │ │
│  │  ├─ 发言前拦截                                │ │
│  │  └─ 发言后拦截                                │ │
│  └───────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│  输出                                               │
│  ├─ 控制台日志                                      │
│  ├─ 数据库持久化                                    │
│  ├─ SignalR实时推送                                 │
│  ├─ Aspire Dashboard                                │
│  └─ 自定义UI                                        │
└─────────────────────────────────────────────────────┘
```

---

## 📊 方案选择指南

### 1. 根据场景选择

```
┌──────────────────┬───────────────────────────────┐
│ 场景             │ 推荐方案                       │
├──────────────────┼───────────────────────────────┤
│ 自定义UI         │ 方案一（ExecuteStreamingAsync）│
│ 实时监控         │ 方案一 + SignalR              │
│ 调试             │ 方案二（Aspire Dashboard）    │
│ 性能分析         │ 方案二（分布式追踪）          │
│ 敏感词过滤       │ 方案三（中间件）              │
│ 数据持久化       │ 方案三（中间件）              │
│ 完整解决方案      │ 方案一 + 二 + 三              │
└──────────────────┴───────────────────────────────┘
```

### 2. 组合使用

```csharp
// 最佳实践：三种方案组合使用
public class CompleteObservabilityService
{
    public async Task ExecuteAsync(long collaborationId, string request)
    {
        // 1. 配置OpenTelemetry（方案二）
        // 已在Program.cs中配置
        
        // 2. 使用中间件（方案三）
        // 已在DI中注册
        
        // 3. 使用流式执行（方案一）
        var responseStream = orchestrator.ExecuteStreamingAsync(request);
        
        await foreach (var message in responseStream)
        {
            // 实时处理事件
            await HandleEventAsync(collaborationId, message);
        }
    }
}
```

---

## ✅ 核心优势

### 1. **实时性**
- ✅ 流式事件实时推送
- ✅ 无需轮询
- ✅ 低延迟

### 2. **解耦**
- ✅ 观察者不影响执行
- ✅ 可动态添加/移除
- ✅ 支持多个观察者

### 3. **可视化**
- ✅ Aspire Dashboard分布式追踪
- ✅ 甘特图流程展示
- ✅ 结构化日志

### 4. **灵活性**
- ✅ 中间件可自定义
- ✅ 支持拦截和增强
- ✅ 责任链模式

---

## 🚀 实施建议

### 1. **分层观察**

```
Layer 1: ExecuteStreamingAsync（核心层）
  └─ 实时事件流

Layer 2: Middleware（中间层）
  └─ 拦截和增强

Layer 3: SignalR（实时层）
  └─ 前端推送

Layer 4: OpenTelemetry（追踪层）
  └─ 分布式追踪

Layer 5: Aspire Dashboard（可视化层）
  └─ Web仪表盘
```

### 2. **性能优化**

```csharp
// 异步处理事件，避免阻塞
await foreach (var message in responseStream)
{
    _ = Task.Run(async () =>
    {
        await HandleEventAsync(message);
    });
}
```

### 3. **错误隔离**

```csharp
try
{
    await HandleEventAsync(message);
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
4. [观察者模式（Observer Pattern）](https://en.wikipedia.org/wiki/Observer_pattern)

---

## 🎯 总结

MAF观察者模式的三种实现方案：

1. ✅ **方案一（代码级）**：ExecuteStreamingAsync + 事件流
   - 适合自定义UI、实时监控
   - 实时捕获内部对话

2. ✅ **方案二（系统级）**：.NET Aspire + OpenTelemetry
   - 适合调试、性能分析
   - 分布式追踪、可视化

3. ✅ **方案三（中间件级）**：IAgentMiddleware
   - 适合拦截、增强、过滤
   - 发言前后拦截

**三种方案可以组合使用，形成完整的可观测性解决方案！** 🎯
