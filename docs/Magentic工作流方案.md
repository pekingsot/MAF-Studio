# Magentic 工作流方案

## 概述

Magentic 工作流是一种智能化的多Agent协作模式，由一个 Manager Agent 自动分析任务，制定执行计划，并动态选择合适的 Worker Agents 来执行任务。该方案支持人工审核和修改，能够保存优秀的流程作为模板，实现自动学习和优化。

## 核心概念

### 1. Manager Agent（管理者Agent）

Manager Agent 是整个 Magentic 工作流的核心，负责：
- 分析任务需求
- 制定执行计划
- 选择合适的 Worker Agents
- 决定执行顺序（顺序/并发）
- 监控任务进度
- 调整执行策略

### 2. Worker Agents（工作者Agents）

Worker Agents 是执行具体任务的智能体，每个 Worker Agent 都有自己的专长领域：
- 代码开发
- 文档编写
- UI设计
- 测试
- 审阅
- 等等...

### 3. 结构化计划（Structured Plan）

Manager Agent 生成的执行计划是结构化的，包含：
- **节点（Nodes）**: 工作流的各个步骤
- **边（Edges）**: 节点之间的连接关系
- **参数（Parameters）**: 节点的配置参数

## 工作流程

### 1. 任务输入

用户输入任务描述，例如：
```
开发一个用户登录功能，包括前端页面、后端API和数据库设计
```

### 2. Manager 分析任务

Manager Agent 分析任务，识别：
- 任务类型（开发、设计、文档等）
- 需要的技能
- 任务依赖关系
- 执行顺序

### 3. 生成执行计划

Manager Agent 生成结构化的执行计划：

```json
{
  "nodes": [
    {
      "id": "start",
      "type": "start",
      "name": "开始"
    },
    {
      "id": "node-1",
      "type": "agent",
      "agentId": "agent-001",
      "name": "数据库设计",
      "inputTemplate": "设计用户登录功能的数据库表结构"
    },
    {
      "id": "node-2",
      "type": "agent",
      "agentId": "agent-002",
      "name": "后端API开发",
      "inputTemplate": "开发用户登录的后端API接口"
    },
    {
      "id": "node-3",
      "type": "agent",
      "agentId": "agent-003",
      "name": "前端页面开发",
      "inputTemplate": "开发用户登录的前端页面"
    },
    {
      "id": "node-4",
      "type": "aggregator",
      "name": "结果汇总"
    }
  ],
  "edges": [
    {
      "type": "sequential",
      "from": "start",
      "to": "node-1"
    },
    {
      "type": "sequential",
      "from": "node-1",
      "to": "node-2"
    },
    {
      "type": "fan-out",
      "from": "node-2",
      "to": ["node-3"]
    },
    {
      "type": "fan-in",
      "from": ["node-3"],
      "to": "node-4"
    }
  ]
}
```

### 4. 人工审核（可选）

生成的计划可以提交给人工审核：
- 查看可视化流程图
- 修改节点配置
- 调整执行顺序
- 添加/删除节点
- 替换Agent

### 5. 执行工作流

审核通过后，执行工作流：
- 按照计划依次执行各个节点
- 实时显示执行进度
- 记录执行日志
- 处理异常情况

### 6. 保存模板（可选）

如果工作流执行效果好，可以保存为模板：
- 保存工作流定义
- 记录任务类型和标签
- 统计使用次数
- 用于下次类似任务

## 节点类型

### 1. Start 节点
- 工作流的起始点
- 只有一个输出边

### 2. Agent 节点
- 执行具体任务的Agent
- 配置Agent ID和输入模板
- 可以有多个输入和输出边

### 3. Aggregator 节点
- 汇聚多个节点的结果
- 用于并发执行后的结果合并
- 可以有多个输入边

### 4. Condition 节点
- 条件判断节点
- 根据条件选择不同的执行路径
- 支持多个输出边

### 5. Loop 节点
- 循环节点
- 支持迭代执行
- 直到满足退出条件

## 边类型

### 1. Sequential（顺序）
- 顺序执行
- 前一个节点完成后执行下一个节点

### 2. Fan-Out（扇出）
- 并发执行
- 从一个节点分发到多个节点同时执行

### 3. Fan-In（扇入）
- 结果汇聚
- 多个节点的结果汇聚到一个节点

### 4. Conditional（条件）
- 条件分支
- 根据条件选择执行路径

### 5. Loop（循环）
- 循环执行
- 直到满足退出条件

## 技术实现

### 后端实现

#### 1. Manager Agent

```csharp
public async Task<WorkflowDefinitionDto> GenerateMagenticPlanAsync(
    long collaborationId,
    string task,
    CancellationToken cancellationToken = default)
{
    var chatClients = await GetAgentsAsync(collaborationId);
    var managerClient = chatClients[0]; // 第一个Agent是Manager
    
    // 构建Manager的提示词
    var managerPrompt = $@"
你是一个智能工作流协调者（Magentic Manager），负责分析任务并制定执行计划。

任务：{task}

可用的Worker Agents：
{string.Join("\n", workerDescriptions)}

请分析任务，制定执行计划。输出格式为JSON...
";
    
    // Manager生成计划
    var managerResponse = await managerClient.GetResponseAsync(
        new[] { new ChatMessage(ChatRole.User, managerPrompt) },
        cancellationToken: cancellationToken);
    
    // 解析JSON计划
    var workflow = ParseWorkflowFromJson(managerOutput);
    
    return workflow;
}
```

#### 2. 工作流执行引擎

```csharp
public async Task<CollaborationResult> ExecuteCustomWorkflowAsync(
    long collaborationId,
    WorkflowDefinitionDto workflow,
    string input,
    CancellationToken cancellationToken = default)
{
    // 构建节点映射
    var nodeMap = BuildNodeMap(workflow);
    
    // 从Start节点开始执行
    var startNode = workflow.Nodes.First(n => n.Type == "start");
    
    await ExecuteNodeAsync(
        startNode,
        workflow,
        nodeMap,
        executedNodes,
        nodeResults,
        input,
        collaborationId,
        messages,
        cancellationToken);
    
    return new CollaborationResult
    {
        Success = true,
        Output = nodeResults.Values.Last(),
        Messages = messages
    };
}
```

### 前端实现

#### 1. Magentic 工作流页面

```tsx
const MagenticWorkflow: React.FC = () => {
  const [workflow, setWorkflow] = useState<WorkflowDefinition | null>(null);
  
  // 生成Magentic计划
  const handleGenerate = async (values: any) => {
    const result = await workflowTemplateApi.generateMagenticPlan({
      collaborationId: values.collaborationId,
      task: values.task,
    });
    
    if (result.success && result.workflow) {
      setWorkflow(result.workflow);
      renderWorkflow(result.workflow);
      setReviewModalVisible(true);
    }
  };
  
  // 渲染工作流
  const renderWorkflow = (workflow: WorkflowDefinition) => {
    const nodes = workflow.nodes.map((node, index) => ({
      id: node.id,
      type: node.type,
      position: { x: 250, y: index * 150 },
      data: node,
    }));
    
    const edges = workflow.edges.map((edge, index) => ({
      id: `edge-${index}`,
      source: edge.from,
      target: Array.isArray(edge.to) ? edge.to[0] : edge.to,
      type: 'custom',
      data: { type: edge.type },
    }));
    
    setNodes(nodes);
    setEdges(edges);
  };
  
  return (
    <div>
      {/* 任务输入表单 */}
      <Form onFinish={handleGenerate}>
        <Form.Item name="task" label="任务描述">
          <Input.TextArea rows={4} />
        </Form.Item>
        <Button type="primary" htmlType="submit">
          生成计划
        </Button>
      </Form>
      
      {/* 工作流预览 */}
      <ReactFlow nodes={nodes} edges={edges} />
      
      {/* 审核和执行按钮 */}
      <Space>
        <Button onClick={handleSave}>保存为模板</Button>
        <Button type="primary" onClick={handleExecute}>执行工作流</Button>
      </Space>
    </div>
  );
};
```

## 使用场景

### 1. 软件开发
- 需求分析 → 设计 → 开发 → 测试 → 部署
- Manager 自动识别开发流程，选择合适的开发、测试、运维 Agent

### 2. 文档编写
- 资料收集 → 大纲设计 → 内容编写 → 审阅修改 → 发布
- Manager 自动分配写作、审阅、编辑 Agent

### 3. 数据分析
- 数据收集 → 数据清洗 → 数据分析 → 报告生成
- Manager 自动选择数据工程师、分析师、可视化 Agent

### 4. 项目管理
- 任务分解 → 任务分配 → 进度跟踪 → 风险评估
- Manager 自动协调项目经理、开发、测试 Agent

## 优势

### 1. 智能化
- 自动分析任务
- 自动选择Agent
- 自动制定计划

### 2. 灵活性
- 支持人工审核和修改
- 支持保存和复用
- 支持动态调整

### 3. 可视化
- 流程图可视化
- 实时预览
- 直观易懂

### 4. 学习能力
- 保存优秀流程
- 自动匹配相似任务
- 持续优化

## 未来扩展

### 1. 多Manager协作
- 支持多个Manager Agent协作
- 处理更复杂的任务

### 2. 动态Agent创建
- 根据任务需求动态创建Agent
- 自动配置Agent参数

### 3. 性能优化
- 学习最优执行策略
- 自动调整并发度
- 资源调度优化

### 4. 异常处理
- 自动识别失败节点
- 自动重试和降级
- 人工介入机制

## 总结

Magentic 工作流是一种创新的多Agent协作模式，通过 Manager Agent 的智能编排，实现了任务的自动化分解、分配和执行。结合人工审核和模板保存机制，系统能够不断学习和优化，为用户提供越来越好的协作体验。
