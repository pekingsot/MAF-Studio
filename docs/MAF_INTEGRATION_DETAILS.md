# MAF框架集成与四种协作工作流详解

## 🎯 MAF框架核心集成

### 1. MAF框架是什么？

**Microsoft Agent Framework (MAF)** 是微软提供的多Agent协作框架，它提供了：

- ✅ **标准化的Agent接口** (`IChatClient`)
- ✅ **内置的工作流模式** (Sequential, Concurrent, Handoffs, GroupChat)
- ✅ **消息传递机制** (ChatMessage, ChatResponse)
- ✅ **流式响应支持** (Streaming)
- ✅ **工具调用能力** (Tools/Functions)

### 2. 我们的集成方式

#### 2.1 安装MAF NuGet包

```xml
<!-- MAFStudio.Application.csproj -->
<PackageReference Include="Microsoft.Extensions.AI.Abstractions" Version="10.0.3" />
```

#### 2.2 实现MAF的核心接口

**IChatClient接口** - 所有Agent都必须实现这个接口：

```csharp
// MAF标准接口
public interface IChatClient
{
    Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default);
    
    IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default);
}
```

#### 2.3 自定义ChatClient实现

为了支持通义千问、DeepSeek等非原生支持的Provider，我们创建了自定义ChatClient：

```csharp
// CustomOpenAICompatibleChatClient.cs
public class CustomOpenAICompatibleChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _endpoint;
    private readonly string _modelId;

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        // 将MAF的ChatMessage转换为OpenAI兼容格式
        var openAiMessages = messages.Select(m => new
        {
            role = m.Role.ToString().ToLower(),
            content = m.Text
        }).ToList();

        // 调用OpenAI兼容API
        var response = await CallOpenAICompatibleApi(openAiMessages);
        
        // 将响应转换回MAF格式
        return new ChatResponse
        {
            Messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.Assistant, response)
            }
        };
    }
}
```

**关键点**：
- ✅ 实现了MAF的标准接口
- ✅ 支持任何OpenAI兼容的API
- ✅ 可以无缝集成到MAF工作流中

## 🔄 四种协作工作流详解

### 工作流1：顺序工作流（Sequential）

#### 📋 概念
多个Agent按顺序依次执行，前一个Agent的输出作为后一个Agent的输入。

#### 🔧 实现代码

```csharp
// CollaborationWorkflowService.cs
public async Task<CollaborationResult> ExecuteSequentialAsync(
    long collaborationId, 
    string input, 
    CancellationToken cancellationToken = default)
{
    // 1. 获取协作中的所有Agent
    var agents = await GetAgentsAsync(collaborationId);
    
    // 2. 初始化消息列表和当前输入
    var messages = new List<ChatMessageDto>();
    var currentInput = input;

    // 3. 按顺序执行每个Agent
    foreach (var agent in agents)
    {
        // 调用MAF的GetResponseAsync方法
        var response = await agent.GetResponseAsync(
            new[] { new ChatMessage(ChatRole.User, currentInput) },
            cancellationToken: cancellationToken);

        // 提取响应内容
        var content = response.Messages.LastOrDefault()?.Text ?? string.Empty;
        
        // 记录消息
        messages.Add(new ChatMessageDto
        {
            Sender = "Agent",
            Content = content,
            Timestamp = DateTime.UtcNow
        });

        // 将当前输出作为下一个Agent的输入
        currentInput = content;
    }

    // 4. 返回最终结果
    return new CollaborationResult
    {
        Success = true,
        Output = currentInput,
        Messages = messages
    };
}
```

#### 📊 执行流程图

```
用户输入
   ↓
Agent1处理 → 输出1
   ↓
Agent2处理(输入=输出1) → 输出2
   ↓
Agent3处理(输入=输出2) → 输出3
   ↓
最终结果
```

#### 💡 实际应用场景

**场景：产品开发流水线**

```
产品需求文档
   ↓
[产品经理Agent] 分析需求 → 需求分析报告
   ↓
[架构师Agent] 设计架构 → 架构设计文档
   ↓
[开发工程师Agent] 编写代码 → 代码实现
   ↓
[测试工程师Agent] 测试验证 → 测试报告
```

**前端调用**：
```typescript
const result = await collaborationWorkflowService.executeSequential(
  collaborationId,
  "开发用户管理模块"
);
```

### 工作流2：并发工作流（Concurrent）

#### 📋 概念
多个Agent同时执行相同任务，最后合并所有结果。

#### 🔧 实现代码

```csharp
public async Task<CollaborationResult> ExecuteConcurrentAsync(
    long collaborationId, 
    string input, 
    CancellationToken cancellationToken = default)
{
    // 1. 获取所有Agent
    var agents = await GetAgentsAsync(collaborationId);
    
    // 2. 创建并发任务列表
    var tasks = agents.Select(async agent =>
    {
        // 每个Agent独立处理相同的输入
        var response = await agent.GetResponseAsync(
            new[] { new ChatMessage(ChatRole.User, input) },
            cancellationToken: cancellationToken);

        return new ChatMessageDto
        {
            Sender = "Agent",
            Content = response.Messages.LastOrDefault()?.Text ?? string.Empty,
            Timestamp = DateTime.UtcNow
        };
    });

    // 3. 并发执行所有任务
    var messages = (await Task.WhenAll(tasks)).ToList();

    // 4. 合并所有结果
    var combinedOutput = string.Join("\n\n---\n\n", 
        messages.Select(m => m.Content));

    return new CollaborationResult
    {
        Success = true,
        Output = combinedOutput,
        Messages = messages
    };
}
```

#### 📊 执行流程图

```
           用户输入
          ↙    ↓    ↘
    Agent1   Agent2   Agent3
      ↓        ↓        ↓
    输出1    输出2    输出3
      ↓        ↓        ↓
        合并所有输出
           ↓
        最终结果
```

#### 💡 实际应用场景

**场景：代码多角度审查**

```
           代码文件
          ↙    ↓    ↘
[安全审查] [性能审查] [风格审查]
    ↓         ↓         ↓
安全报告   性能报告   风格报告
    ↓         ↓         ↓
        综合审查报告
```

**前端调用**：
```typescript
const result = await collaborationWorkflowService.executeConcurrent(
  collaborationId,
  "审查这段代码的安全性和性能"
);
```

### 工作流3：任务移交工作流（Handoffs）

#### 📋 概念
Agent之间可以相互移交任务，形成动态协作链。

#### 🔧 实现代码

```csharp
public async Task<CollaborationResult> ExecuteHandoffsAsync(
    long collaborationId, 
    string input, 
    CancellationToken cancellationToken = default)
{
    var agents = await GetAgentsAsync(collaborationId);
    var messages = new List<ChatMessageDto>();
    var currentInput = input;
    var currentAgentIndex = 0;
    var maxIterations = 10; // 防止无限循环

    for (int i = 0; i < maxIterations; i++)
    {
        var agent = agents[currentAgentIndex];
        
        // 添加移交提示
        var prompt = $@"
{currentInput}

如果你认为任务已完成，请在回复中包含[COMPLETED]。
如果你需要移交给其他Agent，请在回复中包含[HANDOFF:Agent名称]。

可用的Agent列表：
{string.Join("\n", agents.Select((a, idx) => $"{idx}. Agent{idx}"))}";

        var response = await agent.GetResponseAsync(
            new[] { new ChatMessage(ChatRole.User, prompt) },
            cancellationToken: cancellationToken);

        var content = response.Messages.LastOrDefault()?.Text ?? string.Empty;
        
        messages.Add(new ChatMessageDto
        {
            Sender = $"Agent{currentAgentIndex}",
            Content = content,
            Timestamp = DateTime.UtcNow
        });

        // 检查是否完成
        if (content.Contains("[COMPLETED]"))
        {
            break;
        }

        // 检查是否需要移交
        var handoffMatch = Regex.Match(content, @"\[HANDOFF:Agent(\d+)\]");
        if (handoffMatch.Success)
        {
            currentAgentIndex = int.Parse(handoffMatch.Groups[1].Value);
            currentInput = content.Replace(handoffMatch.Value, "").Trim();
        }
        else
        {
            // 默认移交给下一个Agent
            currentAgentIndex = (currentAgentIndex + 1) % agents.Count;
            currentInput = content;
        }
    }

    return new CollaborationResult
    {
        Success = true,
        Output = messages.LastOrDefault()?.Content ?? string.Empty,
        Messages = messages
    };
}
```

#### 📊 执行流程图

```
用户输入
   ↓
Agent1处理
   ↓
判断：需要修改？
   ↓ 是
移交给Agent2
   ↓
Agent2处理
   ↓
判断：是否满意？
   ↓ 否
移交给Agent1
   ↓
Agent1修改
   ↓
判断：是否完成？
   ↓ 是
最终结果
```

#### 💡 实际应用场景

**场景：文档编写与审核**

```
编写需求文档
   ↓
[文档编写员] 编写初稿
   ↓
[审核员] 审核 → 不满意，打回修改
   ↓
[文档编写员] 修改文档
   ↓
[审核员] 审核 → 满意，标记完成
   ↓
最终文档
```

**前端调用**：
```typescript
const result = await collaborationWorkflowService.executeHandoffs(
  collaborationId,
  "编写API接口文档"
);
```

### 工作流4：群聊工作流（Group Chat）

#### 📋 概念
多个Agent进行群聊讨论，共同解决问题。

#### 🔧 实现代码

```csharp
public async IAsyncEnumerable<ChatMessageDto> ExecuteGroupChatStreamAsync(
    long collaborationId, 
    string input, 
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var agents = await GetAgentsAsync(collaborationId);
    var conversationHistory = new List<ChatMessage>
    {
        new ChatMessage(ChatRole.User, input)
    };

    // 群聊轮次
    for (int round = 0; round < 5; round++)
    {
        // 每个Agent轮流发言
        foreach (var agent in agents)
        {
            // 流式调用
            await foreach (var update in agent.GetStreamingResponseAsync(
                conversationHistory, 
                cancellationToken: cancellationToken))
            {
                var message = new ChatMessageDto
                {
                    Sender = "Agent",
                    Content = update.Text ?? string.Empty,
                    Timestamp = DateTime.UtcNow
                };

                yield return message;
            }

            // 将Agent的回复添加到历史
            conversationHistory.Add(
                new ChatMessage(ChatRole.Assistant, "Agent回复内容"));
        }
    }
}
```

#### 📊 执行流程图

```
用户提出问题
   ↓
Agent1发言 → Agent2看到发言 → Agent2回复
   ↓                           ↓
Agent3看到讨论 → Agent3发表意见
   ↓
Agent1回应 → Agent2补充 → Agent3总结
   ↓
讨论结束
```

#### 💡 实际应用场景

**场景：技术方案讨论**

```
提出问题：如何设计高并发系统？
   ↓
[架构师] 建议使用微服务架构
   ↓
[DBA] 提出数据库分库分表方案
   ↓
[运维工程师] 建议容器化部署
   ↓
[架构师] 综合意见，形成方案
   ↓
最终方案
```

**前端调用**：
```typescript
// 使用流式响应
await collaborationWorkflowService.executeGroupChat(
  collaborationId,
  "讨论如何设计高并发系统"
);
```

## 🎨 前端界面体现

### 1. 工作流选择界面

```typescript
// WorkflowExecutor.tsx
<Select value={workflowType} onChange={setWorkflowType}>
  <Option value="sequential">
    <PlayCircleOutlined /> 顺序执行
  </Option>
  <Option value="concurrent">
    <ThunderboltOutlined /> 并发执行
  </Option>
  <Option value="handoffs">
    <SwapOutlined /> 任务移交
  </Option>
  <Option value="groupchat">
    <TeamOutlined /> 群聊协作
  </Option>
</Select>
```

### 2. 执行过程展示

```typescript
// 显示每个Agent的执行过程
<List
  dataSource={result.messages}
  renderItem={(msg) => (
    <List.Item>
      <Tag color="blue">{msg.sender}</Tag>
      <Text>{msg.content}</Text>
      <Text type="secondary">
        {new Date(msg.timestamp).toLocaleString()}
      </Text>
    </List.Item>
  )}
/>
```

## 📊 MAF集成的关键优势

### 1. 标准化接口

| 特性 | 说明 | 优势 |
|------|------|------|
| IChatClient | 统一的Agent接口 | 所有Agent遵循相同标准 |
| ChatMessage | 标准消息格式 | 跨Agent通信无障碍 |
| ChatResponse | 统一响应格式 | 结果处理一致 |

### 2. 工作流模式

| 工作流 | MAF支持 | 我们的实现 | 应用场景 |
|--------|---------|-----------|---------|
| Sequential | ✅ | ✅ | 流水线任务 |
| Concurrent | ✅ | ✅ | 并行处理 |
| Handoffs | ✅ | ✅ | 迭代优化 |
| GroupChat | ✅ | ✅ | 团队讨论 |

### 3. 扩展性

```csharp
// 轻松添加新的Provider
public class CustomProviderChatClient : IChatClient
{
    public async Task<ChatResponse> GetResponseAsync(...)
    {
        // 实现自定义逻辑
    }
}

// 注册到Agent工厂
services.AddScoped<IAgentFactoryService, AgentFactoryService>();
```

## 🔍 如何验证MAF集成

### 1. 检查NuGet包

```bash
cd backend/src/MAFStudio.Application
dotnet list package
```

应该看到：
```
Microsoft.Extensions.AI.Abstractions    10.0.3
```

### 2. 检查接口实现

```csharp
// 所有Agent都实现了IChatClient
public class CustomOpenAICompatibleChatClient : IChatClient
{
    // 实现MAF标准方法
}
```

### 3. 检查工作流调用

```csharp
// 使用MAF的标准方法调用Agent
var response = await agent.GetResponseAsync(
    new[] { new ChatMessage(ChatRole.User, input) },
    cancellationToken: cancellationToken);
```

### 4. 运行单元测试

```bash
cd backend/tests/MAFStudio.Application.Tests
dotnet test
```

所有测试应该通过，验证MAF集成的正确性。

## 📝 总结

### MAF集成的体现

1. ✅ **接口标准化**：所有Agent实现`IChatClient`接口
2. ✅ **消息标准化**：使用`ChatMessage`和`ChatResponse`
3. ✅ **工作流模式**：实现了MAF的四种标准工作流
4. ✅ **流式支持**：支持`GetStreamingResponseAsync`
5. ✅ **扩展性**：可以轻松添加新的Provider

### 四种工作流的体现

1. ✅ **顺序工作流**：流水线式处理，前一个输出作为后一个输入
2. ✅ **并发工作流**：多个Agent同时处理，合并结果
3. ✅ **任务移交**：Agent间动态移交任务，迭代优化
4. ✅ **群聊协作**：多Agent群聊讨论，共同解决问题

所有工作流都基于MAF框架的标准接口实现，确保了代码的规范性和可维护性！
