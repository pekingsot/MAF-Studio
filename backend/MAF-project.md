# MAFStudio 集成 Microsoft Agent Framework 规划

## 项目现状分析

### 当前架构

```
MAFStudio/
├── MAFStudio.Api/           # Web API 层
├── MAFStudio.Application/   # 应用服务层
├── MAFStudio.Core/          # 核心领域层
├── MAFStudio.Infrastructure/# 基础设施层
└── MAFStudio.Tests/         # 测试层
```

### 现有功能

- 用户认证与授权
- LLM 配置管理（支持多 Provider）
- Agent 类型管理
- Agent 实例管理
- 协作（Collaboration）管理
- 操作日志与系统日志

---

## MAF 集成规划

### 阶段一：基础集成（预计 2 周）

#### 1.1 引入 MAF 包

```xml
<!-- MAFStudio.Infrastructure/MAFStudio.Infrastructure.csproj -->
<ItemGroup>
    <PackageReference Include="Microsoft.Agents.AI.OpenAI" Version="1.0.0-preview" />
    <PackageReference Include="Microsoft.Agents.AI.Foundry" Version="1.0.0-preview" />
</ItemGroup>
```

#### 1.2 创建 Agent 工厂服务

```csharp
// MAFStudio.Application/Services/AgentFactoryService.cs
namespace MAFStudio.Application.Services;

public interface IAgentFactoryService
{
    Task<AIAgent> CreateAgentAsync(long agentId);
    Task<AIAgent> CreateAgentFromConfigAsync(LlmConfig config, string instructions, string name);
}

public class AgentFactoryService : IAgentFactoryService
{
    private readonly ILlmConfigRepository _llmConfigRepository;
    private readonly IAgentRepository _agentRepository;

    public async Task<AIAgent> CreateAgentAsync(long agentId)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent?.LlmConfig == null)
        {
            throw new InvalidOperationException("Agent or LLM config not found");
        }

        return await CreateAgentFromConfigAsync(agent.LlmConfig, agent.Configuration, agent.Name);
    }

    public async Task<AIAgent> CreateAgentFromConfigAsync(LlmConfig config, string instructions, string name)
    {
        var client = CreateChatClient(config);
        return client.AsAIAgent(instructions: instructions, name: name);
    }

    private IChatClient CreateChatClient(LlmConfig config)
    {
        return config.Provider.ToLower() switch
        {
            "openai" => CreateOpenAIClient(config),
            "azure" => CreateAzureOpenAIClient(config),
            "qwen" => CreateQwenClient(config),
            "deepseek" => CreateDeepSeekClient(config),
            _ => throw new NotSupportedException($"Provider {config.Provider} not supported")
        };
    }
}
```

#### 1.3 集成现有 LLM 配置

```csharp
// MAFStudio.Application/Services/ChatService.cs
public class ChatService : IChatService
{
    private readonly IAgentFactoryService _agentFactory;

    public async Task<string> ChatAsync(long agentId, string message)
    {
        var agent = await _agentFactory.CreateAgentAsync(agentId);
        return await agent.RunAsync(message);
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(long agentId, string message)
    {
        var agent = await _agentFactory.CreateAgentAsync(agentId);
        
        await foreach (var chunk in agent.RunStreamingAsync(message))
        {
            if (chunk.Text != null)
            {
                yield return chunk.Text;
            }
        }
    }
}
```

---

### 阶段二：工作流集成（预计 3 周）

#### 2.1 Sequential Workflow 实现

```csharp
// MAFStudio.Application/Workflows/SequentialWorkflowService.cs
public class SequentialWorkflowService
{
    public async Task<WorkflowResult> ExecuteSequentialAsync(
        List<long> agentIds, 
        string input)
    {
        var builder = AgentWorkflowBuilder.Create();
        
        foreach (var agentId in agentIds)
        {
            var agent = await _agentFactory.CreateAgentAsync(agentId);
            builder.AddAgent(agent);
        }
        
        var workflow = builder.Build();
        return await workflow.RunAsync(input);
    }
}
```

#### 2.2 Handoffs Workflow 实现

```csharp
// MAFStudio.Application/Workflows/HandoffsWorkflowService.cs
public class HandoffsWorkflowService
{
    public async Task SetupHandoffsAsync(long primaryAgentId, List<long> targetAgentIds)
    {
        var primaryAgent = await _agentFactory.CreateAgentAsync(primaryAgentId);
        var targetAgents = new List<AIAgent>();
        
        foreach (var id in targetAgentIds)
        {
            targetAgents.Add(await _agentFactory.CreateAgentAsync(id));
        }
        
        primaryAgent.AddHandoffs(targetAgents);
    }
}
```

#### 2.3 Group Chat 实现

```csharp
// MAFStudio.Application/Services/GroupChatService.cs
public class GroupChatService
{
    public async IAsyncEnumerable<GroupChatMessage> RunGroupChatAsync(
        long collaborationId, 
        string topic)
    {
        var collaboration = await _collaborationRepository.GetByIdAsync(collaborationId);
        var agents = new List<AIAgent>();
        
        foreach (var agentMember in collaboration.Agents)
        {
            agents.Add(await _agentFactory.CreateAgentAsync(agentMember.AgentId));
        }
        
        var groupChat = new AgentGroupChat(agents);
        
        await foreach (var message in groupChat.RunStreamingAsync(topic))
        {
            yield return new GroupChatMessage
            {
                Sender = message.Sender,
                Content = message.Content,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
```

---

### 阶段三：工具集成（预计 2 周）

#### 3.1 定义工具基类

```csharp
// MAFStudio.Core/Tools/AgentToolBase.cs
public abstract class AgentToolBase
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters);
}
```

#### 3.2 实现常用工具

```csharp
// MAFStudio.Infrastructure/Tools/WeatherTool.cs
public class WeatherTool : AgentToolBase
{
    public override string Name => "get_weather";
    public override string Description => "获取指定城市的天气信息";
    
    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var city = parameters["city"]?.ToString();
        // 调用天气 API
        var weather = await _weatherService.GetWeatherAsync(city);
        return new ToolResult { Success = true, Data = weather };
    }
}

// MAFStudio.Infrastructure/Tools/DatabaseQueryTool.cs
public class DatabaseQueryTool : AgentToolBase
{
    public override string Name => "query_database";
    public override string Description => "查询数据库（只读）";
    
    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var query = parameters["query"]?.ToString();
        // 安全的只读查询
        var result = await _dbService.ExecuteQueryAsync(query);
        return new ToolResult { Success = true, Data = result };
    }
}
```

#### 3.3 工具注册服务

```csharp
// MAFStudio.Application/Services/ToolRegistryService.cs
public class ToolRegistryService
{
    private readonly Dictionary<string, AgentToolBase> _tools = new();
    
    public void RegisterTool(AgentToolBase tool)
    {
        _tools[tool.Name] = tool;
    }
    
    public IEnumerable<AgentToolBase> GetTools(IEnumerable<string> toolNames)
    {
        return toolNames.Where(name => _tools.ContainsKey(name))
                       .Select(name => _tools[name]);
    }
}
```

---

### 阶段四：状态持久化（预计 2 周）

#### 4.1 会话状态管理

```csharp
// MAFStudio.Application/Services/ConversationStateService.cs
public class ConversationStateService
{
    public async Task SaveStateAsync(string sessionId, ConversationState state)
    {
        // 保存到数据库
        var entity = new ConversationStateEntity
        {
            SessionId = sessionId,
            State = JsonSerializer.Serialize(state),
            UpdatedAt = DateTime.UtcNow
        };
        await _stateRepository.SaveAsync(entity);
    }
    
    public async Task<ConversationState?> LoadStateAsync(string sessionId)
    {
        var entity = await _stateRepository.GetBySessionIdAsync(sessionId);
        if (entity == null) return null;
        
        return JsonSerializer.Deserialize<ConversationState>(entity.State);
    }
}
```

#### 4.2 集成到 Agent

```csharp
public class StatefulChatService
{
    public async Task<string> ChatAsync(string sessionId, long agentId, string message)
    {
        var agent = await _agentFactory.CreateAgentAsync(agentId);
        
        // 恢复状态
        var state = await _stateService.LoadStateAsync(sessionId);
        if (state != null)
        {
            agent.SetState(state);
        }
        
        // 执行对话
        var response = await agent.RunAsync(message);
        
        // 保存状态
        var newState = agent.GetState();
        await _stateService.SaveStateAsync(sessionId, newState);
        
        return response;
    }
}
```

---

## 数据库扩展

### 新增表结构

```sql
-- 会话状态表
CREATE TABLE conversation_states (
    id BIGINT PRIMARY KEY,
    session_id VARCHAR(100) NOT NULL UNIQUE,
    agent_id BIGINT NOT NULL,
    user_id VARCHAR(36) NOT NULL,
    state TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 工具定义表
CREATE TABLE agent_tools (
    id BIGINT PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    parameters_schema TEXT,
    is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Agent 工具关联表
CREATE TABLE agent_tool_mappings (
    id BIGINT PRIMARY KEY,
    agent_id BIGINT NOT NULL,
    tool_id BIGINT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 工作流定义表
CREATE TABLE workflow_definitions (
    id BIGINT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    type VARCHAR(50) NOT NULL,  -- sequential, concurrent, handoffs, group_chat
    config TEXT,  -- JSON 配置
    is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 工作流执行记录表
CREATE TABLE workflow_executions (
    id BIGINT PRIMARY KEY,
    workflow_id BIGINT NOT NULL,
    user_id VARCHAR(36) NOT NULL,
    input TEXT,
    output TEXT,
    status VARCHAR(20) NOT NULL,
    started_at TIMESTAMP NOT NULL,
    completed_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

---

## API 扩展

### 新增接口

```csharp
// 对话接口
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    [HttpPost("session/{sessionId}/agent/{agentId}")]
    public async Task<ActionResult> Chat(string sessionId, long agentId, [FromBody] ChatRequest request);
    
    [HttpPost("session/{sessionId}/agent/{agentId}/stream")]
    public async IAsyncEnumerable<string> ChatStream(string sessionId, long agentId, [FromBody] ChatRequest request);
}

// 工作流接口
[ApiController]
[Route("api/[controller]")]
public class WorkflowsController : ControllerBase
{
    [HttpPost("{workflowId}/execute")]
    public async Task<ActionResult> ExecuteWorkflow(long workflowId, [FromBody] WorkflowRequest request);
    
    [HttpGet("{workflowId}/executions")]
    public async Task<ActionResult> GetExecutions(long workflowId);
}

// 工具接口
[ApiController]
[Route("api/[controller]")]
public class ToolsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetAvailableTools();
    
    [HttpPost("{toolId}/test")]
    public async Task<ActionResult> TestTool(long toolId, [FromBody] Dictionary<string, object> parameters);
}
```

---

## 实施时间表

| 阶段 | 内容 | 预计时间 | 优先级 |
|------|------|---------|--------|
| 阶段一 | 基础集成 | 2 周 | 高 |
| 阶段二 | 工作流集成 | 3 周 | 高 |
| 阶段三 | 工具集成 | 2 周 | 中 |
| 阶段四 | 状态持久化 | 2 周 | 中 |
| 阶段五 | 测试与优化 | 1 周 | 高 |

**总计：约 10 周**

---

## 风险与缓解措施

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| MAF 预览版不稳定 | 高 | 保持关注官方更新，准备回退方案 |
| Provider 兼容性 | 中 | 实现自定义 Provider 适配器 |
| 性能问题 | 中 | 实现缓存、异步处理 |
| 学习曲线 | 低 | 提供详细文档和示例 |

---

## 成功指标

1. **功能完整性**：支持所有四种工作流模式
2. **性能**：单次对话响应时间 < 3s
3. **可靠性**：工作流执行成功率 > 99%
4. **可扩展性**：支持自定义工具和 Provider
