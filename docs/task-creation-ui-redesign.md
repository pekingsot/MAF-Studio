# 任务创建UI重新设计方案

## 📋 用户需求

### 1. 协调者选择
- ✅ 只显示**Manager类型**的Agent
- ✅ 选择后可以修改提示词
- ✅ 必填字段

### 2. 队员选择
- ✅ 只显示**Worker类型**的Agent
- ✅ 至少选择一个，不允许为空
- ✅ 验证：如果未选择Worker，显示错误提示

### 3. 工作流类型
- ✅ 只有两种：
  - **群聊协作**（默认协调者模式）
  - **Magentic工作流**

### 4. 移除协调模式选择
- ❌ 移除轮询模式
- ❌ 移除智能模式
- ✅ 默认就是协调者模式

---

## 🎯 新UI设计

### 创建任务弹窗

```
标题: [必填]
描述: [可选]

执行配置（折叠面板，默认展开）:
├─ 工作流类型: 群聊协作 / Magentic工作流
├─ 协调者（Manager类型）: [下拉框，只显示Manager]
│  └─ 自定义协调者提示词: [文本框，可选]
├─ 队员（Worker类型）: [穿梭框，只显示Worker]
│  └─ 验证：至少选择一个
├─ Magentic工作流配置（仅Magentic模式）:
│  ├─ 工作流计划
│  ├─ 阈值标准
│  └─ 最大尝试次数
└─ 最大迭代次数

任务提示词（折叠面板）:
└─ 任务级别提示词
```

---

## 📝 代码修改

### 1. 状态变量初始化

```tsx
// 默认协调者模式
const [taskOrchestrationMode, setTaskOrchestrationMode] = useState<string>('Manager');
```

### 2. 协调者选择（只显示Manager）

```tsx
<Form.Item 
  label={<span><CrownOutlined style={{ color: '#faad14', marginRight: 4 }} />协调者（Manager类型）</span>}
  required
  tooltip="协调者负责引导整个工作流的流转"
>
  <Select
    placeholder="请选择协调者（Manager类型）"
    value={taskManagerAgentId}
    onChange={(value) => setTaskManagerAgentId(value)}
    style={{ width: '100%' }}
  >
    {selectedCollaboration?.agents?.filter(agent => agent.role === 'Manager').map(agent => (
      <Option key={agent.agentId} value={agent.agentId}>
        <Space>
          <CrownOutlined style={{ color: '#faad14' }} />
          {agent.agentName}
          <Tag color="gold">Manager</Tag>
        </Space>
      </Option>
    ))}
  </Select>
</Form.Item>
```

### 3. 队员选择（只显示Worker，至少一个）

```tsx
<Form.Item 
  label={<span><TeamOutlined style={{ color: '#1890ff', marginRight: 4 }} />队员（Worker类型）</span>}
  required
  tooltip="至少选择一个Worker类型的Agent"
  validateStatus={selectedTaskAgents.length === 0 ? 'error' : ''}
  help={selectedTaskAgents.length === 0 ? '请至少选择一个Worker' : ''}
>
  <Transfer
    dataSource={selectedCollaboration?.agents?.filter(agent => agent.role === 'Worker').map(agent => ({
      key: agent.agentId.toString(),
      title: agent.agentName,
    })) || []}
    titles={['可选Worker', '已选Worker']}
    targetKeys={selectedTaskAgents}
    onChange={(targetKeys) => {
      setSelectedTaskAgents(targetKeys as string[]);
    }}
    render={item => item.title}
    listStyle={{ width: 280, height: 180 }}
    selectAllLabels={['全选', '全选']}
  />
</Form.Item>
```

### 4. 工作流类型选择（移除协调模式）

```tsx
<Form.Item label="工作流类型">
  <Radio.Group 
    value={taskWorkflowType} 
    onChange={(e) => {
      setTaskWorkflowType(e.target.value);
      setTaskOrchestrationMode('Manager'); // 默认协调者模式
    }}
  >
    <Space direction="vertical">
      <Radio value="GroupChat">
        <Space>
          <TeamOutlined style={{ color: '#1890ff' }} />
          <span>群聊协作</span>
          <Text type="secondary" style={{ fontSize: 12 }}>协调者引导Worker协作讨论</Text>
        </Space>
      </Radio>
      <Radio value="ReviewIterative">
        <Space>
          <BulbOutlined style={{ color: '#722ed1' }} />
          <span>Magentic智能工作流</span>
          <Text type="secondary" style={{ fontSize: 12 }}>MagenticManager动态协调Worker</Text>
        </Space>
      </Radio>
    </Space>
  </Radio.Group>
</Form.Item>
```

---

## ✅ 核心改进

### 1. 协调者选择
- ✅ 只显示`role === 'Manager'`的Agent
- ✅ 带有Manager标签
- ✅ 必填字段

### 2. 队员选择
- ✅ 只显示`role === 'Worker'`的Agent
- ✅ 至少选择一个Worker
- ✅ 实时验证，显示错误提示

### 3. 简化工作流
- ✅ 移除协调模式选择（轮询、智能）
- ✅ 默认协调者模式
- ✅ 只保留群聊协作和Magentic工作流

### 4. 统一协调者配置
- ✅ 群聊协作和Magentic工作流都使用协调者
- ✅ 协调者提示词可自定义
- ✅ MagenticManager使用协调者的LLM配置

---

## 🚀 下一步

1. 修改`Collaborations.tsx`文件
2. 更新编辑任务的Modal（同样逻辑）
3. 测试功能
