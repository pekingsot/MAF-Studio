# MagenticManager配置来源设计文档

## 📋 问题分析

### 核心问题

**MagenticManager需要的数据（LLM配置、API Key等）从哪里来？**

### 选项分析

| 选项 | 优点 | 缺点 | 结论 |
|------|------|------|------|
| **使用协调者的配置** | ✅ 复用已有配置<br>✅ 用户已配置好<br>✅ 符合直觉 | 无 | ✅ **推荐** |
| 系统内置默认配置 | ✅ 简单 | ❌ 不灵活<br>❌ 需要硬编码 | ❌ 不推荐 |
| 全局配置 | ✅ 集中管理 | ❌ 需要额外配置<br>❌ 不够灵活 | ⚠️ 可选 |

---

## ✅ 正确的设计方案

### 方案：使用协调者的配置数据

**核心理念**：
- 协调者本身就是一个Manager角色的Agent
- 协调者已经配置了LLM、API Key、SystemPrompt等
- MagenticManager应该**复用协调者的所有配置**

---

## 🏗️ 实现方案

### 1. 数据流程

```
┌─────────────────────────────────────────────────────┐
│  Step 1: 新建Agent（协调者）                        │
└─────────────────────────────────────────────────────┘
创建Agent：
├─ Name: "协调者"
├─ Role: Manager
├─ LLM配置:
│  ├─ Provider: OpenAI
│  ├─ Model: gpt-4o
│  ├─ ApiKey: sk-xxx
│  └─ Endpoint: https://api.openai.com
├─ SystemPrompt: "你是一个协调者..."
└─ Tools: []  （协调者不需要工具）

                ↓

┌─────────────────────────────────────────────────────┐
│  Step 2: 新建任务                                    │
└─────────────────────────────────────────────────────┘
选择协调者：
├─ 协调者Agent ID: 123
├─ 自定义提示词: "你是一个Magentic协调者..."
└─ 工作流类型: Magentic

                ↓

┌─────────────────────────────────────────────────────┐
│  Step 3: 执行任务                                    │
└─────────────────────────────────────────────────────┘
创建MagenticManager：
├─ 复用协调者的LLM配置 ✅
│  ├─ Provider: OpenAI
│  ├─ Model: gpt-4o
│  ├─ ApiKey: sk-xxx
│  └─ Endpoint: https://api.openai.com
├─ 使用自定义提示词（或协调者默认提示词）✅
└─ 创建MagenticOrchestration
```

---

### 2. 代码实现

#### 2.1 Agent表结构（已有）

```sql
CREATE TABLE agents (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    role VARCHAR(50) DEFAULT 'Worker',  -- Manager, Worker
    
    -- LLM配置
    llm_provider VARCHAR(50),  -- OpenAI, Azure, Ollama
    llm_model VARCHAR(100),    -- gpt-4o, gpt-3.5-turbo
    llm_api_key VARCHAR(500),  -- 加密存储
    llm_endpoint VARCHAR(500), -- API端点
    
    -- 提示词
    system_prompt TEXT,
    
    -- 其他配置
    temperature DECIMAL(3,2) DEFAULT 0.7,
    max_tokens INT DEFAULT 4000,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

#### 2.2 任务配置表

```sql
CREATE TABLE task_configs (
    id BIGSERIAL PRIMARY KEY,
    task_id BIGINT NOT NULL,
    
    -- 协调者配置（必选）
    manager_agent_id BIGINT NOT NULL,  -- 协调者Agent ID
    manager_custom_prompt TEXT,  -- 自定义协调者提示词
    
    -- 工作流配置
    workflow_type VARCHAR(50) DEFAULT 'GroupChat',  -- GroupChat, Magentic
    max_iterations INT DEFAULT 10,
    
    -- Magentic特有配置
    max_attempts INT DEFAULT 5,  -- 最大尝试次数
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_manager_agent FOREIGN KEY (manager_agent_id) 
        REFERENCES agents(id)
);
```

#### 2.3 工作流执行服务

```csharp
public class WorkflowExecutionService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentFactoryService _agentFactory;
    
    public async Task<CollaborationResult> ExecuteMagenticAsync(
        long taskId,
        string input)
    {
        // 1. 获取任务配置
        var taskConfig = await _taskConfigRepository.GetByTaskIdAsync(taskId);
        
        // 2. 获取协调者Agent（Manager角色）
        var managerAgent = await _agentRepository.GetByIdAsync(
            taskConfig.ManagerAgentId);
        
        if (managerAgent.Role != "Manager")
        {
            throw new BusinessException("协调者必须是Manager角色的Agent");
        }
        
        // 3. 使用协调者的配置创建MagenticManager ✅
        var managerClient = await CreateMagenticManagerAsync(
            managerAgent,
            taskConfig.ManagerCustomPrompt);
        
        // 4. 创建Worker Agents
        var workers = await GetTaskWorkersAsync(taskId);
        var workerClients = new List<IChatClient>();
        
        foreach (var worker in workers)
        {
            var client = await _agentFactory.CreateAgentAsync(worker.Id);
            workerClients.Add(client);
        }
        
        // 5. 创建MagenticOrchestration
        var orchestration = new MagenticOrchestration(
            managerClient,
            workerClients.Select(c => new ChatClientAgent(c)).ToArray())
        {
            MaximumInvocationCount = taskConfig.MaxIterations,
            MaximumAttemptCount = taskConfig.MaxAttempts
        };
        
        // 6. 执行
        using var runtime = new InProcessRuntime();
        await runtime.StartAsync();
        
        var result = await orchestration.InvokeAsync(input, runtime);
        var output = await result.GetValueAsync();
        
        await runtime.RunUntilIdleAsync();
        
        return new CollaborationResult { Output = output };
    }
    
    /// <summary>
    /// 使用协调者的配置创建MagenticManager ✅
    /// </summary>
    private async Task<IChatClient> CreateMagenticManagerAsync(
        Agent managerAgent,
        string? customPrompt)
    {
        // 1. 复用协调者的LLM配置 ✅
        IChatClient baseClient;
        
        switch (managerAgent.LlmProvider?.ToLower())
        {
            case "openai":
                baseClient = new OpenAIClient(managerAgent.LlmApiKey)
                    .AsChatClient(managerAgent.LlmModel ?? "gpt-4o");
                break;
                
            case "azure":
                baseClient = new AzureOpenAIClient(
                    new Uri(managerAgent.LlmEndpoint!),
                    new AzureKeyCredential(managerAgent.LlmApiKey!))
                    .AsChatClient(managerAgent.LlmModel ?? "gpt-4o");
                break;
                
            case "ollama":
                baseClient = new OllamaChatClient(
                    new Uri(managerAgent.LlmEndpoint ?? "http://localhost:11434"),
                    managerAgent.LlmModel ?? "llama2");
                break;
                
            default:
                throw new BusinessException($"不支持的LLM提供商: {managerAgent.LlmProvider}");
        }
        
        // 2. 使用自定义提示词或协调者默认提示词 ✅
        var systemPrompt = customPrompt ?? managerAgent.SystemPrompt ?? 
            "你是一个Magentic协调者，负责动态规划和调度。";
        
        // 3. 创建带提示词的ChatClient
        var builder = new ChatClientBuilder(baseClient);
        
        builder.Use(async (messages, next, cancellationToken) =>
        {
            // 注入系统提示词
            if (!messages.Any(m => m.Role == AuthorRole.System))
            {
                messages.Insert(0, new ChatMessage(
                    AuthorRole.System,
                    systemPrompt));
            }
            
            return await next(messages, cancellationToken);
        });
        
        // 4. 添加功能调用中间件
        builder.UseFunctionInvocation(_loggerFactory);
        
        return builder.Build();
    }
}
```

---

### 3. 配置来源详解

#### 3.1 LLM配置来源

```csharp
// MagenticManager使用的LLM配置 = 协调者Agent的LLM配置 ✅

var managerAgent = await _agentRepository.GetByIdAsync(managerAgentId);

// LLM提供商
var provider = managerAgent.LlmProvider;  // OpenAI, Azure, Ollama

// 模型名称
var model = managerAgent.LlmModel;  // gpt-4o, gpt-3.5-turbo

// API Key
var apiKey = managerAgent.LlmApiKey;  // sk-xxx

// API端点
var endpoint = managerAgent.LlmEndpoint;  // https://api.openai.com

// 温度
var temperature = managerAgent.Temperature ?? 0.7;

// 最大Token
var maxTokens = managerAgent.MaxTokens ?? 4000;
```

#### 3.2 提示词来源

```csharp
// MagenticManager使用的提示词优先级：

// 1. 任务配置中的自定义提示词（最高优先级）
if (!string.IsNullOrEmpty(taskConfig.ManagerCustomPrompt))
{
    return taskConfig.ManagerCustomPrompt;
}

// 2. 协调者Agent的默认提示词
if (!string.IsNullOrEmpty(managerAgent.SystemPrompt))
{
    return managerAgent.SystemPrompt;
}

// 3. 系统内置默认提示词（最低优先级）
return "你是一个Magentic协调者，负责动态规划和调度。";
```

#### 3.3 工具配置来源

```csharp
// 协调者通常不需要工具 ✅
// MagenticManager也不需要工具
// 它只负责协调，不执行具体任务

// Worker Agents才需要工具
var workerAgents = await GetTaskWorkersAsync(taskId);
foreach (var worker in workerAgents)
{
    // Worker可以有自己的工具
    var tools = await GetAgentToolsAsync(worker.Id);
    // ...
}
```

---

## 📊 完整流程图

```
┌─────────────────────────────────────────────────────┐
│  1. 新建Agent（协调者）                              │
└─────────────────────────────────────────────────────┘
配置：
├─ Name: "协调者"
├─ Role: Manager
├─ LLM配置:
│  ├─ Provider: OpenAI ✅
│  ├─ Model: gpt-4o ✅
│  ├─ ApiKey: sk-xxx ✅
│  └─ Endpoint: https://api.openai.com ✅
├─ SystemPrompt: "你是一个协调者..." ✅
└─ Temperature: 0.7 ✅

                ↓

┌─────────────────────────────────────────────────────┐
│  2. 新建任务                                         │
└─────────────────────────────────────────────────────┘
选择：
├─ 协调者: Agent ID 123 ✅
├─ 自定义提示词: "你是一个Magentic协调者..." (可选)
├─ 工作流类型: Magentic ✅
└─ Workers: [Agent ID 456, Agent ID 789]

                ↓

┌─────────────────────────────────────────────────────┐
│  3. 执行任务                                         │
└─────────────────────────────────────────────────────┘
创建MagenticManager：
├─ LLM配置（来自协调者Agent）✅
│  ├─ Provider: OpenAI
│  ├─ Model: gpt-4o
│  ├─ ApiKey: sk-xxx
│  └─ Endpoint: https://api.openai.com
├─ 提示词（优先级）✅
│  ├─ 自定义提示词 > 协调者默认提示词 > 系统默认
│  └─ 使用: "你是一个Magentic协调者..."
└─ 其他配置（来自协调者Agent）✅
   ├─ Temperature: 0.7
   └─ MaxTokens: 4000

                ↓

┌─────────────────────────────────────────────────────┐
│  4. MagenticManager运行                             │
└─────────────────────────────────────────────────────┘
使用协调者的配置：
├─ 调用LLM（使用协调者的ApiKey）✅
├─ 使用提示词（自定义或默认）✅
├─ 维护任务账本
├─ 维护进度账本
└─ 协调Worker Agents
```

---

## ✅ 核心优势

### 1. **配置复用**
- ✅ 复用协调者的LLM配置
- ✅ 复用协调者的API Key
- ✅ 复用协调者的其他配置

### 2. **用户友好**
- ✅ 用户只需配置一次协调者
- ✅ 无需为MagenticManager单独配置
- ✅ 符合直觉

### 3. **灵活性**
- ✅ 支持自定义提示词
- ✅ 支持不同的LLM提供商
- ✅ 支持不同的模型

### 4. **安全性**
- ✅ API Key统一管理
- ✅ 加密存储
- ✅ 权限控制

---

## 🚀 实施建议

### 1. **Agent创建界面**

```tsx
<Form form={form} layout="vertical">
  <Form.Item label="Agent角色" name="role">
    <Radio.Group>
      <Radio value="Manager">
        <CrownOutlined /> Manager（协调者）
        <Alert message="作为协调者，需要配置LLM，用于Magentic或GroupChat模式" />
      </Radio>
      <Radio value="Worker">
        <UserOutlined /> Worker（执行者）
        <Alert message="作为执行者，需要配置LLM和工具" />
      </Radio>
    </Radio.Group>
  </Form.Item>
  
  {/* LLM配置（协调者和Worker都需要） */}
  <Form.Item label="LLM提供商" name="llmProvider">
    <Select>
      <Option value="OpenAI">OpenAI</Option>
      <Option value="Azure">Azure OpenAI</Option>
      <Option value="Ollama">Ollama（本地）</Option>
    </Select>
  </Form.Item>
  
  <Form.Item label="模型" name="llmModel">
    <Input placeholder="gpt-4o" />
  </Form.Item>
  
  <Form.Item label="API Key" name="llmApiKey">
    <Input.Password placeholder="sk-xxx" />
  </Form.Item>
  
  <Form.Item label="API端点" name="llmEndpoint">
    <Input placeholder="https://api.openai.com" />
  </Form.Item>
  
  {/* 系统提示词 */}
  <Form.Item label="系统提示词" name="systemPrompt">
    <Input.TextArea rows={6} />
  </Form.Item>
</Form>
```

### 2. **任务创建界面**

```tsx
<Form.Item label="协调者" name="managerAgentId">
  <Select>
    {agents.filter(a => a.role === 'Manager').map(agent => (
      <Option key={agent.id} value={agent.id}>
        <Space>
          <CrownOutlined />
          {agent.name}
          <Tag color="blue">{agent.llmModel}</Tag>
        </Space>
      </Option>
    ))}
  </Select>
</Form.Item>

{/* 显示协调者的LLM配置 */}
{selectedManager && (
  <Alert
    message="协调者配置"
    description={
      <div>
        <p>LLM: {selectedManager.llmProvider} - {selectedManager.llmModel}</p>
        <p>此配置将用于MagenticManager</p>
      </div>
    }
    type="info"
  />
)}

{/* 自定义提示词（可选） */}
<Form.Item label="自定义协调者提示词（可选）">
  <Input.TextArea 
    rows={6}
    placeholder="留空则使用协调者的默认提示词"
  />
</Form.Item>
```

---

## 🎯 总结

### 核心设计原则

**MagenticManager的所有配置都来自协调者Agent！**

```
MagenticManager配置 = 协调者Agent配置 ✅
├─ LLM配置 ✅
│  ├─ Provider
│  ├─ Model
│  ├─ ApiKey
│  └─ Endpoint
├─ 提示词 ✅
│  ├─ 自定义提示词（优先）
│  └─ 协调者默认提示词
└─ 其他配置 ✅
   ├─ Temperature
   └─ MaxTokens
```

### 为什么这样设计？

1. ✅ **复用已有配置**：用户已经配置好了协调者
2. ✅ **符合直觉**：协调者就是Manager，MagenticManager也是Manager
3. ✅ **简化配置**：无需为MagenticManager单独配置
4. ✅ **统一管理**：所有配置都在Agent表中

**这是最合理、最优雅的设计方案！** 🎯
