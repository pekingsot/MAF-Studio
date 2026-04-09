# MAF高级群聊系统架构设计文档 v2.0

## 📋 概述

本文档基于 **Microsoft Agent Framework (MAF) v1.0** 和 **Magentic-One** 研究成果，设计一个专业级、高大上的智能群聊系统。核心设计理念：

> **摆脱简单的"轮询发言"，转向"基于语义的动态调度"**

---

## 🎯 核心设计理念

### 1. 隐藏协调者（Hidden Orchestrator）

**核心理念**：协调者不参与对话内容生成，只负责决策流转。

```
┌─────────────────────────────────────────────────────┐
│              Hidden Orchestrator                     │
│  ┌───────────────────────────────────────────────┐  │
│  │         Task Ledger (任务账本)                │  │
│  │  • 全局目标：完成登录功能                     │  │
│  │  • 当前状态：代码已生成，待评审               │  │
│  │  • 下一步：调用 ReviewerAgent                 │  │
│  └───────────────────────────────────────────────┘  │
│                                                      │
│  ┌───────────────────────────────────────────────┐  │
│  │      Speaker Selection Strategy               │  │
│  │  分析上一轮对话 → 决定下一位发言者           │  │
│  │  "提到漏洞" → 自动点名"安全专家"              │  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
         ↓                    ↓                    ↓
   ┌──────────┐        ┌──────────┐        ┌──────────┐
   │ Coder    │        │ Reviewer │        │ Security │
   │ Agent    │        │ Agent    │        │ Agent    │
   └──────────┘        └──────────┘        └──────────┘
```

**高大上之处**：
- ✅ 用户只看到Agent们在专业对话，感觉非常默契
- ✅ 避免死循环：协调者发现复读或卡住时强制介入
- ✅ 状态管理：协调者只给Agent最有用的信息，防止Token爆炸

---

### 2. 三种协调模式对比

| 模式 | 协调者类型 | 发言顺序 | 高级特性 | 适用场景 |
|------|-----------|---------|---------|---------|
| **轮询模式** | `RoundRobinGroupChatManager` | 固定轮流 | 简单稳定 | 固定流程协作 |
| **协调者模式** | `ManagerGroupChatManager` | Manager引导 | 人工干预点 | 需要人工把关 |
| **智能模式** | `IntelligentGroupChatManager` | 语义动态选择 | 自动识别专业领域 | 高级专业讨论 |

#### 2.1 轮询模式（基础版）

```csharp
// 简单的轮流发言
RoundRobinGroupChatManager manager = new RoundRobinGroupChatManager 
{ 
    MaximumInvocationCount = 10,
    AllowRepeatSpeaker = false  // 避免复读机
};
```

**特点**：
- ✅ 简单可靠
- ❌ 缺乏智能调度

#### 2.2 协调者模式（中级版）

```csharp
// Manager引导发言，可以在关键点请求人工干预
ManagerGroupChatManager manager = new ManagerGroupChatManager(managerAgent)
{
    MaximumInvocationCount = 10,
    EnableHumanInTheLoop = true  // 启用人在回路
};
```

**特点**：
- ✅ Manager可以引导讨论方向
- ✅ 支持关键决策点的人工干预
- ❌ 需要指定Manager Agent

#### 2.3 智能模式（高级版）⭐

```csharp
// 基于语义的动态调度
IntelligentGroupChatManager manager = new IntelligentGroupChatManager
{
    MaximumInvocationCount = 10,
    SpeakerSelectionMode = SpeakerSelectionMode.Auto,  // LLM自动选择
    AllowRepeatSpeaker = false,
    
    // 自定义发言人选择策略
    SpeakerSelectionStrategy = new SemanticSpeakerSelector(
        async (history) => {
            var lastMessage = history.Last();
            
            // 基于语义的智能调度
            if (lastMessage.Content.Contains("漏洞") || 
                lastMessage.Content.Contains("安全"))
                return "安全专家";
            
            if (lastMessage.Content.Contains("代码") || 
                lastMessage.Content.Contains("实现"))
                return "程序员";
            
            if (lastMessage.Content.Contains("架构") || 
                lastMessage.Content.Contains("设计"))
                return "架构师";
            
            // 默认：LLM自动选择
            return null;
        }
    )
};
```

**高大上特性**：
- ✅ **语义理解**：分析对话内容，自动识别专业领域
- ✅ **动态调度**：根据上下文智能选择最合适的发言者
- ✅ **避免混乱**：自动跳过不相关的Agent
- ✅ **专业匹配**：讨论安全问题时自动点名安全专家

---

### 3. 反思与评审机制（Critic & Reviewer）

**核心理念**：引入专门负责"挑刺"或"总结"的角色，显著提升产出质量。

```
┌─────────────────────────────────────────────────────┐
│          三级评审流程（Three-Tier Review）           │
└─────────────────────────────────────────────────────┘

第一轮：执行者（Executor）
┌──────────────────────────────────────┐
│ CoderAgent: "这是登录功能的代码..."  │
└──────────────────────────────────────┘
                ↓
第二轮：评审员（Critic）
┌──────────────────────────────────────┐
│ SecurityAgent: "发现SQL注入风险..."  │
│ CodeReviewerAgent: "代码质量需改进"  │
└──────────────────────────────────────┘
                ↓
第三轮：终审员（Global Admin/Human）
┌──────────────────────────────────────┐
│ Human: "批准部署" ✅                 │
└──────────────────────────────────────┘
                ↓
最终输出：完美的解决方案
```

**角色配置**：

```csharp
// 1. 执行者：负责输出初稿
ChatCompletionAgent coderAgent = new ChatCompletionAgent
{
    Name = "程序员",
    Description = "负责编写代码实现",
    Instructions = "你是一个资深程序员，负责编写高质量的代码...",
    Kernel = kernel
};

// 2. 评审员：负责挑刺
ChatCompletionAgent securityAgent = new ChatCompletionAgent
{
    Name = "安全专家",
    Description = "专注于寻找安全漏洞",
    Instructions = "你是一个安全专家，负责审查代码中的安全风险...",
    Kernel = kernel
};

ChatCompletionAgent codeReviewerAgent = new ChatCompletionAgent
{
    Name = "代码评审员",
    Description = "关注代码质量和实现细节",
    Instructions = "你是一个代码评审专家，负责检查代码质量...",
    Kernel = kernel
};

// 3. 终审员：人类把关
// 通过 Human-in-the-Loop 机制实现
```

**高大上之处**：
- ✅ 用户只看到最终完美的答案
- ✅ 后台经历了一场精彩的辩论和自我修正
- ✅ 显著提升产出质量

---

### 4. 多模态与实时感知（Rich Interactions）

**核心理念**：利用MAF对MCP（Model Context Protocol）的支持，让Agent拥有"手"和"眼"。

#### 4.1 实时绘图Agent

```csharp
// 当讨论到架构时，自动生成流程图
ChatCompletionAgent architectAgent = new ChatCompletionAgent
{
    Name = "架构师",
    Description = "负责架构设计和流程图绘制",
    Instructions = @"你是一个架构师，负责设计系统架构。
    
当讨论到架构或流程时，你会自动调用绘图工具生成流程图。",
    Kernel = kernel
};

// 配置MCP工具
architectAgent.Tools.Add(new DrawDiagramTool());
```

**使用场景**：
- 讨论系统架构时，自动生成架构图
- 讨论业务流程时，自动生成流程图
- 讨论数据流时，自动生成数据流图

#### 4.2 外部感知Agent（WebSurfer）

```csharp
// 当话题涉及实时数据时，自动插入最新信息
ChatCompletionAgent webSurferAgent = new ChatCompletionAgent
{
    Name = "信息检索员",
    Description = "负责获取实时数据和最新信息",
    Instructions = @"你负责从互联网获取实时数据。
    
当讨论涉及实时信息（如股价、新闻、天气）时，你会自动搜索并插入最新数据。",
    Kernel = kernel
};

// 配置Web搜索工具
webSurferAgent.Tools.Add(new WebSearchTool());
webSurferAgent.Tools.Add(new StockPriceTool());
```

**使用场景**：
- 讨论股价时，自动获取实时股价
- 讨论新闻时，自动搜索最新新闻
- 讨论天气时，自动获取实时天气

#### 4.3 文件操作Agent（FileSurfer）

```csharp
// 负责文件读写操作
ChatCompletionAgent fileAgent = new ChatCompletionAgent
{
    Name = "文件管理员",
    Description = "负责文件读写和代码提交",
    Instructions = "你负责文件操作，包括读取文件、写入文件、Git提交等。",
    Kernel = kernel
};

// 配置文件工具
fileAgent.Tools.Add(new ReadFileTool());
fileAgent.Tools.Add(new WriteFileTool());
fileAgent.Tools.Add(new GitCommitTool());
```

---

### 5. 人在回路（Human-in-the-Loop, HITL）

**核心理念**：在关键决策点，群聊自动挂起并请求人类批准。

```
┌─────────────────────────────────────────────────────┐
│          Human-in-the-Loop 工作流                   │
└─────────────────────────────────────────────────────┘

Agent讨论中...
                ↓
检测到关键决策点（如：是否部署代码）
                ↓
┌──────────────────────────────────────┐
│  🔔 推送通知到你的手机                 │
│  "是否批准此架构方案？"                │
│  [批准] [拒绝] [修改建议]             │
└──────────────────────────────────────┘
                ↓
用户点击"批准" ✅
                ↓
群聊继续执行...
```

**实现方式**：

```csharp
// 配置HITL
GroupChatOrchestration orchestration = new GroupChatOrchestration(
    manager,
    agents.ToArray())
{
    ResponseCallback = responseCallback,
    
    // 关键：配置人工干预回调
    InteractiveCallback = async () =>
    {
        // 推送通知到用户手机
        await SendPushNotification("是否批准此方案？");
        
        // 等待用户响应
        var userResponse = await WaitForUserInput();
        
        return new ChatMessageContent(AuthorRole.User, userResponse);
    }
};
```

**关键决策点**：
- ✅ 是否部署代码
- ✅ 是否批准架构方案
- ✅ 是否执行高风险操作
- ✅ 是否调用外部API

---

## 🏗️ 完整架构设计

### 架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    MAF Advanced Group Chat                   │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  Layer 1: Orchestration Layer (协调层)                      │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Hidden Orchestrator (隐藏协调者)                      │ │
│  │  ┌──────────────────────────────────────────────────┐ │ │
│  │  │  MagenticOneOrchestrator / GroupChatManager      │ │ │
│  │  │  • Task Ledger (任务账本)                        │ │ │
│  │  │  • Progress Ledger (进度账本)                    │ │ │
│  │  │  • Speaker Selection Strategy (发言人选择策略)   │ │ │
│  │  └──────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 2: Agent Layer (智能体层)                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ Executor     │  │ Critic       │  │ Specialist   │      │
│  │ Agents       │  │ Agents       │  │ Agents       │      │
│  │              │  │              │  │              │      │
│  │ • Coder      │  │ • Security   │  │ • Architect  │      │
│  │ • Writer     │  │ • Reviewer   │  │ • WebSurfer  │      │
│  │ • Analyst    │  │ • QA         │  │ • FileSurfer │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 3: Tool Layer (工具层 - MCP)                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ Code Tools   │  │ Web Tools    │  │ File Tools   │      │
│  │              │  │              │  │              │      │
│  │ • Execute    │  │ • Search     │  │ • Read       │      │
│  │ • Test       │  │ • Fetch      │  │ • Write      │      │
│  │ • Lint       │  │ • API Call   │  │ • Git Commit │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 4: Human Layer (人类层 - HITL)                       │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Human-in-the-Loop (人在回路)                          │ │
│  │  • 关键决策点推送通知                                  │ │
│  │  • 批准/拒绝机制                                       │ │
│  │  • 修改建议输入                                        │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔧 技术实现方案

### 1. MagenticOneOrchestrator 初始化

```csharp
public class AdvancedGroupChatService
{
    private readonly IChatClient _managerClient;  // 协调者专用LLM（建议GPT-4o）
    private readonly ILogger<AdvancedGroupChatService> _logger;

    public async Task<CollaborationResult> ExecuteAdvancedGroupChatAsync(
        long collaborationId,
        string input,
        AdvancedGroupChatConfig config)
    {
        // 1. 创建Worker Agents
        var agents = await CreateAgentsAsync(collaborationId, config);
        
        // 2. 创建隐藏协调者（MagenticOneOrchestrator）
        var orchestrator = new MagenticOneOrchestrator(
            _managerClient,
            new OpenAIPromptExecutionSettings())
        {
            MaximumInvocationCount = config.MaxIterations,
            ShowPlanningProcess = false,  // 隐藏思考过程
            
            // 配置任务账本
            TaskLedger = new TaskLedger
            {
                GlobalGoal = input,
                KnownFacts = new List<string>(),
                OverallPlan = "动态制定执行计划",
                Boundaries = new List<string>()
            }
        };
        
        // 3. 创建GroupChatOrchestration
        var orchestration = new GroupChatOrchestration(
            orchestrator,
            agents.ToArray())
        {
            ResponseCallback = (response) =>
            {
                _logger.LogInformation("[{Agent}] {Content}", 
                    response.AuthorName, response.Content);
                return ValueTask.CompletedTask;
            },
            
            // 配置人在回路
            InteractiveCallback = config.EnableHITL 
                ? CreateHITLCallback() 
                : null
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
    
    private Func<Task<ChatMessageContent>> CreateHITLCallback()
    {
        return async () =>
        {
            // 推送通知
            await SendPushNotification("需要您的批准");
            
            // 等待用户输入
            var userInput = await WaitForUserResponse();
            
            return new ChatMessageContent(AuthorRole.User, userInput);
        };
    }
}
```

### 2. 智能发言人选择策略

```csharp
public class SemanticSpeakerSelector : ISpeakerSelectionStrategy
{
    private readonly Func<IReadOnlyList<ChatMessageContent>, Task<string?>> _selector;

    public SemanticSpeakerSelector(
        Func<IReadOnlyList<ChatMessageContent>, Task<string?>> selector)
    {
        _selector = selector;
    }

    public async Task<string?> SelectNextSpeakerAsync(
        IReadOnlyList<ChatMessageContent> history,
        IReadOnlyList<Agent> agents)
    {
        // 1. 尝试自定义语义选择
        var selectedAgent = await _selector(history);
        
        if (!string.IsNullOrEmpty(selectedAgent))
        {
            return selectedAgent;
        }
        
        // 2. 默认：使用LLM自动选择
        return await AutoSelectByLLM(history, agents);
    }
    
    private async Task<string?> AutoSelectByLLM(
        IReadOnlyList<ChatMessageContent> history,
        IReadOnlyList<Agent> agents)
    {
        // 使用LLM分析对话历史，选择最合适的发言者
        // ...
    }
}
```

### 3. MCP工具集成

```csharp
// 定义MCP工具
public class DrawDiagramTool : Tool
{
    public override string Name => "draw_diagram";
    public override string Description => "绘制架构图或流程图";
    
    public override async Task<ToolResult> ExecuteAsync(ToolInput input)
    {
        var diagramType = input.Parameters["type"];
        var content = input.Parameters["content"];
        
        // 调用绘图API
        var imageUrl = await DiagramService.GenerateAsync(diagramType, content);
        
        return new ToolResult
        {
            Output = $"已生成{diagramType}图：{imageUrl}"
        };
    }
}

// 注册工具到Agent
agent.Tools.Add(new DrawDiagramTool());
agent.Tools.Add(new WebSearchTool());
agent.Tools.Add(new GitCommitTool());
```

---

## 📊 配置模型设计

### 1. 高级配置模型

```csharp
public class AdvancedGroupChatConfig
{
    // 协调模式
    public OrchestrationMode Mode { get; set; } = OrchestrationMode.Intelligent;
    
    // 最大迭代次数
    public int MaxIterations { get; set; } = 10;
    
    // 是否显示协调者思考过程
    public bool ShowPlanningProcess { get; set; } = false;
    
    // 是否启用人在回路
    public bool EnableHITL { get; set; } = false;
    
    // 发言人选择策略
    public SpeakerSelectionMode SpeakerSelectionMode { get; set; } = 
        SpeakerSelectionMode.Auto;
    
    // 是否允许重复发言
    public bool AllowRepeatSpeaker { get; set; } = false;
    
    // 自定义语义选择规则（可选）
    public List<SemanticRule>? SemanticRules { get; set; }
    
    // 关键决策点（用于HITL）
    public List<string>? CriticalDecisionPoints { get; set; }
}

public enum OrchestrationMode
{
    RoundRobin,      // 轮询模式
    Manager,         // 协调者模式
    Intelligent      // 智能模式（推荐）
}

public enum SpeakerSelectionMode
{
    Auto,            // LLM自动选择
    Semantic,        // 基于语义规则
    Hybrid           // 混合模式
}

public class SemanticRule
{
    public string Keyword { get; set; }  // 关键词
    public string TargetAgent { get; set; }  // 目标Agent
    public int Priority { get; set; }  // 优先级
}
```

### 2. 示例配置

```json
{
  "mode": "Intelligent",
  "maxIterations": 15,
  "showPlanningProcess": false,
  "enableHITL": true,
  "speakerSelectionMode": "Semantic",
  "allowRepeatSpeaker": false,
  "semanticRules": [
    {
      "keyword": "安全|漏洞|风险",
      "targetAgent": "安全专家",
      "priority": 10
    },
    {
      "keyword": "代码|实现|编程",
      "targetAgent": "程序员",
      "priority": 8
    },
    {
      "keyword": "架构|设计|流程",
      "targetAgent": "架构师",
      "priority": 9
    }
  ],
  "criticalDecisionPoints": [
    "是否部署代码",
    "是否批准架构方案",
    "是否执行高风险操作"
  ]
}
```

---

## 🎨 前端UI设计

### 1. 高级配置界面

```tsx
<Form form={form} layout="vertical">
  {/* 协调模式选择 */}
  <Form.Item label="协调模式">
    <Radio.Group value={mode}>
      <Space direction="vertical">
        <Radio value="RoundRobin">
          <Space>
            <SwapOutlined />
            <span>轮询模式</span>
            <Tag color="default">基础</Tag>
          </Space>
        </Radio>
        <Radio value="Manager">
          <Space>
            <CrownOutlined />
            <span>协调者模式</span>
            <Tag color="blue">中级</Tag>
          </Space>
        </Radio>
        <Radio value="Intelligent">
          <Space>
            <BulbOutlined />
            <span>智能模式</span>
            <Tag color="gold">高级 ⭐</Tag>
          </Space>
        </Radio>
      </Space>
    </Radio.Group>
  </Form.Item>

  {/* 智能模式专属配置 */}
  {mode === 'Intelligent' && (
    <>
      <Alert
        message="智能模式特性"
        description={
          <ul>
            <li>✅ 基于语义的动态调度</li>
            <li>✅ 自动识别专业领域</li>
            <li>✅ 隐藏协调者（用户不可见）</li>
          </ul>
        }
        type="info"
      />
      
      <Form.Item label="发言人选择策略">
        <Radio.Group value={speakerSelectionMode}>
          <Radio value="Auto">LLM自动选择</Radio>
          <Radio value="Semantic">语义规则匹配</Radio>
          <Radio value="Hybrid">混合模式</Radio>
        </Radio.Group>
      </Form.Item>
      
      {/* 语义规则配置 */}
      {speakerSelectionMode === 'Semantic' && (
        <Form.Item label="语义规则">
          <SemanticRulesEditor />
        </Form.Item>
      )}
    </>
  )}

  {/* 人在回路配置 */}
  <Form.Item label="人在回路（HITL）">
    <Switch 
      checked={enableHITL} 
      onChange={setEnableHITL}
      checkedChildren="启用"
      unCheckedChildren="禁用"
    />
    <Tooltip title="在关键决策点，系统会推送通知请求您的批准">
      <InfoCircleOutlined style={{ marginLeft: 8 }} />
    </Tooltip>
  </Form.Item>
  
  {enableHITL && (
    <Form.Item label="关键决策点">
      <Select
        mode="tags"
        placeholder="输入关键决策点，如：是否部署代码"
        value={criticalDecisionPoints}
        onChange={setCriticalDecisionPoints}
      />
    </Form.Item>
  )}
</Form>
```

### 2. 可视化看板（.NET Aspire Dashboard）

```tsx
// 实时展示群聊状态
<Dashboard>
  {/* Agent状态卡片 */}
  <AgentStatusGrid>
    {agents.map(agent => (
      <AgentCard key={agent.id}>
        <Avatar name={agent.name} status={agent.status} />
        <ProgressBar 
          value={agent.energyLevel} 
          label="能量值" 
        />
        <SpeechCount count={agent.speechCount} />
      </AgentCard>
    ))}
  </AgentStatusGrid>
  
  {/* 对话流程图 */}
  <ConversationFlow>
    <Timeline>
      {messages.map(msg => (
        <Timeline.Item 
          key={msg.id}
          color={msg.agent === 'Orchestrator' ? 'blue' : 'green'}
        >
          <strong>{msg.agent}</strong>: {msg.content}
        </Timeline.Item>
      ))}
    </Timeline>
  </ConversationFlow>
  
  {/* 协调者状态（可隐藏） */}
  {showOrchestrator && (
    <OrchestratorPanel>
      <TaskLedgerViewer ledger={taskLedger} />
      <ProgressLedgerViewer ledger={progressLedger} />
    </OrchestratorPanel>
  )}
</Dashboard>
```

---

## 🚀 实施路线图

### Phase 1: 核心功能实现（2周）

1. ✅ 实现三种协调模式
2. ✅ 实现隐藏协调者机制
3. ✅ 实现智能发言人选择策略
4. ✅ 集成MAF Runtime

### Phase 2: 高级特性（2周）

1. ✅ 实现反思评审机制
2. ✅ 集成MCP工具（绘图、Web搜索、文件操作）
3. ✅ 实现人在回路（HITL）
4. ✅ 推送通知集成

### Phase 3: 可视化与优化（1周）

1. ✅ .NET Aspire Dashboard集成
2. ✅ 实时状态展示
3. ✅ 性能优化
4. ✅ 用户体验优化

---

## 📚 参考资料

1. [MAF官方文档 - Magentic编排](https://learn.microsoft.com/zh-cn/semantic-kernel/frameworks/agent/agent-orchestration/magentic)
2. [MAF官方文档 - GroupChat编排](https://learn.microsoft.com/zh-cn/semantic-kernel/frameworks/agent/agent-orchestration/group-chat)
3. [MAF官方文档 - 高级主题](https://learn.microsoft.com/zh-cn/semantic-kernel/frameworks/agent/agent-orchestration/advanced-topics)
4. [Magentic-One研究论文](https://www.microsoft.com/en-us/research/publication/magentic-one/)

---

## ✅ 预期成果

### 功能成果

1. **专业级群聊系统**：
   - ✅ 基于语义的动态调度
   - ✅ 隐藏协调者机制
   - ✅ 反思评审流程
   - ✅ 多模态交互

2. **高大上特性**：
   - ✅ 用户只看到完美结果
   - ✅ 后台智能调度
   - ✅ 关键决策人工把关
   - ✅ 实时可视化

### 技术成果

1. **代码质量**：
   - ✅ 完全基于MAF官方API
   - ✅ 类型安全
   - ✅ 可维护性强

2. **性能优化**：
   - ✅ MAF Runtime优化
   - ✅ Token使用优化
   - ✅ 并发执行支持

---

## 🎯 下一步行动

**请确认此设计文档后，我将开始实施：**

1. ✅ 实现隐藏协调者机制
2. ✅ 实现智能发言人选择策略
3. ✅ 实现反思评审机制
4. ✅ 集成MCP工具
5. ✅ 实现人在回路

**请审核此高级架构设计文档！** 🚀
