# MAF工作流架构设计文档

## 📋 概述

本文档基于 **Microsoft Agent Framework (MAF) v1.0** 官方最佳实践，设计简化后的工作流架构。我们将只保留两种核心工作流类型：

1. **群聊协作（GroupChat）**
2. **Magentic智能工作流（Magentic）**

## 🎯 设计原则

### ✅ 遵循MAF最佳实践

1. **使用官方API**：直接使用MAF提供的`GroupChatOrchestration`和`MagenticOrchestration`
2. **避免过度封装**：不自行实现Agent基类或编排逻辑
3. **类型安全**：利用C#强类型特性定义参数
4. **Runtime管理**：使用`InProcessRuntime`管理执行生命周期

### ❌ 避免的误区

1. 不将执行任务的Agent设置为协调者
2. 不自行实现工作流引擎
3. 不绕过MAF的Runtime机制

---

## 🏗️ 架构设计

### 1. 群聊协作（GroupChat）

#### 1.1 使用场景

- **固定流程的协作**：如"文案撰写 → 审核 → 修改"的循环
- **需要人工干预**：可以在对话中引入人类参与者
- **明确的发言顺序**：轮询、协调者引导、智能选择

#### 1.2 MAF官方实现

```csharp
// 1. 创建Agents
ChatCompletionAgent writer = new ChatCompletionAgent {
    Name = "CopyWriter",
    Description = "A copy writer",
    Instructions = "You are a copywriter...",
    Kernel = kernel,
};

ChatCompletionAgent editor = new ChatCompletionAgent {
    Name = "Reviewer",
    Description = "An editor.",
    Instructions = "You are an art director...",
    Kernel = kernel,
};

// 2. 创建GroupChatManager（三种模式）
// 模式1：轮询模式
RoundRobinGroupChatManager roundRobinManager = new RoundRobinGroupChatManager 
{ 
    MaximumInvocationCount = 5 
};

// 模式2：协调者模式（需要指定Manager）
ManagerGroupChatManager managerMode = new ManagerGroupChatManager(managerAgent);

// 模式3：智能模式
IntelligentGroupChatManager intelligentManager = new IntelligentGroupChatManager();

// 3. 创建Orchestration
GroupChatOrchestration orchestration = new GroupChatOrchestration(
    manager,  // GroupChatManager实例
    writer,
    editor)
{
    ResponseCallback = responseCallback,
};

// 4. 使用Runtime执行
InProcessRuntime runtime = new InProcessRuntime();
await runtime.StartAsync();

var result = await orchestration.InvokeAsync("Create a slogan...", runtime);
string output = await result.GetValueAsync(TimeSpan.FromSeconds(60));

await runtime.RunUntilIdleAsync();
```

#### 1.3 三种协调模式对比

| 模式 | Manager类型 | 适用场景 | 是否需要指定Manager Agent |
|------|------------|---------|-------------------------|
| **轮询模式** | `RoundRobinGroupChatManager` | 所有Agent轮流发言 | ❌ 不需要 |
| **协调者模式** | `ManagerGroupChatManager` | Manager引导Worker发言 | ✅ 需要 |
| **智能模式** | `IntelligentGroupChatManager` | AI智能选择发言者 | ❌ 不需要 |

---

### 2. Magentic智能工作流（Magentic）

#### 2.1 使用场景

- **不确定的任务路径**：需要Agent自主决策"下一步找谁"
- **复杂开放式任务**：如"分析数据 → 搜索资料 → 编写代码"的动态组合
- **需要反思和调整**：Manager根据进度动态调整计划

#### 2.2 MAF官方实现

```csharp
// 1. 创建Worker Agents
ChatCompletionAgent researchAgent = new ChatCompletionAgent {
    Name = "ResearchAgent",
    Description = "A helpful assistant with access to web search...",
    Instructions = "You are a Researcher...",
    Kernel = researchKernel,
};

AzureAIAgent coderAgent = new AzureAIAgent(definition, agentsClient);

// 2. 创建Magentic Manager（独立的协调者）
StandardMagenticManager manager = new StandardMagenticManager(
    managerKernel.GetRequiredService<IChatCompletionService>(),
    new OpenAIPromptExecutionSettings())
{
    MaximumInvocationCount = 5,
};

// 3. 创建MagenticOrchestration
MagenticOrchestration orchestration = new MagenticOrchestration(
    manager,          // StandardMagenticManager实例
    researchAgent,    // Worker Agent 1
    coderAgent)       // Worker Agent 2
{
    ResponseCallback = responseCallback,
};

// 4. 使用Runtime执行
InProcessRuntime runtime = new InProcessRuntime();
await runtime.StartAsync();

var result = await orchestration.InvokeAsync("Analyze ML models...", runtime);
string output = await result.GetValueAsync(TimeSpan.FromSeconds(60));

await runtime.RunUntilIdleAsync();
```

#### 2.3 Magentic核心机制

**双环规划（Dual-Loop Planning）**：

1. **外环 - Task Ledger（任务账本）**：
   - 维护全局目标
   - 记录已知事实
   - 制定总体规划
   - 定义任务边界

2. **内环 - Progress Ledger（进度账本）**：
   - 当前步骤目标
   - 已完成步骤
   - 反思结果
   - 下一步行动

---

## 🔧 重构方案

### 1. 数据模型调整

#### 1.1 TaskConfig简化

```csharp
public class TaskConfig
{
    // 工作流类型：GroupChat 或 Magentic
    public WorkflowType WorkflowType { get; set; } = WorkflowType.GroupChat;
    
    // GroupChat专用配置
    public GroupChatConfig? GroupChat { get; set; }
    
    // Magentic专用配置
    public MagenticConfig? Magentic { get; set; }
}

public enum WorkflowType
{
    GroupChat,    // 群聊协作
    Magentic      // Magentic智能工作流
}

public class GroupChatConfig
{
    // 协调模式：RoundRobin, Manager, Intelligent
    public OrchestrationMode Mode { get; set; } = OrchestrationMode.RoundRobin;
    
    // Manager Agent ID（仅Manager模式需要）
    public long? ManagerAgentId { get; set; }
    
    // 最大迭代次数
    public int MaxIterations { get; set; } = 10;
}

public enum OrchestrationMode
{
    RoundRobin,    // 轮询模式
    Manager,       // 协调者模式
    Intelligent    // 智能模式
}

public class MagenticConfig
{
    // 最大迭代次数
    public int MaxIterations { get; set; } = 10;
    
    // 最大尝试次数
    public int MaxAttempts { get; set; } = 5;
    
    // 阈值标准（可选）
    public Dictionary<string, int>? Thresholds { get; set; }
}
```

#### 1.2 CollaborationAgent角色简化

```csharp
// 只保留Worker角色，不再需要Manager角色
// GroupChat的Manager模式通过GroupChatConfig.ManagerAgentId指定
// Magentic模式自动创建StandardMagenticManager

public class CollaborationAgent
{
    public long AgentId { get; set; }
    public string? CustomPrompt { get; set; }
    
    // 移除Role字段，所有成员都是Worker
    // public string? Role { get; set; }  // ❌ 删除
}
```

---

### 2. 服务层重构

#### 2.1 创建MAF适配器服务

```csharp
public interface IMafOrchestrationService
{
    /// <summary>
    /// 执行GroupChat工作流
    /// </summary>
    Task<CollaborationResult> ExecuteGroupChatAsync(
        long collaborationId,
        string input,
        GroupChatConfig config,
        long? taskId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行Magentic工作流
    /// </summary>
    Task<CollaborationResult> ExecuteMagenticAsync(
        long collaborationId,
        string input,
        MagenticConfig config,
        long? taskId = null,
        CancellationToken cancellationToken = default);
}
```

#### 2.2 使用MAF Runtime

```csharp
public class MafOrchestrationService : IMafOrchestrationService
{
    private readonly IAgentFactoryService _agentFactory;
    private readonly ILogger<MafOrchestrationService> _logger;

    public async Task<CollaborationResult> ExecuteGroupChatAsync(
        long collaborationId,
        string input,
        GroupChatConfig config,
        long? taskId = null,
        CancellationToken cancellationToken = default)
    {
        // 1. 创建Agents
        var agents = await CreateAgentsAsync(collaborationId);
        
        // 2. 创建GroupChatManager
        GroupChatManager manager = config.Mode switch
        {
            OrchestrationMode.RoundRobin => new RoundRobinGroupChatManager 
            { 
                MaximumInvocationCount = config.MaxIterations 
            },
            OrchestrationMode.Manager => await CreateManagerGroupChatManagerAsync(
                config.ManagerAgentId!.Value, 
                config.MaxIterations),
            OrchestrationMode.Intelligent => new IntelligentGroupChatManager(),
            _ => throw new ArgumentException($"Unknown mode: {config.Mode}")
        };
        
        // 3. 创建Orchestration
        var orchestration = new GroupChatOrchestration(manager, agents.ToArray())
        {
            ResponseCallback = (response) => 
            {
                _logger.LogInformation("[{Agent}] {Content}", response.AuthorName, response.Content);
                return ValueTask.CompletedTask;
            }
        };
        
        // 4. 使用Runtime执行
        using var runtime = new InProcessRuntime();
        await runtime.StartAsync();
        
        var result = await orchestration.InvokeAsync(input, runtime);
        var output = await result.GetValueAsync(TimeSpan.FromMinutes(5));
        
        await runtime.RunUntilIdleAsync();
        
        return new CollaborationResult
        {
            Success = true,
            Output = output
        };
    }

    public async Task<CollaborationResult> ExecuteMagenticAsync(
        long collaborationId,
        string input,
        MagenticConfig config,
        long? taskId = null,
        CancellationToken cancellationToken = default)
    {
        // 1. 创建Worker Agents
        var workerAgents = await CreateWorkerAgentsAsync(collaborationId);
        
        // 2. 创建StandardMagenticManager
        var managerClient = await _agentFactory.CreateManagerClientAsync();
        var manager = new StandardMagenticManager(
            managerClient,
            new OpenAIPromptExecutionSettings())
        {
            MaximumInvocationCount = config.MaxIterations
        };
        
        // 3. 创建MagenticOrchestration
        var orchestration = new MagenticOrchestration(
            manager, 
            workerAgents.ToArray())
        {
            ResponseCallback = (response) => 
            {
                _logger.LogInformation("[{Agent}] {Content}", response.AuthorName, response.Content);
                return ValueTask.CompletedTask;
            }
        };
        
        // 4. 使用Runtime执行
        using var runtime = new InProcessRuntime();
        await runtime.StartAsync();
        
        var result = await orchestration.InvokeAsync(input, runtime);
        var output = await result.GetValueAsync(TimeSpan.FromMinutes(10));
        
        await runtime.RunUntilIdleAsync();
        
        return new CollaborationResult
        {
            Success = true,
            Output = output
        };
    }
}
```

---

### 3. 前端UI调整

#### 3.1 任务创建表单简化

```tsx
// 工作流类型选择
<Form.Item label="工作流类型">
  <Radio.Group value={workflowType} onChange={(e) => setWorkflowType(e.target.value)}>
    <Radio.Button value="GroupChat">
      <TeamOutlined /> 群聊协作
    </Radio.Button>
    <Radio.Button value="Magentic">
      <BulbOutlined /> Magentic智能工作流
    </Radio.Button>
  </Radio.Group>
</Form.Item>

// GroupChat配置
{workflowType === 'GroupChat' && (
  <>
    <Form.Item label="协调模式">
      <Radio.Group value={orchestrationMode}>
        <Radio value="RoundRobin">轮询模式</Radio>
        <Radio value="Manager">协调者模式</Radio>
        <Radio value="Intelligent">智能模式</Radio>
      </Radio.Group>
    </Form.Item>
    
    {orchestrationMode === 'Manager' && (
      <Form.Item label="选择协调者" required>
        <Select placeholder="请选择Manager Agent">
          {agents.map(agent => (
            <Option key={agent.id} value={agent.id}>{agent.name}</Option>
          ))}
        </Select>
      </Form.Item>
    )}
    
    <Form.Item label="最大迭代次数">
      <InputNumber min={1} max={50} value={maxIterations} />
    </Form.Item>
  </>
)}

// Magentic配置
{workflowType === 'Magentic' && (
  <>
    <Alert
      message="Magentic智能工作流"
      description="框架将自动创建独立的MagenticManager作为协调者，所有团队成员都作为Worker参与执行。"
      type="info"
    />
    
    <Form.Item label="最大迭代次数">
      <InputNumber min={1} max={50} value={maxIterations} />
    </Form.Item>
    
    <Form.Item label="最大尝试次数">
      <InputNumber min={1} max={20} value={maxAttempts} />
    </Form.Item>
    
    <Form.Item label="阈值标准（可选）">
      <Input.TextArea 
        rows={3} 
        placeholder='{"quality": 85, "accuracy": 90}'
      />
    </Form.Item>
  </>
)}
```

#### 3.2 Agent角色简化

```tsx
// 添加Agent时，不再需要选择角色
// 所有Agent都是Worker，通过任务配置来指定Manager

<Alert
  message="所有Agent都作为Worker参与任务执行"
  description="GroupChat协调者模式会在任务配置时指定，Magentic模式会自动创建协调者。"
  type="info"
/>
```

---

## 📊 对比总结

### 架构对比

| 维度 | 旧架构 | 新架构（基于MAF） |
|------|--------|------------------|
| **工作流类型** | 多种（GroupChat, Magentic, Sequential等） | 两种（GroupChat, Magentic） |
| **协调者实现** | 自定义逻辑，将Agent设置为Manager | 使用MAF官方API（StandardMagenticManager） |
| **Runtime** | 自定义执行引擎 | 使用MAF的InProcessRuntime |
| **Agent角色** | Manager/Worker | 统一为Worker |
| **配置复杂度** | 高（多种参数） | 低（按需配置） |

### 功能对比

| 功能 | GroupChat | Magentic |
|------|-----------|----------|
| **协调者** | 可选（RoundRobin不需要） | 自动创建（StandardMagenticManager） |
| **发言顺序** | 固定（轮询/协调者引导/智能选择） | 动态（Manager自主决策） |
| **适用场景** | 固定流程协作 | 不确定路径的复杂任务 |
| **反思机制** | 无 | 双环规划（Task Ledger + Progress Ledger） |
| **人工干预** | 支持 | 不支持 |

---

## 🚀 实施步骤

### Phase 1: 数据模型调整
1. 简化`TaskConfig`，只保留`GroupChat`和`Magentic`配置
2. 移除`CollaborationAgent.Role`字段
3. 更新数据库迁移脚本

### Phase 2: 服务层重构
1. 创建`IMafOrchestrationService`接口
2. 实现`MafOrchestrationService`，使用MAF官方API
3. 删除自定义的工作流管理器代码
4. 更新`AgentFactoryService`，支持创建MAF Agent

### Phase 3: 前端UI调整
1. 简化任务创建表单
2. 移除Agent角色选择
3. 更新工作流配置UI

### Phase 4: 测试与验证
1. 单元测试：验证MAF API集成
2. 集成测试：验证完整工作流执行
3. 性能测试：对比新旧架构性能

---

## 📚 参考资料

1. [MAF官方文档 - Magentic编排](https://learn.microsoft.com/zh-cn/semantic-kernel/frameworks/agent/agent-orchestration/magentic)
2. [MAF官方文档 - GroupChat编排](https://learn.microsoft.com/zh-cn/semantic-kernel/frameworks/agent/agent-orchestration/group-chat)
3. [MAF官方文档 - 高级主题](https://learn.microsoft.com/zh-cn/semantic-kernel/frameworks/agent/agent-orchestration/advanced-topics)

---

## ✅ 预期收益

1. **代码简化**：删除大量自定义逻辑，代码量减少约40%
2. **维护性提升**：直接使用MAF官方API，跟随框架升级
3. **功能完整**：获得MAF提供的所有特性（如Durable Agents、MCP集成）
4. **性能优化**：MAF Runtime经过优化，性能更好
5. **类型安全**：强类型配置，减少运行时错误

---

## 🎯 下一步行动

请确认此设计文档后，我将开始实施：

1. ✅ 数据模型调整
2. ✅ 服务层重构（使用MAF API）
3. ✅ 前端UI简化
4. ✅ 单元测试更新
5. ✅ 集成测试验证

**请审核此设计文档，如有疑问或建议，请提出！**
