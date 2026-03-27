# Microsoft Agent Framework (MAF) 使用指南

## 概述

Microsoft Agent Framework（MAF）是微软于2025年10月1日发布的开源框架，用于构建、编排和部署 AI Agent（智能体）和多智能体工作流。它是将 **AutoGen** 和 **Semantic Kernel** 项目整合后的统一框架。

### 核心特点

- **统一框架**：整合了 AutoGen 的多智能体编排能力和 Semantic Kernel 的语义处理能力
- **多语言支持**：支持 .NET (C#) 和 Python
- **开源**：完全开源，可扩展
- **企业级**：适合生产环境部署

---

## 安装

### .NET / C#

```bash
dotnet add package Azure.AI.OpenAI --prerelease
dotnet add package Azure.Identity
dotnet add package Microsoft.Agents.AI.OpenAI --prerelease
```

### Python

```bash
pip install agent-framework --pre
```

---

## 快速开始

### 创建第一个 Agent

```csharp
using System;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

// 创建 Agent
AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You are a friendly assistant. Keep your answers brief.", 
        name: "HelloAgent"
    );

// 同步运行
Console.WriteLine(await agent.RunAsync("What is the largest city in France?"));

// 流式响应
await foreach (var update in agent.RunStreamingAsync("Tell me a one-sentence fun fact."))
{
    Console.Write(update);
}
```

---

## 核心概念

### 1. Agent（智能体）

Agent 是框架的核心抽象，代表一个具有特定能力和指令的 AI 实体。

```csharp
// 创建具有特定指令的 Agent
var translatorAgent = new AIAgent(
    client: chatClient,
    name: "Translator",
    instructions: "You are a professional translator. Translate the input to Chinese."
);
```

### 2. Tools / Function Calling（工具调用）

赋予 Agent 调用外部代码、API 和服务的能力。

```csharp
using Microsoft.Agents.AI;

// 定义工具函数
[AgentTool]
public string GetCurrentWeather(string city)
{
    // 调用天气 API
    return $"Weather in {city}: Sunny, 25°C";
}

// 将工具注册到 Agent
var agentWithTools = chatClient
    .AsAIAgent(instructions: "You are a weather assistant.", name: "WeatherAgent")
    .WithTools(new[] { typeof(Program).GetMethod("GetCurrentWeather") });
```

### 3. Workflows（工作流）

MAF 提供四种工作流模式：

#### 3.1 Sequential（顺序执行）

多个 Agent 按顺序依次处理任务。

```csharp
using Microsoft.Agents.AI.Workflows;

var workflow = AgentWorkflowBuilder
    .Create()
    .AddAgent(translatorAgent)      // 第一步：翻译
    .AddAgent(summarizerAgent)      // 第二步：摘要
    .AddAgent(reviewerAgent)        // 第三步：审核
    .Build();

var result = await workflow.RunAsync("Long text to process...");
```

#### 3.2 Concurrent（并行执行）

多个 Agent 同时处理同一输入。

```csharp
var workflow = AgentWorkflowBuilder
    .Create()
    .AddAgent(englishTranslator)    // 并行翻译成英语
    .AddAgent(frenchTranslator)     // 并行翻译成法语
    .AddAgent(spanishTranslator)    // 并行翻译成西班牙语
    .WithMode(WorkflowMode.Concurrent)
    .Build();

var results = await workflow.RunAsync("Hello World");
// 返回三个翻译结果
```

#### 3.3 Handoffs（交接）

Agent 之间可以相互交接任务。

```csharp
var triageAgent = new AIAgent(client, "Triage", "Route requests to appropriate agents");
var salesAgent = new AIAgent(client, "Sales", "Handle sales inquiries");
var supportAgent = new AIAgent(client, "Support", "Handle support tickets");

triageAgent.AddHandoffs(new[] { salesAgent, supportAgent });

// Triage Agent 会根据用户输入决定交接给哪个 Agent
var result = await triageAgent.RunAsync("I need a refund for my order");
// 自动交接给 Support Agent
```

#### 3.4 Group Chat（群聊）

多个 Agent 在群组中协作对话。

```csharp
var groupChat = new AgentGroupChat(
    agents: new[] { writerAgent, editorAgent, reviewerAgent },
    moderator: moderatorAgent  // 可选：协调者
);

await foreach (var message in groupChat.RunStreamingAsync("Write an article about AI"))
{
    Console.WriteLine($"{message.Sender}: {message.Content}");
}
```

---

## 高级特性

### 状态管理与持久化

实现会话状态的保存和恢复。

```csharp
using Microsoft.Agents.AI.State;

// 定义状态
public class ConversationState
{
    public List<Message> History { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
}

// 创建有状态的 Agent
var statefulAgent = new StatefulAIAgent<ConversationState>(
    client: chatClient,
    name: "StatefulAgent",
    instructions: "Remember our conversation context."
);

// 保存状态
var state = await statefulAgent.GetStateAsync();
await SaveToDatabase(state);

// 恢复状态
var savedState = await LoadFromDatabase();
await statefulAgent.SetStateAsync(savedState);
```

### 上下文管理

```csharp
// 添加上下文信息
agent.WithContext(new Dictionary<string, object>
{
    ["userName"] = "John",
    ["preferredLanguage"] = "Chinese",
    ["sessionStartTime"] = DateTime.UtcNow
});
```

### 流式响应

```csharp
await foreach (var chunk in agent.RunStreamingAsync(userInput))
{
    if (chunk.Text != null)
    {
        Console.Write(chunk.Text);
    }
    
    if (chunk.ToolCall != null)
    {
        Console.WriteLine($"\n[Tool Call: {chunk.ToolCall.Name}]");
    }
}
```

---

## 支持的 Provider

| Provider | 包名 | 说明 |
|----------|------|------|
| Azure OpenAI | `Microsoft.Agents.AI.OpenAI` | Azure OpenAI Service |
| OpenAI | `Microsoft.Agents.AI.OpenAI` | OpenAI API |
| Azure AI Foundry | `Microsoft.Agents.AI.Foundry` | Azure AI Foundry |
| 自定义 | - | 实现 `IChatClient` 接口 |

---

## 最佳实践

### 1. Agent 设计原则

- **单一职责**：每个 Agent 应专注于一个特定任务
- **清晰的指令**：提供明确、详细的 instructions
- **合理的命名**：使用有意义的 Agent 名称

### 2. 工作流选择

| 场景 | 推荐工作流 |
|------|-----------|
| 流水线处理 | Sequential |
| 多角度分析 | Concurrent |
| 任务分发 | Handoffs |
| 协作讨论 | Group Chat |

### 3. 错误处理

```csharp
try
{
    var result = await agent.RunAsync(input);
}
catch (AgentException ex)
{
    // 处理 Agent 特定错误
    Console.WriteLine($"Agent error: {ex.Message}");
}
catch (RateLimitException ex)
{
    // 处理速率限制
    await Task.Delay(ex.RetryAfter);
    // 重试...
}
```

---

## 相关资源

- [官方文档](https://learn.microsoft.com/zh-cn/agent-framework/)
- [GitHub 仓库](https://github.com/microsoft/agent-framework)
- [示例代码](https://github.com/microsoft/agent-framework/tree/main/samples)

---

## 版本信息

- 发布日期：2025年10月1日
- 当前状态：公共预览版
- 支持 .NET：.NET 8.0+
- 支持 Python：3.8+
