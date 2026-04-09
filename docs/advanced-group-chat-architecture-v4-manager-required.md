# MAF高级群聊系统架构设计文档 v4.0（协调者必选版）

## 📋 核心理解纠正

### ❌ 之前的错误理解

1. **Magentic模式自动创建协调者**：错误！
2. **所有Agent都是Worker**：错误！
3. **不需要指定Manager Agent**：错误！

### ✅ 正确的架构理解

1. **协调者是必须的**：
   - GroupChat需要协调者
   - Magentic也需要协调者
   - 协调者是核心角色，不是可选的

2. **协调者的角色定位**：
   - 角色是**Manager**
   - **不参与干活**（不执行具体业务任务）
   - 只负责**维护发言顺序和流转**
   - **提示词非常重要**，影响流转逻辑

3. **Worker的角色定位**：
   - 角色是**Worker**
   - 负责执行具体任务
   - 是干活的人

---

## 🏗️ 正确的架构设计

### 1. Agent角色体系

```
┌─────────────────────────────────────────────────────┐
│                  Agent角色体系                       │
└─────────────────────────────────────────────────────┘

┌──────────────────┐          ┌──────────────────┐
│  Manager Agent   │          │  Worker Agent    │
│  （协调者）       │          │  （执行者）       │
├──────────────────┤          ├──────────────────┤
│ • 不参与干活     │          │ • 执行具体任务   │
│ • 维护发言顺序   │          │ • 负责业务逻辑   │
│ • 控制流转逻辑   │          │ • 调用工具       │
│ • 提示词影响流转 │          │ • 产出结果       │
└──────────────────┘          └──────────────────┘
```

### 2. 完整流程

```
┌─────────────────────────────────────────────────────┐
│  Step 1: 新建Agent                                  │
└─────────────────────────────────────────────────────┘
选择角色：
├─ Manager（协调者）
│  └─ 特点：不干活，只维护发言
│  └─ 提示词：控制流转逻辑
│
└─ Worker（执行者）
   └─ 特点：执行具体任务
   └─ 提示词：业务逻辑

                ↓

┌─────────────────────────────────────────────────────┐
│  Step 2: 新建任务                                    │
└─────────────────────────────────────────────────────┘
必选项：
├─ 选择协调者（Manager角色的Agent）⭐ 必选
│  └─ 可以修改协调者提示词（影响流转）
│
└─ 选择Worker Agents（Worker角色的Agent）
   └─ 至少选择1个

                ↓

┌─────────────────────────────────────────────────────┐
│  Step 3: 执行任务                                    │
└─────────────────────────────────────────────────────┘
├─ GroupChat模式
│  └─ 使用Manager Agent作为协调者
│  └─ Manager控制发言顺序
│  └─ Workers执行任务
│
└─ Magentic模式
   └─ 使用Manager Agent作为协调者
   └─ Manager维护任务账本和进度账本
   └─ Workers执行任务
```

---

## 🎯 协调者提示词的重要性

### 1. 提示词影响流转逻辑

**示例提示词（GroupChat协调者）**：

```
你是一个群聊协调者，负责引导讨论。

你的职责：
1. 分析当前讨论内容
2. 决定下一个发言的Agent
3. 确保讨论不偏离主题

发言选择策略：
- 如果讨论到"安全"问题，请让"安全专家"发言
- 如果讨论到"代码"实现，请让"程序员"发言
- 如果讨论到"架构"设计，请让"架构师"发言
- 如果需要总结，请让"项目经理"发言

重要规则：
- 每次只选择一个Agent发言
- 避免重复选择同一个Agent
- 如果发现讨论陷入僵局，请主动引导方向
```

**示例提示词（Magentic协调者）**：

```
你是一个Magentic协调者，负责动态规划和调度。

你的职责：
1. 维护任务账本（Task Ledger）
   - 全局目标
   - 已知事实
   - 总体规划
   - 任务边界

2. 维护进度账本（Progress Ledger）
   - 当前步骤目标
   - 已完成步骤
   - 反思结果
   - 下一步行动

3. 动态选择Worker
   - 根据当前进度选择最合适的Worker
   - 分析Worker的产出是否达标
   - 决定是否需要调整计划

重要规则：
- 你不直接执行任务，只负责协调
- 每次只委托给一个Worker
- 根据Worker的反馈动态调整计划
- 如果Worker执行失败，尝试其他Worker或修改任务
```

### 2. 提示词模板系统

```sql
CREATE TABLE manager_prompt_templates (
    id BIGSERIAL PRIMARY KEY,
    template_name VARCHAR(200) NOT NULL,
    template_category VARCHAR(100),  -- groupchat, magentic, custom
    
    -- 提示词内容
    prompt_content TEXT NOT NULL,
    
    -- 变量占位符
    variables TEXT,  -- JSON数组：["agents", "task", "max_iterations"]
    
    -- 元数据
    description TEXT,
    is_builtin BOOLEAN DEFAULT false,
    usage_count INT DEFAULT 0,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

**内置模板**：

```sql
-- GroupChat协调者模板
INSERT INTO manager_prompt_templates (template_name, template_category, prompt_content, is_builtin) VALUES
('GroupChat标准协调者', 'groupchat', 
'你是一个群聊协调者，负责引导讨论。

可用的团队成员：
{{agents}}

当前任务：{{task}}

你的职责：
1. 分析当前讨论内容
2. 决定下一个发言的Agent
3. 确保讨论在{{max_iterations}}轮内完成

请开始协调！', true);

-- Magentic协调者模板
INSERT INTO manager_prompt_templates (template_name, template_category, prompt_content, is_builtin) VALUES
('Magentic智能协调者', 'magentic', 
'你是一个Magentic协调者，负责动态规划和调度。

可用的团队成员：
{{agents}}

当前任务：{{task}}

你的职责：
1. 维护任务账本（Task Ledger）
2. 维护进度账本（Progress Ledger）
3. 动态选择Worker执行任务

最大迭代次数：{{max_iterations}}

请开始规划！', true);
```

---

## 📊 数据库设计

### 1. Agent表（已有，保留角色字段）

```sql
-- agents表已有role字段，保留
ALTER TABLE agents ADD COLUMN IF NOT EXISTS role VARCHAR(50) DEFAULT 'Worker';
-- role: Manager, Worker
```

### 2. 任务配置表（更新）

```sql
CREATE TABLE task_configs (
    id BIGSERIAL PRIMARY KEY,
    task_id BIGINT NOT NULL,
    
    -- 协调者配置（必选）
    manager_agent_id BIGINT NOT NULL,  -- 协调者Agent ID（必选）
    manager_custom_prompt TEXT,  -- 自定义协调者提示词（可选）
    manager_prompt_template_id BIGINT,  -- 提示词模板ID（可选）
    
    -- 工作流配置
    workflow_type VARCHAR(50) DEFAULT 'GroupChat',  -- GroupChat, Magentic
    max_iterations INT DEFAULT 10,
    max_attempts INT DEFAULT 5,
    
    -- 显示控制
    visibility_level VARCHAR(50) DEFAULT 'Hidden',
    show_task_ledger BOOLEAN DEFAULT false,
    show_progress_ledger BOOLEAN DEFAULT false,
    
    -- 语义规则（可选）
    enable_semantic_rules BOOLEAN DEFAULT false,
    
    -- 人在回路（可选）
    enable_hitl BOOLEAN DEFAULT false,
    hitl_trigger_points TEXT,  -- JSON数组
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_task FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
    CONSTRAINT fk_manager_agent FOREIGN KEY (manager_agent_id) REFERENCES agents(id),
    CONSTRAINT fk_prompt_template FOREIGN KEY (manager_prompt_template_id) 
        REFERENCES manager_prompt_templates(id)
);
```

### 3. 协作Agent表（保留角色字段）

```sql
-- collaboration_agents表已有role字段，保留
-- role: Manager, Worker
```

---

## 🎨 前端UI设计

### 1. 新建Agent界面（保留角色选择）

```tsx
<Form form={form} layout="vertical">
  <Form.Item label="Agent名称" name="name" required>
    <Input placeholder="如：安全专家" />
  </Form.Item>
  
  <Form.Item label="Agent角色" name="role" required initialValue="Worker">
    <Radio.Group>
      <Space direction="vertical">
        <Radio value="Manager">
          <Space>
            <CrownOutlined style={{ color: '#faad14' }} />
            <span>Manager（协调者）</span>
            <Tag color="gold">不干活，只维护发言</Tag>
          </Space>
        </Radio>
        <Radio value="Worker">
          <Space>
            <UserOutlined style={{ color: '#1890ff' }} />
            <span>Worker（执行者）</span>
            <Tag color="blue">执行具体任务</Tag>
          </Space>
        </Radio>
      </Space>
    </Radio.Group>
  </Form.Item>
  
  <Alert
    message="角色说明"
    description={
      <div>
        <p><strong>Manager（协调者）：</strong></p>
        <ul>
          <li>不参与具体任务执行</li>
          <li>负责维护发言顺序和流转</li>
          <li>提示词影响整个工作流的流转逻辑</li>
        </ul>
        <p><strong>Worker（执行者）：</strong></p>
        <ul>
          <li>负责执行具体业务任务</li>
          <li>调用工具完成工作</li>
          <li>产出最终结果</li>
        </ul>
      </div>
    }
    type="info"
    showIcon
  />
  
  <Form.Item label="系统提示词" name="systemPrompt">
    <Input.TextArea 
      rows={6} 
      placeholder={
        form.getFieldValue('role') === 'Manager'
          ? "作为协调者，你的提示词将影响整个工作流的流转逻辑..."
          : "作为执行者，你的提示词定义了你的专业能力和职责..."
      }
    />
  </Form.Item>
</Form>
```

### 2. 新建任务界面（协调者必选）

```tsx
<Form form={form} layout="vertical">
  <Form.Item label="任务标题" name="title" required>
    <Input placeholder="请输入任务标题" />
  </Form.Item>
  
  {/* 协调者选择（必选）⭐ */}
  <Form.Item 
    label={
      <Space>
        <CrownOutlined style={{ color: '#faad14' }} />
        <span>协调者</span>
        <Tag color="red">必选</Tag>
      </Space>
    }
    name="managerAgentId"
    rules={[{ required: true, message: '请选择协调者' }]}
  >
    <Select 
      placeholder="请选择Manager角色的Agent作为协调者"
      showSearch
      filterOption={(input, option) =>
        (option?.children as unknown as string)?.toLowerCase().includes(input.toLowerCase())
      }
    >
      {agents
        .filter(agent => agent.role === 'Manager')
        .map(agent => (
          <Option key={agent.id} value={agent.id}>
            <Space>
              <CrownOutlined style={{ color: '#faad14' }} />
              {agent.name}
              <Tag color="gold">Manager</Tag>
            </Space>
          </Option>
        ))
      }
    </Select>
  </Form.Item>
  
  {/* 协调者提示词编辑（重要！） */}
  {selectedManagerAgent && (
    <Form.Item label="协调者提示词（影响流转逻辑）">
      <Space direction="vertical" style={{ width: '100%' }}>
        <Alert
          message="提示词重要性"
          description="协调者的提示词将直接影响工作流的流转逻辑，请仔细编写！"
          type="warning"
          showIcon
        />
        
        {/* 提示词模板选择 */}
        <Form.Item name="managerPromptTemplateId">
          <Select 
            placeholder="选择提示词模板（可选）"
            allowClear
            onChange={handleTemplateChange}
          >
            {promptTemplates.map(template => (
              <Option key={template.id} value={template.id}>
                {template.templateName}
              </Option>
            ))}
          </Select>
        </Form.Item>
        
        {/* 自定义提示词 */}
        <Form.Item name="managerCustomPrompt">
          <Input.TextArea
            rows={10}
            placeholder="自定义协调者提示词，将覆盖模板和Agent默认提示词..."
            value={managerCustomPrompt}
            onChange={(e) => setManagerCustomPrompt(e.target.value)}
          />
        </Form.Item>
        
        {/* 提示词预览 */}
        <Collapse>
          <Panel header="预览最终提示词" key="1">
            <pre style={{ whiteSpace: 'pre-wrap', fontSize: 12 }}>
              {finalManagerPrompt}
            </pre>
          </Panel>
        </Collapse>
      </Space>
    </Form.Item>
  )}
  
  {/* Worker选择 */}
  <Form.Item label="选择执行者">
    <Transfer
      dataSource={agents
        .filter(agent => agent.role === 'Worker')
        .map(agent => ({
          key: agent.id.toString(),
          title: agent.name,
        }))
      }
      titles={['可选Worker', '已选Worker']}
      targetKeys={selectedWorkers}
      onChange={setSelectedWorkers}
    />
  </Form.Item>
  
  {/* 工作流类型 */}
  <Form.Item label="工作流类型">
    <Radio.Group value={workflowType}>
      <Radio value="GroupChat">
        <Space>
          <TeamOutlined />
          <span>群聊协作</span>
        </Space>
      </Radio>
      <Radio value="Magentic">
        <Space>
          <BulbOutlined />
          <span>Magentic智能工作流</span>
        </Space>
      </Radio>
    </Radio.Group>
  </Form.Item>
  
  {/* 其他配置... */}
</Form>
```

---

## 🔧 后端实现

### 1. 任务创建服务

```csharp
public class TaskService
{
    public async Task<Task> CreateTaskAsync(CreateTaskRequest request)
    {
        // 1. 验证协调者
        var managerAgent = await _agentRepository.GetByIdAsync(request.ManagerAgentId);
        if (managerAgent == null)
        {
            throw new BusinessException("协调者不存在");
        }
        
        if (managerAgent.Role != "Manager")
        {
            throw new BusinessException("协调者必须是Manager角色的Agent");
        }
        
        // 2. 验证Workers
        if (request.WorkerAgentIds == null || request.WorkerAgentIds.Count == 0)
        {
            throw new BusinessException("至少需要选择一个Worker");
        }
        
        var workers = await _agentRepository.GetByIdsAsync(request.WorkerAgentIds);
        if (workers.Any(w => w.Role != "Worker"))
        {
            throw new BusinessException("执行者必须是Worker角色的Agent");
        }
        
        // 3. 构建协调者提示词
        var managerPrompt = await BuildManagerPromptAsync(
            request.ManagerAgentId,
            request.ManagerPromptTemplateId,
            request.ManagerCustomPrompt,
            workers
        );
        
        // 4. 创建任务
        var task = new Task
        {
            Title = request.Title,
            Description = request.Description,
            CollaborationId = request.CollaborationId,
            Status = TaskStatus.Pending
        };
        
        task = await _taskRepository.CreateAsync(task);
        
        // 5. 创建任务配置
        var config = new TaskConfig
        {
            TaskId = task.Id,
            ManagerAgentId = request.ManagerAgentId,
            ManagerCustomPrompt = managerPrompt,
            WorkflowType = request.WorkflowType,
            MaxIterations = request.MaxIterations ?? 10,
            // ...
        };
        
        await _taskConfigRepository.CreateAsync(config);
        
        return task;
    }
    
    private async Task<string> BuildManagerPromptAsync(
        long managerAgentId,
        long? templateId,
        string? customPrompt,
        List<Agent> workers)
    {
        // 优先级：自定义提示词 > 模板 > Agent默认提示词
        
        if (!string.IsNullOrEmpty(customPrompt))
        {
            return customPrompt;
        }
        
        if (templateId.HasValue)
        {
            var template = await _promptTemplateRepository.GetByIdAsync(templateId.Value);
            if (template != null)
            {
                // 替换变量
                var prompt = template.PromptContent
                    .Replace("{{agents}}", string.Join(", ", workers.Select(w => w.Name)))
                    .Replace("{{task}}", "{{task}}")  // 任务在执行时替换
                    .Replace("{{max_iterations}}", "10");
                
                return prompt;
            }
        }
        
        // 使用Agent默认提示词
        var managerAgent = await _agentRepository.GetByIdAsync(managerAgentId);
        return managerAgent?.SystemPrompt ?? "";
    }
}
```

### 2. 工作流执行服务

```csharp
public class WorkflowExecutionService
{
    public async Task<CollaborationResult> ExecuteAsync(long taskId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        var config = await _taskConfigRepository.GetByTaskIdAsync(taskId);
        
        // 1. 创建协调者Agent
        var managerAgent = await _agentRepository.GetByIdAsync(config.ManagerAgentId);
        var managerClient = await _agentFactory.CreateAgentAsync(
            config.ManagerAgentId,
            customPrompt: config.ManagerCustomPrompt  // 使用自定义提示词
        );
        
        // 2. 创建Worker Agents
        var workers = await GetTaskWorkersAsync(taskId);
        var workerClients = new List<IChatClient>();
        foreach (var worker in workers)
        {
            var client = await _agentFactory.CreateAgentAsync(worker.Id);
            workerClients.Add(client);
        }
        
        // 3. 根据工作流类型执行
        return config.WorkflowType switch
        {
            "GroupChat" => await ExecuteGroupChatAsync(managerClient, workerClients, task, config),
            "Magentic" => await ExecuteMagenticAsync(managerClient, workerClients, task, config),
            _ => throw new ArgumentException($"Unknown workflow type: {config.WorkflowType}")
        };
    }
    
    private async Task<CollaborationResult> ExecuteGroupChatAsync(
        IChatClient managerClient,
        List<IChatClient> workerClients,
        Task task,
        TaskConfig config)
    {
        // 使用Manager Agent作为协调者
        var manager = new ManagerGroupChatManager(managerClient)
        {
            MaximumInvocationCount = config.MaxIterations
        };
        
        // 创建GroupChatOrchestration
        var orchestration = new GroupChatOrchestration(
            manager,
            workerClients.Select(c => new ChatClientAgent(c)).ToArray())
        {
            ResponseCallback = (response) =>
            {
                _logger.LogInformation("[{Agent}] {Content}", 
                    response.AuthorName, response.Content);
                return ValueTask.CompletedTask;
            }
        };
        
        // 执行
        using var runtime = new InProcessRuntime();
        await runtime.StartAsync();
        
        var result = await orchestration.InvokeAsync(task.Description, runtime);
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

## 📊 完整流程图

```
┌─────────────────────────────────────────────────────┐
│  1. 新建Agent                                        │
└─────────────────────────────────────────────────────┘
选择角色：
├─ Manager（协调者）
│  ├─ 不干活，只维护发言
│  ├─ 提示词影响流转
│  └─ 示例："你是一个协调者，负责引导讨论..."
│
└─ Worker（执行者）
   ├─ 执行具体任务
   ├─ 调用工具
   └─ 示例："你是一个程序员，负责编写代码..."

                ↓

┌─────────────────────────────────────────────────────┐
│  2. 新建任务                                         │
└─────────────────────────────────────────────────────┘
必选项：
├─ 选择协调者（Manager角色的Agent）⭐ 必选
│  ├─ 只能选择Manager角色的Agent
│  ├─ 可以选择提示词模板
│  └─ 可以自定义提示词（最重要！）
│
├─ 选择Worker Agents（Worker角色的Agent）
│  └─ 至少选择1个
│
└─ 选择工作流类型
   ├─ GroupChat
   └─ Magentic

                ↓

┌─────────────────────────────────────────────────────┐
│  3. 执行任务                                         │
└─────────────────────────────────────────────────────┘
├─ 创建协调者Agent（使用自定义提示词）
├─ 创建Worker Agents
├─ 创建Orchestration
│  ├─ GroupChat: ManagerGroupChatManager
│  └─ Magentic: StandardMagenticManager
└─ 使用Runtime执行

                ↓

┌─────────────────────────────────────────────────────┐
│  4. 工作流运行                                       │
└─────────────────────────────────────────────────────┘
协调者（Manager Agent）：
├─ 分析当前状态
├─ 决定下一个发言者
├─ 维护任务账本（Magentic）
└─ 控制流转逻辑

Worker Agents：
├─ 执行具体任务
├─ 调用工具
└─ 返回结果
```

---

## ✅ 核心优势

### 1. **协调者提示词的重要性**

- ✅ 提示词直接影响流转逻辑
- ✅ 可以自定义协调策略
- ✅ 支持模板快速应用
- ✅ 灵活适配不同场景

### 2. **角色分工明确**

- ✅ Manager：不干活，只协调
- ✅ Worker：执行任务，产出结果
- ✅ 职责清晰，避免混乱

### 3. **配置灵活**

- ✅ 新建Agent时选择角色
- ✅ 新建任务时选择协调者
- ✅ 可以修改协调者提示词
- ✅ 支持提示词模板

---

## 🚀 下一步实施

**请确认此设计后，我将开始实施：**

1. ✅ 保留Agent角色字段（Manager/Worker）
2. ✅ 任务配置表添加manager_agent_id（必选）
3. ✅ 实现协调者提示词编辑功能
4. ✅ 实现提示词模板系统
5. ✅ 更新前端UI（协调者必选）
6. ✅ 更新工作流执行逻辑

**这个设计完全正确：协调者是必须的，提示词影响流转，角色分工明确！** 🎯
