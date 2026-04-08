# MAF Studio 完整技术方案文档

## 📋 文档说明

本文档是MAF Studio的完整技术方案文档，涵盖了从基础的Agent协作机制到完整的AI公司经营系统的所有设计细节。文档分为以下几个部分：

1. **Agent协作基础**：四种基本协作模式
2. **Magentic工作流**：智能编排机制
3. **工作流模板系统**：复用和学习机制
4. **AI公司系统**：完整的经营模式
5. **人才市场机制**：动态定价和流转
6. **员工管理系统**：全生命周期管理
7. **技术实现细节**：数据库、API、算法

---

## 第一部分：Agent协作基础

### 1.1 协作模式概述

MAF Studio基于Microsoft Agent Framework (MAF)，支持四种基本的协作模式：

| 模式 | 描述 | 适用场景 | 执行方式 |
|------|------|---------|---------|
| **顺序执行** | Agent按顺序依次处理 | 流水线式任务 | 串行 |
| **并发执行** | 多个Agent同时处理 | 多方案对比 | 并行 |
| **任务移交** | Agent之间移交任务 | 专业分工协作 | 串行+移交 |
| **群聊协作** | Agent群聊讨论 | 需要讨论决策 | 并发+讨论 |

### 1.2 顺序执行（Sequential）

#### 1.2.1 概念说明

**定义**：多个Agent按照预定义的顺序依次处理任务，前一个Agent的输出作为后一个Agent的输入。

**执行流程**：
```
用户输入 
  ↓
Agent1（需求分析）
  ↓ 输出：需求文档
Agent2（系统设计）
  ↓ 输出：设计文档
Agent3（代码实现）
  ↓ 输出：代码文件
Agent4（测试验证）
  ↓ 输出：测试报告
最终结果
```

#### 1.2.2 技术实现

**使用MAF的WorkflowBuilder**：

```csharp
/// <summary>
/// 顺序执行工作流
/// </summary>
public async Task<WorkflowResult> ExecuteSequentialWorkflowAsync(
    List<long> agentIds,
    string input,
    CancellationToken cancellationToken = default)
{
    // 1. 获取所有Agent的ChatClient
    var chatClients = new List<IChatClient>();
    foreach (var agentId in agentIds)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId);
        var chatClient = await CreateChatClientAsync(agent);
        chatClients.Add(chatClient);
    }
    
    // 2. 构建顺序工作流
    var startExecutor = new StartExecutor();
    var executors = new List<Executor>();
    
    foreach (var chatClient in chatClients)
    {
        var executor = new ChatClientAgent(chatClient);
        executors.Add(executor);
    }
    
    // 3. 使用WorkflowBuilder构建工作流
    var workflowBuilder = new WorkflowBuilder(startExecutor);
    
    // 添加顺序边
    for (int i = 0; i < executors.Count; i++)
    {
        if (i == 0)
        {
            workflowBuilder.AddEdge(startExecutor, executors[i]);
        }
        else
        {
            workflowBuilder.AddEdge(executors[i - 1], executors[i]);
        }
    }
    
    var workflow = workflowBuilder
        .WithOutputFrom(executors.Last())
        .Build();
    
    // 4. 执行工作流
    var result = await workflow.ExecuteAsync(input, cancellationToken);
    
    return new WorkflowResult
    {
        Success = true,
        Output = result.Output,
        ExecutionPath = result.ExecutionPath
    };
}
```

#### 1.2.3 配置参数

```json
{
  "workflowType": "Sequential",
  "parameters": {
    "agentOrder": [1, 2, 3, 4],
    "stopKeywords": ["完成", "DONE", "结束"],
    "maxRounds": 10,
    "timeout": 300,
    "saveIntermediateResults": true
  }
}
```

**参数说明**：
- `agentOrder`：Agent执行顺序（按ID）
- `stopKeywords`：停止关键词，遇到这些词停止执行
- `maxRounds`：最大执行轮数
- `timeout`：超时时间（秒）
- `saveIntermediateResults`：是否保存中间结果

#### 1.2.4 使用场景

**场景1：软件开发流程**
```
需求分析 → 架构设计 → 编码实现 → 测试验证 → 部署上线
```

**场景2：文档编写流程**
```
资料收集 → 大纲设计 → 内容编写 → 审阅修改 → 最终定稿
```

**场景3：数据处理流程**
```
数据采集 → 数据清洗 → 数据分析 → 报告生成
```

#### 1.2.5 优缺点分析

**优点**：
- ✅ 流程清晰，易于理解和控制
- ✅ 每个步骤都有明确的输入输出
- ✅ 适合需要严格顺序的任务
- ✅ 容易调试和追踪问题

**缺点**：
- ❌ 执行速度较慢（串行）
- ❌ 如果某个Agent失败，整个流程失败
- ❌ 无法利用并行优势

---

### 1.3 并发执行（Concurrent）

#### 1.3.1 概念说明

**定义**：多个Agent同时处理同一个任务，最后汇总所有结果。

**执行流程**：
```
用户输入
  ↓
  ├→ Agent1（方案A）→ 结果1
  ├→ Agent2（方案B）→ 结果2
  └→ Agent3（方案C）→ 结果3
  ↓
结果汇总器
  ↓
最终结果
```

#### 1.3.2 技术实现

**使用MAF的FanOut和FanIn**：

```csharp
/// <summary>
/// 并发执行工作流
/// </summary>
public async Task<WorkflowResult> ExecuteConcurrentWorkflowAsync(
    List<long> agentIds,
    string input,
    string aggregationStrategy = "Merge",
    CancellationToken cancellationToken = default)
{
    // 1. 获取所有Agent的ChatClient
    var chatClients = new List<IChatClient>();
    foreach (var agentId in agentIds)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId);
        var chatClient = await CreateChatClientAsync(agent);
        chatClients.Add(chatClient);
    }
    
    // 2. 构建并发工作流
    var startExecutor = new StartExecutor();
    var parallelExecutors = new List<Executor>();
    
    foreach (var chatClient in chatClients)
    {
        var executor = new ChatClientAgent(chatClient);
        parallelExecutors.Add(executor);
    }
    
    // 3. 创建结果聚合器
    var aggregator = new ResultAggregatorExecutor(aggregationStrategy);
    
    // 4. 使用WorkflowBuilder构建并发工作流
    var workflowBuilder = new WorkflowBuilder(startExecutor);
    
    // 扇出：从startExecutor分发到所有并行执行器
    workflowBuilder.AddFanOutEdge(startExecutor, parallelExecutors);
    
    // 扇入：所有并行执行器的结果汇聚到聚合器
    workflowBuilder.AddFanInEdge(parallelExecutors, aggregator);
    
    var workflow = workflowBuilder
        .WithOutputFrom(aggregator)
        .Build();
    
    // 5. 执行工作流
    var result = await workflow.ExecuteAsync(input, cancellationToken);
    
    return new WorkflowResult
    {
        Success = true,
        Output = result.Output,
        ParallelResults = result.ParallelResults,
        AggregationStrategy = aggregationStrategy
    };
}
```

**结果聚合器实现**：

```csharp
/// <summary>
/// 结果聚合执行器
/// </summary>
public class ResultAggregatorExecutor : Executor
{
    private readonly string _strategy;
    
    public ResultAggregatorExecutor(string strategy)
    {
        _strategy = strategy;
    }
    
    [MessageHandler]
    public async Task<string> AggregateAsync(IList<string> inputs)
    {
        return _strategy switch
        {
            "Merge" => MergeResults(inputs),
            "Vote" => VoteResults(inputs),
            "Best" => SelectBestResult(inputs),
            "Summarize" => SummarizeResults(inputs),
            _ => MergeResults(inputs)
        };
    }
    
    /// <summary>
    /// 合并所有结果
    /// </summary>
    private string MergeResults(IList<string> inputs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== 综合方案 ===");
        
        for (int i = 0; i < inputs.Count; i++)
        {
            sb.AppendLine($"\n### 方案 {i + 1}");
            sb.AppendLine(inputs[i]);
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// 投票选择最佳结果
    /// </summary>
    private string VoteResults(IList<string> inputs)
    {
        // 使用LLM评估每个方案
        var evaluations = new List<(int Index, int Score)>();
        
        for (int i = 0; i < inputs.Count; i++)
        {
            var score = EvaluateResult(inputs[i]);
            evaluations.Add((i, score));
        }
        
        // 选择得分最高的
        var best = evaluations.OrderByDescending(e => e.Score).First();
        return $"=== 最佳方案（方案{best.Index + 1}）===\n{inputs[best.Index]}";
    }
    
    /// <summary>
    /// 选择最佳结果
    /// </summary>
    private string SelectBestResult(IList<string> inputs)
    {
        // 简单策略：选择最长的结果（假设更详细）
        var best = inputs.OrderByDescending(r => r.Length).First();
        return $"=== 最佳方案 ===\n{best}";
    }
    
    /// <summary>
    /// 总结所有结果
    /// </summary>
    private string SummarizeResults(IList<string> inputs)
    {
        // 使用LLM总结
        var prompt = $@"
请总结以下{inputs.Count}个方案的核心内容：

{string.Join("\n\n", inputs.Select((r, i) => $"方案{i + 1}：\n{r}"))}

请提取每个方案的优点，并给出综合建议。
";
        
        return _llmClient.GetResponseAsync(prompt).Result;
    }
}
```

#### 1.3.3 配置参数

```json
{
  "workflowType": "Concurrent",
  "parameters": {
    "agents": [1, 2, 3],
    "aggregationStrategy": "Merge",
    "maxConcurrency": 5,
    "timeout": 300,
    "saveAllResults": true
  }
}
```

**聚合策略说明**：
- `Merge`：合并所有结果
- `Vote`：投票选择最佳
- `Best`：自动选择最佳
- `Summarize`：总结所有结果

#### 1.3.4 使用场景

**场景1：多方案设计**
```
3个UI设计师同时设计登录页面
  ↓
汇总3个方案
  ↓
用户选择或综合
```

**场景2：多角度分析**
```
3个分析师从不同角度分析市场
  ↓
汇总分析结果
  ↓
综合报告
```

**场景3：代码审查**
```
3个审查员同时审查代码
  ↓
汇总审查意见
  ↓
综合审查报告
```

#### 1.3.5 优缺点分析

**优点**：
- ✅ 执行速度快（并行）
- ✅ 可以获得多个方案
- ✅ 适合需要多视角的任务
- ✅ 提高决策质量

**缺点**：
- ❌ 资源消耗大（同时运行多个Agent）
- ❌ 结果聚合可能复杂
- ❌ 可能产生冲突的结果

---

### 1.4 任务移交（Handoffs）

#### 1.4.1 概念说明

**定义**：Agent之间可以相互移交任务，实现专业分工协作。

**执行流程**：
```
用户输入
  ↓
Agent1（产品经理）
  ↓ 分析需求，移交任务
  ├→ 移交给Agent2（架构师）→ 设计架构
  │   ↓ 移交任务
  │   └→ 移交给Agent3（开发工程师）→ 编码实现
  └→ 移交给Agent4（测试工程师）→ 测试验证
```

#### 1.4.2 技术实现

**使用MAF的Handoff机制**：

```csharp
/// <summary>
/// 任务移交工作流
/// </summary>
public async Task<WorkflowResult> ExecuteHandoffsWorkflowAsync(
    Dictionary<long, List<long>> handoffMap,
    string input,
    CancellationToken cancellationToken = default)
{
    // 1. 创建所有Agent
    var agents = new Dictionary<long, ChatClientAgent>();
    
    foreach (var agentId in handoffMap.Keys)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId);
        var chatClient = await CreateChatClientAsync(agent);
        agents[agentId] = new ChatClientAgent(chatClient);
    }
    
    // 2. 配置移交关系
    foreach (var (fromAgentId, toAgentIds) in handoffMap)
    {
        var fromAgent = agents[fromAgentId];
        var toAgents = toAgentIds.Select(id => agents[id]).ToList();
        
        // 配置Agent可以移交给哪些Agent
        fromAgent.ConfigureHandoffs(toAgents);
    }
    
    // 3. 从第一个Agent开始执行
    var startAgent = agents.Values.First();
    var result = await startAgent.ExecuteAsync(input, cancellationToken);
    
    return new WorkflowResult
    {
        Success = true,
        Output = result.Output,
        HandoffPath = result.HandoffPath
    };
}
```

**Agent配置移交**：

```csharp
/// <summary>
/// ChatClient Agent实现
/// </summary>
public class ChatClientAgent : Agent
{
    private readonly IChatClient _chatClient;
    private readonly List<ChatClientAgent> _handoffTargets;
    
    public ChatClientAgent(IChatClient chatClient)
    {
        _chatClient = chatClient;
        _handoffTargets = new List<ChatClientAgent>();
    }
    
    /// <summary>
    /// 配置移交目标
    /// </summary>
    public void ConfigureHandoffs(List<ChatClientAgent> targets)
    {
        _handoffTargets.AddRange(targets);
    }
    
    /// <summary>
    /// 执行任务
    /// </summary>
    [MessageHandler]
    public async Task<string> ExecuteAsync(string input)
    {
        // 构建提示词
        var systemPrompt = $@"
你是一个{AgentRole}。

你可以将任务移交给以下Agent：
{string.Join("\n", _handoffTargets.Select(a => $"- {a.AgentRole}: {a.AgentDescription}"))}

当你需要移交任务时，请使用以下格式：
[HANDOFF:Agent角色]任务描述

例如：
[HANDOFF:架构师]请设计系统架构
[HANDOFF:开发工程师]请实现登录功能
";
        
        // 调用LLM
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, input)
        };
        
        var response = await _chatClient.GetResponseAsync(messages);
        var output = response.Messages.Last().Text;
        
        // 检查是否需要移交
        var handoffMatch = Regex.Match(output, @"\[HANDOFF:(\w+)\](.+)");
        
        if (handoffMatch.Success)
        {
            var targetRole = handoffMatch.Groups[1].Value;
            var taskDescription = handoffMatch.Groups[2].Value;
            
            // 找到目标Agent
            var targetAgent = _handoffTargets.FirstOrDefault(a => 
                a.AgentRole.Equals(targetRole, StringComparison.OrdinalIgnoreCase));
            
            if (targetAgent != null)
            {
                // 移交任务
                return await targetAgent.ExecuteAsync(taskDescription);
            }
        }
        
        // 不需要移交，返回结果
        return output;
    }
}
```

#### 1.4.3 配置参数

```json
{
  "workflowType": "Handoffs",
  "parameters": {
    "startAgent": 1,
    "handoffMap": {
      "1": [2, 3],
      "2": [4, 5],
      "3": [6],
      "4": [],
      "5": [],
      "6": []
    },
    "maxHandoffs": 10,
    "timeout": 300
  }
}
```

**移交图说明**：
```
Agent1 (产品经理)
  ├→ Agent2 (架构师)
  │   ├→ Agent4 (后端工程师)
  │   └→ Agent5 (前端工程师)
  └→ Agent3 (测试工程师)
      └→ Agent6 (运维工程师)
```

#### 1.4.4 使用场景

**场景1：软件开发流程**
```
产品经理 → 架构师 → 开发工程师 → 测试工程师
```

**场景2：客户服务流程**
```
客服 → 技术支持 → 工程师 → 质检
```

**场景3：内容审核流程**
```
初审 → 复审 → 终审 → 发布
```

#### 1.4.5 优缺点分析

**优点**：
- ✅ 专业分工，各司其职
- ✅ 灵活的协作方式
- ✅ 可以处理复杂流程
- ✅ 支持动态决策

**缺点**：
- ❌ 配置复杂
- ❌ 可能出现循环移交
- ❌ 难以预测执行路径

---

### 1.5 群聊协作（GroupChat）

#### 1.5.1 概念说明

**定义**：多个Agent在群聊环境中讨论问题，共同完成任务。

**执行流程**：
```
用户输入
  ↓
群聊开始
  ↓
Agent1发言 → Agent2回复 → Agent3补充 → ...
  ↓
达成共识或达到轮数限制
  ↓
总结输出
```

#### 1.5.2 技术实现

**使用MAF的GroupChat**：

```csharp
/// <summary>
/// 群聊协作工作流
/// </summary>
public async Task<WorkflowResult> ExecuteGroupChatWorkflowAsync(
    List<long> agentIds,
    string input,
    int maxRounds = 10,
    CancellationToken cancellationToken = default)
{
    // 1. 创建所有Agent
    var agents = new List<Agent>();
    
    foreach (var agentId in agentIds)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId);
        var chatClient = await CreateChatClientAsync(agent);
        agents.Add(new ChatClientAgent(chatClient));
    }
    
    // 2. 创建群聊管理器
    var groupChat = new GroupChatManager(agents)
    {
        MaxRounds = maxRounds,
        SelectionStrategy = "RoundRobin", // 轮流发言
        TerminationCondition = "Consensus" // 达成共识终止
    };
    
    // 3. 执行群聊
    var result = await groupChat.ExecuteAsync(input, cancellationToken);
    
    return new WorkflowResult
    {
        Success = true,
        Output = result.Summary,
        ChatHistory = result.ChatHistory,
        Rounds = result.Rounds
    };
}
```

**群聊管理器实现**：

```csharp
/// <summary>
/// 群聊管理器
/// </summary>
public class GroupChatManager
{
    private readonly List<Agent> _agents;
    public int MaxRounds { get; set; } = 10;
    public string SelectionStrategy { get; set; } = "RoundRobin";
    public string TerminationCondition { get; set; } = "Consensus";
    
    public GroupChatManager(List<Agent> agents)
    {
        _agents = agents;
    }
    
    /// <summary>
    /// 执行群聊
    /// </summary>
    public async Task<GroupChatResult> ExecuteAsync(string input, CancellationToken cancellationToken)
    {
        var chatHistory = new List<ChatMessage>();
        var currentSpeaker = 0;
        var rounds = 0;
        
        // 添加用户输入
        chatHistory.Add(new ChatMessage(ChatRole.User, input));
        
        while (rounds < MaxRounds)
        {
            // 选择下一个发言者
            var speaker = SelectNextSpeaker(currentSpeaker, chatHistory);
            
            // 让Agent发言
            var response = await speaker.SpeakAsync(chatHistory, cancellationToken);
            chatHistory.Add(new ChatMessage(ChatRole.Assistant, $"[{speaker.Name}]: {response}"));
            
            // 检查终止条件
            if (ShouldTerminate(chatHistory))
            {
                break;
            }
            
            currentSpeaker = (currentSpeaker + 1) % _agents.Count;
            rounds++;
        }
        
        // 总结讨论结果
        var summary = await SummarizeDiscussionAsync(chatHistory, cancellationToken);
        
        return new GroupChatResult
        {
            Summary = summary,
            ChatHistory = chatHistory,
            Rounds = rounds
        };
    }
    
    /// <summary>
    /// 选择下一个发言者
    /// </summary>
    private Agent SelectNextSpeaker(int currentIndex, List<ChatMessage> history)
    {
        return SelectionStrategy switch
        {
            "RoundRobin" => _agents[currentIndex],
            "Random" => _agents[new Random().Next(_agents.Count)],
            "LLM" => SelectSpeakerByLLM(history),
            _ => _agents[currentIndex]
        };
    }
    
    /// <summary>
    /// 使用LLM选择发言者
    /// </summary>
    private Agent SelectSpeakerByLLM(List<ChatMessage> history)
    {
        var lastMessage = history.Last().Text;
        
        var prompt = $@"
根据最后的消息，谁应该下一个发言？

可选的Agent：
{string.Join("\n", _agents.Select(a => $"- {a.Name}: {a.Role}"))}

最后消息：{lastMessage}

请直接输出Agent的名字。
";
        
        var response = _llmClient.GetResponseAsync(prompt).Result;
        return _agents.FirstOrDefault(a => 
            a.Name.Equals(response.Trim(), StringComparison.OrdinalIgnoreCase)) 
            ?? _agents.First();
    }
    
    /// <summary>
    /// 检查是否应该终止
    /// </summary>
    private bool ShouldTerminate(List<ChatMessage> history)
    {
        var lastMessage = history.Last().Text;
        
        return TerminationCondition switch
        {
            "Consensus" => lastMessage.Contains("同意") || lastMessage.Contains("达成共识"),
            "Keywords" => lastMessage.Contains("结束") || lastMessage.Contains("完成"),
            "Manual" => false,
            _ => false
        };
    }
    
    /// <summary>
    /// 总结讨论结果
    /// </summary>
    private async Task<string> SummarizeDiscussionAsync(
        List<ChatMessage> history, 
        CancellationToken cancellationToken)
    {
        var prompt = $@"
请总结以下讨论的核心内容和结论：

{string.Join("\n", history.Select(m => m.Text))}

请输出：
1. 讨论主题
2. 主要观点
3. 达成的共识
4. 最终结论
";
        
        return await _llmClient.GetResponseAsync(prompt, cancellationToken: cancellationToken);
    }
}
```

#### 1.5.3 配置参数

```json
{
  "workflowType": "GroupChat",
  "parameters": {
    "agents": [1, 2, 3, 4],
    "maxRounds": 10,
    "selectionStrategy": "RoundRobin",
    "terminationCondition": "Consensus",
    "timeout": 600
  }
}
```

**选择策略说明**：
- `RoundRobin`：轮流发言
- `Random`：随机发言
- `LLM`：LLM智能选择

**终止条件说明**：
- `Consensus`：达成共识
- `Keywords`：关键词触发
- `Manual`：手动终止

#### 1.5.4 使用场景

**场景1：需求评审会议**
```
产品经理、架构师、开发工程师、测试工程师
  ↓
讨论需求可行性
  ↓
达成共识
```

**场景2：技术方案讨论**
```
架构师、技术专家、开发工程师
  ↓
讨论技术选型
  ↓
确定方案
```

**场景3：问题解决讨论**
```
多个专家
  ↓
讨论问题原因
  ↓
提出解决方案
```

#### 1.5.5 优缺点分析

**优点**：
- ✅ 充分讨论，集思广益
- ✅ 可以处理复杂问题
- ✅ 支持动态决策
- ✅ 模拟真实会议

**缺点**：
- ❌ 执行时间长
- ❌ 资源消耗大
- ❌ 可能无法达成共识
- ❌ 可能偏离主题

---

## 第二部分：Magentic工作流

### 2.1 Magentic工作流概念

**定义**：Magentic工作流是一种智能化的多Agent协作模式，由一个Manager Agent自动分析任务，制定执行计划，并动态选择合适的Worker Agents来执行任务。

**核心特点**：
1. **智能编排**：Manager Agent自动制定计划
2. **资源感知**：考虑当前可用资源
3. **动态调度**：根据执行情况调整
4. **人工审核**：支持人工干预和修改
5. **学习能力**：保存优秀流程作为模板

### 2.2 架构设计

```
┌─────────────────────────────────────────────────────────┐
│                   Magentic工作流架构                       │
├─────────────────────────────────────────────────────────┤
│  用户输入                                                 │
│    ↓                                                     │
│  Manager Agent                                           │
│    ├── 分析任务需求                                       │
│    ├── 查询可用员工                                       │
│    ├── 制定执行计划                                       │
│    └── 输出结构化计划                                     │
│    ↓                                                     │
│  人工审核（可选）                                          │
│    ├── 查看计划                                           │
│    ├── 修改计划                                           │
│    └── 批准执行                                           │
│    ↓                                                     │
│  执行引擎                                                 │
│    ├── 解析计划                                           │
│    ├── 分配任务                                           │
│    ├── 监控进度                                           │
│    └── 动态调整                                           │
│    ↓                                                     │
│  Worker Agents                                           │
│    ├── Agent1（执行任务1）                                │
│    ├── Agent2（执行任务2）                                │
│    └── Agent3（执行任务3）                                │
│    ↓                                                     │
│  结果汇总                                                 │
│    ↓                                                     │
│  保存模板（可选）                                          │
└─────────────────────────────────────────────────────────┘
```

### 2.3 Manager Agent设计

#### 2.3.1 Manager Agent职责

1. **任务分析**：
   - 识别任务类型
   - 分析任务需求
   - 确定需要的技能

2. **资源查询**：
   - 查询当前空闲员工
   - 分析员工技能匹配度
   - 评估员工工作效率

3. **计划制定**：
   - 制定执行步骤
   - 确定执行顺序（顺序/并发）
   - 分配任务给员工

4. **输出计划**：
   - 生成结构化JSON计划
   - 包含节点和边
   - 包含参数配置

#### 2.3.2 Manager Agent提示词设计

```csharp
/// <summary>
/// 生成Manager Agent提示词
/// </summary>
public string GenerateManagerPrompt(
    string task,
    List<Employee> allMembers,
    List<Employee> availableEmployees)
{
    return $@"
你是一个智能项目经理（Magentic Manager），负责分析任务并制定执行计划。

## 任务描述
{task}

## 项目团队
{string.Join("\n", allMembers.Select(e => 
    $"- {e.Name}（{e.Role}）: {string.Join(", ", e.Skills)} | 状态: {e.Status}"))}

## 当前可用员工
{string.Join("\n", availableEmployees.Select(e => 
    $"- {e.Name}（{e.Role}）: {string.Join(", ", e.Skills)} | 评分: {e.Rating}/5 | 效率: {e.AverageCompletionTime}小时"))}

## 你的任务
请分析任务需求，制定执行计划。注意：
1. **只能选择当前空闲的员工**
2. 如果某个角色没有空闲员工，需要等待或调整计划
3. 考虑员工的技能匹配度和效率
4. 合理安排执行顺序（顺序或并发）

## 输出格式
请输出JSON格式的执行计划：

```json
{{
  ""analysis"": ""任务分析..."",
  ""nodes"": [
    {{
      ""id"": ""node-1"",
      ""type"": ""agent"",
      ""name"": ""任务名称"",
      ""employeeId"": {availableEmployees.First().Id},
      ""employeeName"": ""{availableEmployees.First().Name}"",
      ""inputTemplate"": ""任务描述""
    }}
  ],
  ""edges"": [
    {{
      ""type"": ""sequential"",
      ""from"": ""start"",
      ""to"": ""node-1""
    }}
  ],
  ""executionMode"": ""sequential"",
  ""estimatedTime"": ""预计时间"",
  ""risks"": [""风险1"", ""风险2""]
}}
```

## 注意事项
- type可以是：start、agent、aggregator、condition
- edge type可以是：sequential、fan-out、fan-in、conditional
- 如果需要并发执行，使用fan-out和fan-in
- 如果有条件分支，使用conditional
- 请确保计划合理且可执行

请直接输出JSON，不要有其他内容。
";
}
```

#### 2.3.3 Manager Agent执行

```csharp
/// <summary>
/// Manager Agent生成计划
/// </summary>
public async Task<WorkflowDefinitionDto> GenerateMagenticPlanAsync(
    long collaborationId,
    string task,
    CancellationToken cancellationToken = default)
{
    // 1. 获取协作的所有成员
    var allMembers = await _employeeRepository.GetByCollaborationIdAsync(collaborationId);
    
    // 2. 获取当前空闲的员工
    var availableEmployees = allMembers.Where(e => e.Status == "Idle").ToList();
    
    // 3. 获取Manager Agent（第一个员工）
    var managerEmployee = allMembers.First();
    var managerClient = await CreateChatClientAsync(managerEmployee);
    
    // 4. 生成提示词
    var prompt = GenerateManagerPrompt(task, allMembers, availableEmployees);
    
    // 5. 调用Manager Agent
    var messages = new List<ChatMessage>
    {
        new(ChatRole.System, "你是一个智能项目经理，负责制定执行计划。"),
        new(ChatRole.User, prompt)
    };
    
    var response = await managerClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
    var planJson = response.Messages.Last().Text;
    
    // 6. 解析JSON计划
    var workflow = JsonSerializer.Deserialize<WorkflowDefinitionDto>(planJson);
    
    // 7. 验证计划
    ValidateWorkflow(workflow, availableEmployees);
    
    return workflow;
}
```

### 2.4 执行引擎设计

#### 2.4.1 工作流解析

```csharp
/// <summary>
/// 解析工作流定义
/// </summary>
public async Task<WorkflowExecutionPlan> ParseWorkflowAsync(
    WorkflowDefinitionDto workflow,
    List<Employee> availableEmployees)
{
    var plan = new WorkflowExecutionPlan();
    
    // 1. 解析节点
    foreach (var node in workflow.Nodes)
    {
        if (node.Type == "agent")
        {
            var employee = availableEmployees.FirstOrDefault(e => e.Id == node.EmployeeId);
            if (employee == null)
            {
                throw new InvalidOperationException($"员工 {node.EmployeeId} 不可用");
            }
            
            plan.Steps.Add(new ExecutionStep
            {
                NodeId = node.Id,
                EmployeeId = employee.Id,
                EmployeeName = employee.Name,
                TaskDescription = ApplyTemplate(node.InputTemplate, workflow.Parameters),
                Type = StepType.Agent
            });
        }
        else if (node.Type == "aggregator")
        {
            plan.Steps.Add(new ExecutionStep
            {
                NodeId = node.Id,
                Type = StepType.Aggregator
            });
        }
    }
    
    // 2. 解析边
    foreach (var edge in workflow.Edges)
    {
        plan.Edges.Add(new ExecutionEdge
        {
            Type = edge.Type,
            From = edge.From,
            To = edge.To,
            Condition = edge.Condition
        });
    }
    
    return plan;
}
```

#### 2.4.2 工作流执行

```csharp
/// <summary>
/// 执行工作流
/// </summary>
public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
    WorkflowExecutionPlan plan,
    string input,
    CancellationToken cancellationToken = default)
{
    var context = new WorkflowExecutionContext
    {
        Input = input,
        Results = new Dictionary<string, string>(),
        Status = new Dictionary<string, StepStatus>()
    };
    
    // 1. 找到起始节点
    var startEdge = plan.Edges.FirstOrDefault(e => e.From == "start");
    if (startEdge == null)
    {
        throw new InvalidOperationException("找不到起始节点");
    }
    
    // 2. 执行工作流
    await ExecuteFromNodeAsync(startEdge.To, plan, context, cancellationToken);
    
    // 3. 返回结果
    return new WorkflowExecutionResult
    {
        Success = true,
        Output = context.Results.Values.Last(),
        ExecutionPath = context.ExecutionPath,
        StepResults = context.Results
    };
}

/// <summary>
/// 从指定节点开始执行
/// </summary>
private async Task ExecuteFromNodeAsync(
    string nodeId,
    WorkflowExecutionPlan plan,
    WorkflowExecutionContext context,
    CancellationToken cancellationToken)
{
    var step = plan.Steps.FirstOrDefault(s => s.NodeId == nodeId);
    if (step == null) return;
    
    // 检查状态
    if (context.Status.ContainsKey(nodeId) && context.Status[nodeId] == StepStatus.Completed)
    {
        return;
    }
    
    // 执行步骤
    if (step.Type == StepType.Agent)
    {
        // 获取员工
        var employee = await _employeeRepository.GetByIdAsync(step.EmployeeId);
        
        // 更新员工状态
        employee.Status = "Busy";
        await _employeeRepository.UpdateAsync(employee);
        
        try
        {
            // 执行任务
            var chatClient = await CreateChatClientAsync(employee);
            var result = await chatClient.GetResponseAsync(
                new[] { new ChatMessage(ChatRole.User, step.TaskDescription) },
                cancellationToken: cancellationToken);
            
            context.Results[nodeId] = result.Messages.Last().Text;
            context.Status[nodeId] = StepStatus.Completed;
            
            // 记录工作经历
            await RecordWorkExperienceAsync(employee, step, result.Messages.Last().Text);
        }
        finally
        {
            // 恢复员工状态
            employee.Status = "Idle";
            await _employeeRepository.UpdateAsync(employee);
        }
    }
    else if (step.Type == StepType.Aggregator)
    {
        // 汇总结果
        var inputs = context.Results.Values.ToList();
        var aggregated = await AggregateResultsAsync(inputs, cancellationToken);
        context.Results[nodeId] = aggregated;
        context.Status[nodeId] = StepStatus.Completed;
    }
    
    // 查找下一个节点
    var nextEdges = plan.Edges.Where(e => e.From == nodeId).ToList();
    
    if (nextEdges.Any())
    {
        if (nextEdges.Count == 1)
        {
            // 顺序执行
            await ExecuteFromNodeAsync(nextEdges[0].To, plan, context, cancellationToken);
        }
        else
        {
            // 并发执行
            var tasks = nextEdges.Select(e => 
                ExecuteFromNodeAsync(e.To, plan, context, cancellationToken));
            await Task.WhenAll(tasks);
        }
    }
}
```

### 2.5 人工审核机制

#### 2.5.1 审核流程

```
Manager Agent生成计划
  ↓
保存计划到数据库
  ↓
通知用户审核
  ↓
用户查看计划
  ↓
用户选择：
  ├── [直接执行] → 开始执行
  ├── [修改计划] → 编辑计划 → 执行
  └── [保存模板] → 保存为模板
```

#### 2.5.2 审核界面设计

```
┌─────────────────────────────────────────────────────────┐
│  📋 Magentic工作流审核                                    │
├─────────────────────────────────────────────────────────┤
│  任务：开发用户登录功能                                    │
│  生成时间：2024-01-15 10:30:00                           │
├─────────────────────────────────────────────────────────┤
│  📊 任务分析                                              │
│  该任务需要开发一个完整的用户登录功能，包括数据库设计、     │
│  后端API开发和前端页面开发。建议采用顺序执行模式。          │
├─────────────────────────────────────────────────────────┤
│  📝 执行计划                                              │
│  ┌─────────────────────────────────────────────────┐   │
│  │ 步骤1：数据库设计                                 │   │
│  │ 执行者：张三（架构师）                            │   │
│  │ 任务：设计用户登录功能的数据库表结构              │   │
│  │ 预计时间：2小时                                   │   │
│  ├─────────────────────────────────────────────────┤   │
│  │ 步骤2：后端API开发                                │   │
│  │ 执行者：李四（后端工程师）                        │   │
│  │ 任务：开发用户登录的后端API接口                   │   │
│  │ 预计时间：4小时                                   │   │
│  ├─────────────────────────────────────────────────┤   │
│  │ 步骤3：前端页面开发                               │   │
│  │ 执行者：王五（前端工程师）                        │   │
│  │ 任务：开发用户登录的前端页面                      │   │
│  │ 预计时间：3小时                                   │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  执行模式：顺序执行                                      │
│  预计总时间：9小时                                       │
├─────────────────────────────────────────────────────────┤
│  ⚠️ 风险提示                                              │
│  • 李四当前评分较低（3.2/5），可能影响质量                 │
│  • 王五刚入职，经验不足                                   │
├─────────────────────────────────────────────────────────┤
│  [直接执行] [修改计划] [保存为模板] [取消]                 │
└─────────────────────────────────────────────────────────┘
```

#### 2.5.3 修改计划功能

```typescript
// 前端：修改计划界面
const WorkflowEditor: React.FC<{ workflow: WorkflowDefinition }> = ({ workflow }) => {
  const [nodes, setNodes] = useState(workflow.nodes);
  const [edges, setEdges] = useState(workflow.edges);
  
  const handleNodeUpdate = (nodeId: string, updates: Partial<WorkflowNode>) => {
    setNodes(nodes.map(n => n.id === nodeId ? { ...n, ...updates } : n));
  };
  
  const handleAddNode = (type: NodeType) => {
    const newNode: WorkflowNode = {
      id: `node-${Date.now()}`,
      type,
      name: getDefaultNodeName(type),
    };
    setNodes([...nodes, newNode]);
  };
  
  const handleSave = async () => {
    const updatedWorkflow = { ...workflow, nodes, edges };
    await workflowApi.update(workflow.id, updatedWorkflow);
    message.success('计划已更新');
  };
  
  return (
    <div className="workflow-editor">
      <div className="toolbar">
        <Button onClick={() => handleAddNode('agent')}>添加Agent节点</Button>
        <Button onClick={() => handleAddNode('aggregator')}>添加聚合节点</Button>
        <Button onClick={() => handleAddNode('condition')}>添加条件节点</Button>
      </div>
      
      <ReactFlow nodes={nodes} edges={edges}>
        {/* 自定义节点和边 */}
      </ReactFlow>
      
      <div className="actions">
        <Button onClick={handleSave}>保存修改</Button>
        <Button type="primary" onClick={handleExecute}>执行计划</Button>
      </div>
    </div>
  );
};
```

---

## 第三部分：工作流模板系统

（由于篇幅限制，后续部分将在下一个文档中继续...）

### 3.1 模板系统概述

工作流模板系统允许用户保存、管理和复用工作流定义，支持：
- 模板创建和编辑
- 模板分类和搜索
- 参数化配置
- 使用统计
- Magentic学习

### 3.2 数据库设计

```sql
CREATE TABLE workflow_templates (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    category VARCHAR(100),
    tags JSONB DEFAULT '[]'::jsonb,
    workflow_definition JSONB NOT NULL,
    parameters JSONB DEFAULT '{}'::jsonb,
    created_by BIGINT,
    is_public BOOLEAN DEFAULT false,
    usage_count INTEGER DEFAULT 0,
    source VARCHAR(50) DEFAULT 'manual',
    original_task TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

---

## 总结

本文档详细介绍了MAF Studio的核心技术方案，包括：

1. **Agent协作基础**：四种基本协作模式的详细设计和实现
2. **Magentic工作流**：智能编排机制的完整架构
3. **工作流模板系统**：复用和学习机制的设计

后续文档将继续介绍：
- AI公司系统完整设计
- 人才市场机制
- 员工管理系统
- 商业模式
- 实施计划

这个方案将MAF Studio从一个简单的多智能体协作平台升级为一个具有真实感和趣味性的AI公司经营平台！🚀
