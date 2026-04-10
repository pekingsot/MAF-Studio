# Magentic 智能工作流改进计划

## 📊 当前系统分析

### ✅ 已实现的功能

#### 1. 基础参数配置
- ✅ 协调者选择（Manager类型Agent）
- ✅ 最大迭代次数
- ✅ 最大尝试次数
- ✅ 阈值标准
- ✅ 工作流计划

#### 2. 团队成员管理
- ✅ Agent列表
- ✅ 角色定义（Manager/Worker）
- ✅ 自定义提示词

---

### ❌ 缺失的核心功能

#### 1. 核心大脑参数（LLM Settings）
- ❌ **LLM模型选择**：无法为协调者指定高性能模型（如GPT-4o）
- ❌ **Temperature设置**：无法设置协调者的确定性（应为0）
- ❌ **MaxTokens配置**：无法为协调者分配足够的思考空间（建议2000+）

#### 2. 人工回路（Human-in-the-Loop, HITL）
- ❌ **工具调用审批**：无法在Git Push等危险操作前暂停
- ❌ **计划确认模式**：Manager拆解任务后无法人工确认
- ❌ **异步审批机制**：无法持久化状态等待人工审批

#### 3. 安全配置
- ❌ **Git Token管理**：无法安全地注入Token
- ❌ **环境变量注入**：无法从KeyVault读取敏感信息
- ❌ **SharedState管理**：无法安全地传递Git地址、分支等

#### 4. 高级功能
- ❌ **ShowPlanningProcess**：无法显示Manager的内心独白
- ❌ **TaskLedger初始化**：无法预设子任务清单
- ❌ **Session持久化**：无法跟踪和恢复对话

---

## 💡 改进建议

### 1. 界面改进

#### 新增"协调者配置"区域
```
┌─────────────────────────────────────────────────────────┐
│  协调者配置                                              │
├─────────────────────────────────────────────────────────┤
│  协调者模型：[GPT-4o ▼]                                 │
│  Temperature：[0] (建议设为0，确保协调者决策确定性)      │
│  MaxTokens：[2000] (为协调者留出足够思考空间)            │
│                                                          │
│  ☑ 显示规划过程（ShowPlanningProcess）                  │
│  ☑ 开启人工审批（RequireHumanApproval）                 │
│  ☑ 计划确认模式（PlanReview）                           │
└─────────────────────────────────────────────────────────┘
```

#### 新增"安全配置"区域
```
┌─────────────────────────────────────────────────────────┐
│  安全配置                                                │
├─────────────────────────────────────────────────────────┤
│  Git仓库地址：[https://github.com/...]                  │
│  Git分支：[feature-1]                                    │
│  ☑ 需要人工审批危险操作                                 │
│    - Git Push                                            │
│    - 删除分支                                            │
│    - 数据库修改                                          │
└─────────────────────────────────────────────────────────┘
```

---

### 2. 后端改进

#### 新增参数类
```csharp
public class MagenticOneOptions
{
    // 核心大脑参数
    public string? LlmModelName { get; set; }
    public double Temperature { get; set; } = 0;
    public int MaxTokens { get; set; } = 2000;
    
    // 编排约束参数
    public int MaxRounds { get; set; } = 20;
    public int MaxRetryAttempts { get; set; } = 3;
    public bool ShowPlanningProcess { get; set; } = true;
    
    // 人工回路参数
    public bool RequireHumanApprovalForTools { get; set; } = true;
    public bool EnablePlanReview { get; set; } = true;
    public List<string> ToolsRequiringApproval { get; set; } = new() 
    { 
        "GitPush", 
        "DeleteBranch", 
        "DatabaseModify" 
    };
    
    // 安全配置
    public string? GitRepositoryUrl { get; set; }
    public string? GitBranch { get; set; }
    public string? GitToken { get; set; } // 从环境变量读取
}
```

---

### 3. 工作流改进

#### 人工审批流程
```
1. Manager拆解任务
   ↓
2. 【暂停】等待人工确认计划
   ↓
3. 人工批准/修改
   ↓
4. Worker执行任务
   ↓
5. 【暂停】遇到危险操作（Git Push）
   ↓
6. 人工批准/拒绝
   ↓
7. 继续执行
```

---

## 🎯 优先级建议

### P0（必须实现）
1. ✅ **协调者模型选择**：允许用户选择高性能模型
2. ✅ **Temperature设置**：默认设为0
3. ✅ **Git Token安全注入**：从环境变量读取

### P1（重要功能）
1. ⭐ **工具调用审批**：Git Push前暂停
2. ⭐ **计划确认模式**：Manager拆解任务后暂停
3. ⭐ **ShowPlanningProcess**：显示Manager内心独白

### P2（锦上添花）
1. 💡 **异步审批机制**：持久化状态
2. 💡 **Session持久化**：恢复对话
3. 💡 **TaskLedger初始化**：预设子任务

---

## 📝 实施计划

### 第一阶段：完善群聊协作
- [x] 任务描述添加到系统提示词
- [x] 任务提示词变量替换
- [x] 协调者配置优化
- [ ] 群聊模式测试和优化

### 第二阶段：Magentic基础功能
- [ ] 协调者模型选择
- [ ] Temperature和MaxTokens配置
- [ ] Git Token安全注入
- [ ] ShowPlanningProcess功能

### 第三阶段：人工回路
- [ ] 工具调用审批机制
- [ ] 计划确认模式
- [ ] 异步审批机制

### 第四阶段：高级功能
- [ ] Session持久化
- [ ] TaskLedger初始化
- [ ] 状态恢复机制

---

## 🔗 参考资料

### MAF核心概念
1. **Task Ledger（任务账本）**：Manager维护的任务清单
2. **Progress Ledger（进度账本）**：Manager维护的进度记录
3. **MagenticManager**：智能协调者，负责规划、分派、反思

### 最佳实践
1. **协调者模型**：使用高性能模型（GPT-4o）
2. **Temperature**：设为0，确保决策确定性
3. **MaxTokens**：至少2000，留出足够思考空间
4. **人工审批**：对危险操作必须开启

---

## 📌 注意事项

1. **安全性**：Git Token等敏感信息不能传给LLM，只能传给Tool
2. **确定性**：协调者Temperature必须为0，避免随机性
3. **可控性**：危险操作必须有人工审批
4. **持久化**：长时间任务需要状态持久化

---

*最后更新：2026-04-10*
