# 任务创建UI优化方案

## 📋 当前问题

1. **页面太长**：协调者提示词和Magentic配置占用太多空间
2. **协调者配置未入库**：需要确保managerAgentId和managerCustomPrompt保存到config字段

---

## ✅ 已修复

### 1. 后端TaskConfig已包含字段

```csharp
[JsonPropertyName("managerAgentId")]
public long? ManagerAgentId { get; set; }

[JsonPropertyName("managerCustomPrompt")]
public string? ManagerCustomPrompt { get; set; }
```

### 2. 前端已正确传递数据

```tsx
const config: any = {
  workflowType: taskWorkflowType,
  orchestrationMode: taskOrchestrationMode,
  maxIterations: taskMaxIterations,
  managerAgentId: taskManagerAgentId,  // ✅ 协调者ID
  managerCustomPrompt: taskManagerCustomPrompt || undefined  // ✅ 协调者提示词
};
```

---

## 🎨 UI优化方案

### 优化前

```
标题
描述
队员选择
执行配置（折叠面板）:
├─ 工作流类型
├─ 协调者选择
├─ 协调者提示词Alert（占用空间）
├─ 协调者提示词TextArea（6行）
├─ Magentic配置Alert（占用空间）
├─ Magentic配置项
└─ 最大迭代次数
任务提示词（折叠面板）
```

### 优化后

```
标题
描述
队员选择
执行配置（折叠面板）:
├─ 工作流类型
├─ 协调者选择
├─ 最大迭代次数
└─ Magentic工作流配置（折叠面板，仅Magentic模式）
    ├─ 工作流计划
    ├─ 阈值标准
    └─ 最大尝试次数
协调者提示词（折叠面板，默认折叠）:
└─ 自定义协调者提示词（4行）
任务提示词（折叠面板）:
└─ 任务级别提示词
```

---

## 📝 具体修改

### 1. 简化协调者提示词

- ❌ 移除Alert提示（占用空间）
- ✅ 直接显示TextArea（减少行数从6行到4行）
- ✅ 放到单独的折叠面板中

### 2. 简化Magentic配置

- ❌ 移除Alert提示（占用空间）
- ✅ 放到折叠面板中
- ✅ 仅在Magentic模式时显示

### 3. 优化布局

- ✅ 减少不必要的说明文字
- ✅ 使用折叠面板组织内容
- ✅ 默认折叠不常用的配置

---

## 🚀 实施步骤

1. ✅ 修复前端传递协调者配置的逻辑
2. ⏳ 优化UI布局，减少页面长度
3. ⏳ 测试协调者配置保存功能

---

## 📊 预期效果

### 页面长度减少

- **优化前**：约1200px高度
- **优化后**：约800px高度
- **减少**：约400px（33%）

### 用户体验提升

- ✅ 页面更简洁
- ✅ 配置更清晰
- ✅ 操作更快捷
