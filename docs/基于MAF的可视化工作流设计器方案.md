# 基于MAF的可视化工作流设计器方案

## 🎯 核心理念

**不要重复造轮子！使用MAF框架提供的功能！**

## 📊 MAF框架已经提供的功能

### 1. 工作流模式

✅ **顺序编排** - `WorkflowMode.Sequential`
```csharp
var workflow = AgentWorkflowBuilder
    .Create()
    .AddAgent(agent1)
    .AddAgent(agent2)
    .AddAgent(agent3)
    .WithMode(WorkflowMode.Sequential)
    .Build();
```

✅ **并发编排** - `WorkflowMode.Concurrent`
```csharp
var workflow = AgentWorkflowBuilder
    .Create()
    .AddAgent(agent1)
    .AddAgent(agent2)
    .AddAgent(agent3)
    .WithMode(WorkflowMode.Concurrent)
    .Build();
```

✅ **移交编排** - `AddHandoffs()`
```csharp
var primaryAgent = new AIAgent(client, "Primary", "Main agent");
var supportAgent = new AIAgent(client, "Support", "Support agent");

primaryAgent.AddHandoffs(new[] { supportAgent });
```

✅ **群聊编排** - `AgentGroupChat`
```csharp
var groupChat = new AgentGroupChat(
    agents: new[] { agent1, agent2, agent3 }
);
```

### 2. 管理者模式

✅ **一个Agent管理多个Agent**
```csharp
var managerAgent = new AIAgent(client, "Manager", "Coordinate tasks");
var workerAgents = new[] { worker1, worker2, worker3 };

// Manager会自动分配任务给workers
```

---

## 🎨 我们需要做的

### 1. 工作流配置表（轻量级）

**只存储配置，不存储执行逻辑！**

```sql
CREATE TABLE workflow_configs (
    id BIGSERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT,
    user_id BIGINT NOT NULL,
    workflow_mode TEXT NOT NULL,  -- 'sequential', 'concurrent', 'handoffs', 'groupchat'
    agent_ids TEXT NOT NULL,       -- JSON数组: [1, 2, 3]
    config TEXT,                   -- JSON配置: { "maxIterations": 10, "stopKeywords": ["DONE"] }
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

### 2. 工作流执行记录表

```sql
CREATE TABLE workflow_executions (
    id BIGSERIAL PRIMARY KEY,
    workflow_config_id BIGINT NOT NULL,
    input TEXT,
    output TEXT,
    status INTEGER NOT NULL DEFAULT 0,
    error_message TEXT,
    started_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP
);
```

---

## 🔧 后端实现

### 1. 工作流配置服务

```csharp
public class WorkflowConfigService
{
    private readonly AgentFactoryService _agentFactory;
    
    public async Task<WorkflowResult> ExecuteWorkflowAsync(
        long configId,
        string input)
    {
        var config = await _configRepository.GetByIdAsync(configId);
        
        // 使用MAF的AgentWorkflowBuilder
        var builder = AgentWorkflowBuilder.Create();
        
        // 添加Agent
        foreach (var agentId in config.AgentIds)
        {
            var agent = await _agentFactory.CreateAgentAsync(agentId);
            builder.AddAgent(agent);
        }
        
        // 设置工作流模式
        var workflow = config.WorkflowMode switch
        {
            "sequential" => builder.WithMode(WorkflowMode.Sequential).Build(),
            "concurrent" => builder.WithMode(WorkflowMode.Concurrent).Build(),
            "handoffs" => builder.WithMode(WorkflowMode.Handoffs).Build(),
            "groupchat" => builder.WithMode(WorkflowMode.GroupChat).Build(),
            _ => throw new NotSupportedException($"不支持的工作流模式: {config.WorkflowMode}")
        };
        
        // 执行工作流
        var result = await workflow.RunAsync(input);
        
        return new WorkflowResult
        {
            Success = true,
            Output = result
        };
    }
}
```

---

## 🎨 前端界面

### 1. 工作流配置界面

```
┌─────────────────────────────────────────┐
│  创建工作流                              │
├─────────────────────────────────────────┤
│  工作流名称: [需求分析流水线]            │
│  描述: [产品需求 → 架构设计 → 开发实现]  │
│                                          │
│  工作流模式:                             │
│  ● 顺序执行  ○ 并发执行                  │
│  ○ 任务移交  ○ 群聊协作                  │
│                                          │
│  Agent列表:                              │
│  [✓] Agent1 - 产品经理                  │
│  [✓] Agent2 - 架构师                    │
│  [✓] Agent3 - 开发工程师                │
│  [ ] Agent4 - 测试工程师                │
│                                          │
│  高级配置:                               │
│  最大迭代次数: [10]                      │
│  停止关键词: [DONE, 完成]                │
│                                          │
│  [保存配置]  [立即执行]                  │
└─────────────────────────────────────────┘
```

### 2. 可视化展示

```
┌─────────────────────────────────────────┐
│  工作流: 需求分析流水线                  │
├─────────────────────────────────────────┤
│                                          │
│  ┌──────────┐                            │
│  │ 产品经理 │                            │
│  │ Agent1   │                            │
│  └────┬─────┘                            │
│       ↓                                  │
│  ┌──────────┐                            │
│  │ 架构师   │                            │
│  │ Agent2   │                            │
│  └────┬─────┘                            │
│       ↓                                  │
│  ┌──────────┐                            │
│  │ 开发工程师│                           │
│  │ Agent3   │                            │
│  └──────────┘                            │
│                                          │
│  模式: 顺序执行                          │
│  状态: ✅ 已配置                         │
│                                          │
│  [执行工作流]                            │
└─────────────────────────────────────────┘
```

---

## 📊 对比

| 特性 | 之前的错误方案 | 正确的MAF方案 |
|------|---------------|--------------|
| **工作流引擎** | ❌ 自己实现 | ✅ 使用MAF的AgentWorkflowBuilder |
| **执行逻辑** | ❌ 自己编写 | ✅ 使用MAF的WorkflowMode |
| **Agent管理** | ❌ 自己管理 | ✅ 使用MAF的Agent管理 |
| **消息传递** | ❌ 自己实现 | ✅ 使用MAF的ChatMessage |
| **代码量** | ❌ 大量代码 | ✅ 极少代码 |
| **维护成本** | ❌ 高 | ✅ 低 |
| **可靠性** | ❌ 需要自己测试 | ✅ MAF框架保证 |

---

## 🚀 实施步骤

### 第一步：创建配置表

```sql
-- 只需要两个简单的表
CREATE TABLE workflow_configs (...);
CREATE TABLE workflow_executions (...);
```

### 第二步：创建配置服务

```csharp
// 使用MAF的AgentWorkflowBuilder
public class WorkflowConfigService
{
    public async Task<WorkflowResult> ExecuteWorkflowAsync(...)
    {
        var workflow = AgentWorkflowBuilder
            .Create()
            .WithMode(WorkflowMode.Sequential)
            .Build();
        
        return await workflow.RunAsync(input);
    }
}
```

### 第三步：创建前端界面

```typescript
// 简单的配置界面
<WorkflowConfigForm>
  <Input label="工作流名称" />
  <Select label="工作流模式" options={['sequential', 'concurrent', 'handoffs', 'groupchat']} />
  <AgentSelector label="Agent列表" />
</WorkflowConfigForm>
```

---

## ✅ 总结

### 核心原则

1. **不要重复造轮子** - MAF已经提供了完整的工作流功能
2. **只做配置层** - 我们只需要存储用户的配置
3. **使用MAF执行** - 所有执行逻辑都交给MAF
4. **可视化界面** - 提供友好的配置界面

### 关键优势

1. ✅ **代码量少** - 只需要配置服务和前端界面
2. ✅ **维护简单** - MAF框架保证质量
3. ✅ **功能强大** - MAF提供了完整的工作流功能
4. ✅ **易于扩展** - 可以随时添加新的工作流模式

---

## 📝 下一步

1. 创建简单的配置表
2. 创建基于MAF的配置服务
3. 创建前端配置界面
4. 测试工作流执行

就这么简单！🎉
