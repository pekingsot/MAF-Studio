# 协调者提示词功能实现总结

## 📋 已完成的工作

### 1. ✅ 后端添加ManagerCustomPrompt字段

**文件**: `/home/pekingost/projects/maf-studio/backend/src/MAFStudio.Application/DTOs/TaskConfig.cs`

**修改内容**:

```csharp
/// <summary>
/// 协调者自定义提示词
/// 如果指定，将覆盖协调者Agent的默认提示词
/// 用于自定义协调者的行为和策略
/// </summary>
[JsonPropertyName("managerCustomPrompt")]
public string? ManagerCustomPrompt { get; set; }
```

**说明**:
- ✅ 添加了`ManagerCustomPrompt`字段
- ✅ 支持自定义协调者提示词
- ✅ 可选字段，留空则使用协调者Agent的默认提示词

---

### 2. ✅ 前端添加协调者提示词编辑UI

**文件**: `/home/pekingost/projects/maf-studio/frontend/src/pages/Collaborations.tsx`

**修改内容**:

#### 2.1 添加状态变量

```tsx
const [taskManagerCustomPrompt, setTaskManagerCustomPrompt] = useState<string>('');
```

#### 2.2 GroupChat - Manager模式下的UI

```tsx
{taskOrchestrationMode === 'Manager' && (
  <>
    <Form.Item 
      label={<span><CrownOutlined style={{ color: '#faad14', marginRight: 4 }} />协调者</span>}
      required
    >
      <Select
        placeholder="请选择协调者"
        value={taskManagerAgentId}
        onChange={(value) => setTaskManagerAgentId(value)}
        style={{ width: '100%' }}
      >
        {selectedCollaboration?.agents?.map(agent => (
          <Option key={agent.agentId} value={agent.agentId}>
            {agent.agentName} {agent.agentType ? `(${agent.agentType})` : ''}
          </Option>
        ))}
      </Select>
    </Form.Item>
    
    <Alert
      message="协调者提示词"
      description={
        <div>
          <p>协调者的提示词将影响整个工作流的流转逻辑，请仔细编写！</p>
          <p style={{ marginTop: 8, fontSize: 12, color: '#666' }}>
            留空则使用协调者Agent的默认提示词
          </p>
        </div>
      }
      type="warning"
      showIcon
      style={{ marginBottom: 12 }}
    />
    
    <Form.Item label="自定义协调者提示词（可选）">
      <Input.TextArea
        rows={8}
        placeholder={`自定义协调者提示词，将覆盖协调者Agent的默认提示词。

示例：
你是一个群聊协调者，负责引导讨论。

你的职责：
1. 分析当前讨论内容
2. 决定下一个发言的Agent
3. 确保讨论不偏离主题

发言选择策略：
- 如果讨论到"安全"问题，请让"安全专家"发言
- 如果讨论到"代码"实现，请让"程序员"发言
- 如果讨论到"架构"设计，请让"架构师"发言`}
        value={taskManagerCustomPrompt}
        onChange={(e) => setTaskManagerCustomPrompt(e.target.value)}
      />
    </Form.Item>
  </>
)}
```

---

## 📊 功能说明

### 1. 工作流类型选择

**已实现** ✅

- **GroupChat**: 群聊协作模式
- **ReviewIterative**: Magentic智能工作流模式

### 2. 协调者选择

**已实现** ✅

- **GroupChat - Manager模式**: 需要选择协调者
- **Magentic模式**: 需要选择协调者（MagenticManager使用协调者的配置）

### 3. 协调者提示词编辑

**已实现** ✅

- **GroupChat - Manager模式**: 可自定义协调者提示词
- **Magentic模式**: 可自定义MagenticManager提示词

---

## 🎯 使用流程

### 1. 新建Agent（协调者）

```tsx
创建Agent：
├─ Name: "协调者"
├─ Role: Manager
├─ LLM配置:
│  ├─ Provider: OpenAI
│  ├─ Model: gpt-4o
│  ├─ ApiKey: sk-xxx
│  └─ Endpoint: https://api.openai.com
├─ SystemPrompt: "你是一个协调者..."
└─ Temperature: 0.7
```

### 2. 新建任务

```tsx
创建任务：
├─ 标题: "技术方案讨论"
├─ 描述: "讨论系统架构设计"
├─ 工作流类型: GroupChat / Magentic
├─ 协调模式: Manager（GroupChat模式下）
├─ 协调者: 选择Manager角色的Agent ✅
├─ 自定义协调者提示词: 可选 ✅
└─ Workers: 选择Worker角色的Agent
```

### 3. 执行任务

```csharp
// 后端使用协调者的配置
var managerAgent = await _agentRepository.GetByIdAsync(managerAgentId);

// 使用协调者的LLM配置
var baseClient = new OpenAIClient(managerAgent.LlmApiKey)
    .AsChatClient(managerAgent.LlmModel);

// 使用自定义提示词或默认提示词
var systemPrompt = taskConfig.ManagerCustomPrompt ?? managerAgent.SystemPrompt;

// 创建MagenticManager或GroupChatManager
var managerClient = CreateManager(baseClient, systemPrompt);
```

---

## ✅ 核心优势

### 1. **配置复用**
- ✅ 复用协调者的LLM配置
- ✅ 复用协调者的API Key
- ✅ 复用协调者的其他配置

### 2. **灵活性**
- ✅ 支持自定义提示词
- ✅ 可覆盖协调者默认提示词
- ✅ 适配不同工作流类型

### 3. **用户友好**
- ✅ 清晰的UI提示
- ✅ 示例提示词
- ✅ 警告提示

---

## 🚀 下一步

### 需要测试的功能

1. **创建任务**:
   - 选择工作流类型（GroupChat / Magentic）
   - 选择协调者
   - 编辑协调者提示词

2. **执行任务**:
   - 验证协调者提示词是否生效
   - 验证LLM配置是否正确使用

3. **编辑任务**:
   - 修改协调者
   - 修改协调者提示词

---

## 📝 注意事项

### 1. 协调者选择

- **GroupChat - Manager模式**: 必须选择协调者
- **Magentic模式**: 必须选择协调者（MagenticManager使用协调者的配置）
- **GroupChat - RoundRobin/Intelligent模式**: 不需要协调者

### 2. 提示词优先级

```
1. 任务配置中的自定义提示词（最高优先级）
2. 协调者Agent的默认提示词
3. 系统内置默认提示词（最低优先级）
```

### 3. LLM配置来源

**所有LLM配置都来自协调者Agent**:
- Provider
- Model
- ApiKey
- Endpoint
- Temperature
- MaxTokens

---

## 🎯 总结

**已完成**:
1. ✅ 后端添加ManagerCustomPrompt字段
2. ✅ 前端添加协调者提示词编辑UI
3. ✅ GroupChat - Manager模式下的协调者选择和提示词编辑
4. ✅ Magentic模式下的协调者选择和提示词编辑

**核心设计**:
- **协调者的所有配置（LLM、提示词等）都会被MagenticManager或GroupChatManager复用**
- **支持自定义提示词，覆盖协调者默认提示词**
- **用户友好的UI，清晰的提示和示例**

**这是最合理、最优雅的设计方案！** 🎯
