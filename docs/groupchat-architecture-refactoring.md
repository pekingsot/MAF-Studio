# GroupChat 架构重构方案

## 🎯 核心观点

### ❌ 当前架构问题

1. **硬编码的业务逻辑**
   - 总结功能写在代码里
   - System消息硬编码
   - 业务逻辑耦合在群聊核心

2. **职责不清晰**
   - 群聊核心承担了太多业务逻辑
   - Agent的能力被限制在代码中
   - 提示词的作用被忽视

### ✅ 正确的架构

1. **群聊核心职责**
   - 创建Agent
   - 协调对话
   - 返回消息

2. **业务逻辑实现**
   - 通过提示词告诉Agent做什么
   - 通过工具赋予Agent能力
   - Agent自主决定如何执行

---

## 🗑️ 需要删除的代码

### 1. **硬编码的总结功能**

#### **位置**：第468-527行
#### **文件**：`CollaborationWorkflowService.GroupChat.cs`

**需要删除的代码**：
```csharp
if (taskId.HasValue && taskId.Value > 0 && session != null && orchestratorAgentId > 0 && orchestratorChatClient != null)
{
    _logger.LogInformation("开始让协调者Agent生成群聊总结文档...");
    
    yield return new ChatMessageDto
    {
        Sender = "System",
        Content = "📝 正在让Agent生成讨论总结文档...",
        Timestamp = DateTime.UtcNow,
        Role = "system"
    };

    string? conclusionResult = null;
    string? errorMessage = null;
    
    try
    {
        var sessionMessages = await _messageRepository.GetBySessionIdAsync(session.Id);
        
        conclusionResult = await _conclusionService.GenerateAndCommitConclusionAsync(
            taskId.Value,
            collaborationId,
            input,
            sessionMessages.ToList(),
            orchestratorAgentId,
            orchestratorAgentName ?? "执行者",
            orchestratorAgentType,
            orchestratorAgentPrompt,
            orchestratorChatClient,
            cancellationToken);
            
        _logger.LogInformation("总结文档结果: {Result}", conclusionResult);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "生成总结文档失败");
        errorMessage = ex.Message;
    }

    if (!string.IsNullOrEmpty(conclusionResult))
    {
        yield return new ChatMessageDto
        {
            Sender = "System",
            Content = $"📄 {conclusionResult}",
            Timestamp = DateTime.UtcNow,
            Role = "system"
        };
    }
    else if (!string.IsNullOrEmpty(errorMessage))
    {
        yield return new ChatMessageDto
        {
            Sender = "System",
            Content = $"⚠️ 总结文档生成失败: {errorMessage}",
            Timestamp = DateTime.UtcNow,
            Role = "system"
        };
    }
}
```

**删除原因**：
- ❌ 总结功能不应该硬编码在代码里
- ✅ 应该通过提示词告诉协调者"最后总结一下"
- ✅ 如果需要保存文档，应该通过工具实现

---

### 2. **硬编码的工作流配置消息**

#### **位置**：第290-301行
#### **文件**：`CollaborationWorkflowService.GroupChat.cs`

**需要删除的代码**：
```csharp
yield return new ChatMessageDto
{
    Sender = "System",
    Content = $"🎯 **工作流配置**\n\n" +
             $"- **模式**: {parameters.OrchestrationMode}\n" +
             $"- **最大轮次**: {parameters.MaxIterations}\n" +
             $"- **参与者**: {string.Join(", ", participants)}\n" +
             (parameters.OrchestrationMode == GroupChatOrchestrationMode.Manager && managerAgent != null 
                 ? $"- **协调者**: {managerAgent.Name}\n" 
                 : ""),
    Timestamp = DateTime.UtcNow,
    Role = "system",
    Metadata = new Dictionary<string, object> 
    { 
        ["agents"] = agentsInfo,
        ["taskPrompt"] = taskPrompt ?? ""
    }
};
```

**删除原因**：
- ❌ 这是前端展示用的，不应该作为消息发送
- ✅ 应该通过API返回配置信息，而不是作为消息

**替代方案**：
```csharp
// 在开始执行前，先返回配置信息（不是消息）
// 前端单独展示配置信息，不作为聊天消息
```

---

### 3. **硬编码的选择思考消息**

#### **位置**：第395-401行
#### **文件**：`CollaborationWorkflowService.GroupChat.cs`

**需要删除的代码**：
```csharp
yield return new ChatMessageDto
{
    Sender = thinkingSender,
    Content = $"🤔 **选择思考**\n\n" +
             $"- **当前轮次**: {roundNumber + 1}\n" +
             $"- **选择Agent**: {newAgentName}\n" +
             $"- **角色**: {member2?.Role ?? "Worker"}\n" +
             $"- **原因**: {thinkingReason}",
    Timestamp = DateTime.UtcNow,
    Role = "assistant"
};
```

**删除原因**：
- ❌ 这是硬编码的思考过程，不是真实的AI思考
- ✅ 如果需要显示思考过程，应该从MAF框架获取真实的思考内容
- ✅ 或者完全删除，让Agent自己决定是否展示思考过程

**替代方案**：
```csharp
// 方案1：完全删除，不显示选择思考
// 方案2：从MAF框架获取真实的思考内容（如果支持）
```

---

### 4. **其他硬编码的System消息**

#### **位置**：多处
#### **文件**：`CollaborationWorkflowService.GroupChat.cs`

**需要删除的代码**：
```csharp
// 第46行：错误消息
yield return new ChatMessageDto
{
    Sender = "System",
    Content = $"协作 {collaborationId} 不存在",
    Role = "system"
};

// 第86行：错误消息
yield return new ChatMessageDto
{
    Sender = "System",
    Content = "没有可用的Agent",
    Role = "system"
};
```

**删除原因**：
- ❌ 错误消息不应该作为聊天消息
- ✅ 应该通过API返回错误信息

**替代方案**：
```csharp
// 直接抛出异常，让上层处理
if (collaboration == null)
{
    throw new NotFoundException($"协作 {collaborationId} 不存在");
}

if (members.Count == 0)
{
    throw new InvalidOperationException("没有可用的Agent");
}
```

---

## 🔄 需要重构的部分

### 1. **群聊核心逻辑**

#### **保留的功能**：
```csharp
public async IAsyncEnumerable<ChatMessageDto> ExecuteGroupChatAsync(
    long collaborationId,
    string input,
    GroupChatParameters? parameters = null,
    long? taskId = null,
    CancellationToken cancellationToken = default)
{
    // 1. 准备工作
    // - 查询协作和Agent
    // - 创建ChatClient
    // - 构建提示词
    
    // 2. 创建工作流
    var workflow = AgentWorkflowBuilder
        .CreateGroupChatBuilderWith(agents => ...)
        .AddParticipants(mafAgents.ToArray())
        .Build();
    
    // 3. 执行工作流
    await using var run = await InProcessExecution.RunStreamingAsync(workflow, messages);
    await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
    
    // 4. 监听事件并返回消息
    await foreach (var evt in run.WatchStreamAsync())
    {
        if (evt is AgentResponseUpdateEvent updateEvent)
        {
            // 返回Agent的消息
            yield return new ChatMessageDto
            {
                Sender = agentName,
                Content = content,
                Role = "assistant"
            };
        }
    }
    
    // 5. 清理工作
    // - 更新Agent状态
    // - 结束会话
}
```

#### **删除的功能**：
- ❌ 硬编码的总结功能
- ❌ 硬编码的System消息
- ❌ 硬编码的选择思考
- ❌ 硬编码的工作流配置消息

---

### 2. **提示词设计**

#### **协调者提示词应该包含**：
```markdown
你是一个群聊协调者，负责引导讨论。

你的职责：
1. 分析当前讨论内容
2. 决定下一个发言的Agent
3. 确保讨论不偏离主题
4. 引导工作流顺利完成
5. 在合适的时候总结讨论成果

注意事项：
- 根据任务需求和Agent专长选择合适的发言者
- 确保讨论有序进行，避免重复发言
- 在所有任务完成后，提供详细的总结

团队成员：
{{members}}

任务描述：
{{taskDescription}}

任务要求：
{{taskPrompt}}
```

#### **Worker提示词应该包含**：
```markdown
你是一个{{agentRole}}，负责{{agentDescription}}。

团队成员：
{{members}}

任务描述：
{{taskDescription}}

任务要求：
{{taskPrompt}}

请根据你的专长完成任务。
```

---

### 3. **工具设计**

#### **如果需要总结功能，应该创建工具**：
```csharp
public class SummaryTool : Tool
{
    [KernelFunction]
    [Description("生成讨论总结并保存到文档")]
    public async Task<string> GenerateSummaryAsync(
        [Description("总结内容")] string summary,
        [Description("文档标题")] string title)
    {
        // 保存总结到数据库或文件
        // 返回保存结果
    }
}
```

#### **如果需要其他功能，也应该通过工具实现**：
```csharp
public class TaskLedgerTool : Tool
{
    [KernelFunction]
    [Description("更新任务账本")]
    public async Task<string> UpdateTaskLedgerAsync(
        [Description("任务列表")] string tasks)
    {
        // 更新任务进度
    }
}

public class ProgressLedgerTool : Tool
{
    [KernelFunction]
    [Description("更新进度账本")]
    public async Task<string> UpdateProgressLedgerAsync(
        [Description("进度信息")] string progress)
    {
        // 更新进度信息
    }
}
```

---

## 📋 重构清单

### 阶段1：删除硬编码的业务逻辑

- [ ] 删除总结功能代码（第468-527行）
- [ ] 删除工作流配置消息（第290-301行）
- [ ] 删除选择思考消息（第395-401行）
- [ ] 删除其他System消息（第46、86行）

### 阶段2：重构错误处理

- [ ] 使用异常代替System消息
- [ ] 在Controller层统一处理异常
- [ ] 返回合适的HTTP状态码

### 阶段3：优化提示词

- [ ] 在协调者提示词中添加总结要求
- [ ] 在Worker提示词中明确职责
- [ ] 确保提示词包含所有必要信息

### 阶段4：添加工具（如果需要）

- [ ] 创建SummaryTool（如果需要总结功能）
- [ ] 创建TaskLedgerTool（如果需要任务账本）
- [ ] 创建ProgressLedgerTool（如果需要进度账本）

---

## 🎯 重构后的架构

### 群聊核心职责

```
┌─────────────────────────────────────┐
│   CollaborationWorkflowService      │
│                                     │
│   1. 准备工作                        │
│      - 查询协作和Agent               │
│      - 创建ChatClient               │
│      - 构建提示词                    │
│                                     │
│   2. 创建工作流                      │
│      - 创建Agent实例                 │
│      - 创建GroupChatManager         │
│                                     │
│   3. 执行工作流                      │
│      - 监听事件                      │
│      - 返回消息                      │
│                                     │
│   4. 清理工作                        │
│      - 更新Agent状态                 │
│      - 结束会话                      │
│                                     │
└─────────────────────────────────────┘
```

### 业务逻辑实现

```
┌─────────────────────────────────────┐
│   Agent（通过提示词和工具）           │
│                                     │
│   1. 协调者                          │
│      - 提示词：告诉它如何协调         │
│      - 工具：总结、任务账本等         │
│                                     │
│   2. Worker                          │
│      - 提示词：告诉它如何工作         │
│      - 工具：具体业务能力             │
│                                     │
└─────────────────────────────────────┘
```

---

## 💡 关键改进

### 改进1：职责分离

**之前**：
- 群聊核心承担业务逻辑
- 代码硬编码总结功能
- System消息混乱

**之后**：
- 群聊核心只负责协调
- 业务逻辑通过提示词和工具实现
- 消息清晰简洁

### 改进2：灵活性提升

**之前**：
- 总结功能固定，无法定制
- 选择思考硬编码，不真实
- 业务逻辑修改需要改代码

**之后**：
- 通过提示词定制总结方式
- Agent自主决定是否展示思考
- 业务逻辑修改只需改提示词或工具

### 改进3：可维护性提升

**之前**：
- 业务逻辑散落在代码各处
- 难以理解和维护
- 修改容易引入bug

**之后**：
- 业务逻辑集中在提示词和工具
- 清晰易懂
- 修改风险低

---

## 🚀 预期收益

### 代码质量
- **代码行数减少：约100行**
- **职责清晰：群聊核心只负责协调**
- **可维护性提升：业务逻辑集中在提示词和工具**

### 灵活性
- **业务逻辑可定制：通过提示词定制**
- **功能可扩展：通过工具扩展**
- **修改风险低：只需改提示词或工具**

### 性能
- **减少不必要的消息：删除System消息**
- **减少数据库操作：删除总结功能**
- **提升响应速度：简化流程**

---

*最后更新：2026-04-10*
