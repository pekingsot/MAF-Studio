# MAF Studio - 多Agent协作系统技术文档

## 📋 文档信息

- **项目名称**: MAF Studio
- **版本**: 1.0.0
- **创建日期**: 2026-03-29
- **技术栈**: .NET 10.0, React, PostgreSQL, Docker
- **核心框架**: Microsoft Agent Framework (MAF)

## ⚠️ 重要说明

**本项目完全基于Microsoft Agent Framework (MAF)构建，所有Agent协作、工作流编排功能均使用MAF提供的原生API，不闭门造车。**

### MAF框架简介

Microsoft Agent Framework（MAF）是微软于2025年10月1日发布的开源框架，用于构建、编排和部署AI Agent和多智能体工作流。它是将**AutoGen**和**Semantic Kernel**项目整合后的统一框架。

### MAF核心能力

MAF提供以下核心能力，本项目直接使用这些能力：

1. **AIAgent创建**: 使用`AsAIAgent()`方法创建智能体
2. **Tools注册**: 使用`WithTools()`方法注册工具
3. **工作流编排**: 使用`AgentWorkflowBuilder`创建工作流
   - Sequential（顺序执行）
   - Concurrent（并发执行）
   - Handoffs（任务移交）
   - Group Chat（群聊）
4. **流式响应**: 使用`RunStreamingAsync()`方法
5. **状态管理**: 使用`StatefulAIAgent<T>`管理状态

### 本项目不重复造轮子

- ✅ **使用MAF的Agent创建**: 不自己实现Agent类
- ✅ **使用MAF的工作流引擎**: 不自己实现工作流编排
- ✅ **使用MAF的Tools机制**: 不自己实现工具调用
- ✅ **使用MAF的流式响应**: 不自己实现流式处理
- 🔧 **扩展MAF**: 仅在MAF基础上添加业务功能（环境管理、Skill加载等）

---

## 📑 目录

1. [系统概述](#1-系统概述)
2. [架构设计](#2-架构设计)
3. [Agent系统](#3-agent系统)
4. [多Agent协作](#4-多agent协作)
5. [Skill系统](#5-skill系统)
6. [能力系统](#6-能力系统)
7. [环境管理](#7-环境管理)
8. [工作流引擎](#8-工作流引擎)
9. [对话流程可视化](#9-对话流程可视化)
10. [数据库设计](#10-数据库设计)
11. [API设计](#11-api设计)
12. [前端设计](#12-前端设计)
13. [部署方案](#13-部署方案)
14. [安全设计](#14-安全设计)
15. [性能优化](#15-性能优化)

---

## 1. 系统概述

### 1.1 项目背景

MAF Studio是一个基于Microsoft Agent Framework (MAF)构建的多Agent协作平台，旨在让多个AI Agent能够协同工作，完成复杂任务。

**重要说明**：本项目完全基于Microsoft Agent Framework (MAF)构建，使用MAF提供的原生API，不闭门造车。MAF是微软于2025年10月1日发布的开源框架，整合了AutoGen和Semantic Kernel项目。

系统支持：

- **智能体管理**: 创建、配置和管理多个AI Agent
- **多Agent协作**: 支持团队协作、流程编排、对话管理
- **能力扩展**: 内置常用能力 + 自定义Skill扩展
- **环境管理**: Docker容器内统一管理运行时环境
- **流程可视化**: 实时查看Agent对话和协作流程

### 1.2 核心特性

| 特性 | 描述 | MAF支持 |
|------|------|---------|
| **多Agent协作** | 支持Sequential、Concurrent、Handoffs、Group Chat等多种协作模式 | ✅ MAF原生支持 |
| **能力系统** | 内置文件、Git、文档操作能力 + 自定义Skill扩展 | ✅ 使用MAF的Tools机制 |
| **环境管理** | Web界面管理Python、Node.js等运行时环境 | 🔧 自定义实现 |
| **工作流引擎** | 可视化编排Agent协作流程 | ✅ 使用MAF的AgentWorkflowBuilder |
| **实时监控** | 查看Agent对话、执行状态、资源使用 | 🔧 基于MAF的流式响应 |
| **即插即用** | Skill按SKILL.md标准，上传即可使用 | 🔧 基于MAF的Tools扩展 |

**图例说明**：
- ✅ MAF原生支持：直接使用MAF提供的API
- 🔧 自定义实现：基于MAF扩展或自定义开发

### 1.3 技术选型

#### 后端技术栈
- **框架**: .NET 10.0, ASP.NET Core Web API
- **ORM**: Dapper (轻量级，高性能)
- **数据库**: PostgreSQL 15
- **AI框架**: Microsoft Agent Framework (MAF)
- **文档处理**: DocumentFormat.OpenXml, NPOI
- **Git操作**: LibGit2Sharp

#### 前端技术栈
- **框架**: React 18, TypeScript
- **UI库**: Ant Design 5
- **状态管理**: React Hooks
- **图表**: Recharts
- **构建工具**: Vite

#### 基础设施
- **容器化**: Docker, Docker Compose
- **反向代理**: Nginx (可选)
- **日志**: Serilog
- **监控**: Prometheus + Grafana (可选)

---

## 2. 架构设计

### 2.1 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                        前端层 (React)                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │ Agent管理 │  │ 协作管理  │  │ 环境管理  │  │ 流程监控  │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
                              ↕ HTTP/WebSocket
┌─────────────────────────────────────────────────────────────┐
│                      API层 (ASP.NET Core)                    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │Agent API │  │协作API   │  │环境API   │  │监控API   │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
                              ↕
┌─────────────────────────────────────────────────────────────┐
│                     应用层 (Application)                      │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │Agent服务  │  │协作服务   │  │Skill服务  │  │工作流服务 │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
                              ↕
┌─────────────────────────────────────────────────────────────┐
│                   基础设施层 (Infrastructure)                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │数据访问   │  │能力系统   │  │环境管理   │  │MAF集成   │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
                              ↕
┌─────────────────────────────────────────────────────────────┐
│                     数据层 (PostgreSQL)                       │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │Agent表    │  │协作表     │  │消息表     │  │配置表     │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 核心模块

#### 2.2.1 Agent模块
负责Agent的创建、配置、生命周期管理。

#### 2.2.2 协作模块
管理多Agent协作，包括团队组建、流程编排、任务分配。

#### 2.2.3 Skill模块
加载、管理、执行自定义Skill。

#### 2.2.4 能力模块
提供内置能力（文件、Git、文档操作）。

#### 2.2.5 环境模块
管理运行时环境（Python、Node.js等）。

#### 2.2.6 工作流模块
编排和执行Agent协作流程。

#### 2.2.7 监控模块
实时监控Agent对话、执行状态。

### 2.3 设计原则

1. **模块化**: 各模块职责清晰，低耦合高内聚
2. **可扩展**: 支持自定义Skill和能力扩展
3. **高性能**: 使用Dapper、异步编程、缓存优化
4. **安全性**: 权限验证、路径检查、操作日志
5. **易用性**: Web界面管理，可视化操作

---

## 3. Agent系统

### 3.1 Agent数据模型（使用现有表结构）

**重要说明**：以下使用项目现有的数据库表结构，不重新设计。

```sql
-- 智能体表（现有）
CREATE TABLE agents (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    type VARCHAR(50) NOT NULL,
    avatar VARCHAR(10),
    status VARCHAR(20) DEFAULT 'Inactive',
    
    -- 主模型配置
    llm_config_id BIGINT REFERENCES llm_configs(id),
    llm_model_config_id BIGINT REFERENCES llm_model_configs(id),
    llm_config_name VARCHAR(100),  -- 冗余字段，优化查询
    llm_model_name VARCHAR(100),   -- 冗余字段，优化查询
    
    -- 副模型配置（JSON格式，包含优先级）
    fallback_models TEXT,  -- JSON数组：[{"llmConfigId":1,"llmConfigName":"名称","llmModelConfigId":2,"modelName":"模型名","priority":1}]
    
    -- 系统提示
    system_prompt TEXT,
    
    -- 创建信息
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by BIGINT,
    
    -- 索引
    INDEX idx_agents_type (type),
    INDEX idx_agents_status (status)
);
```

### 3.2 Agent类型（使用现有agent_types表）

**重要说明**：系统使用现有的`agent_types`表管理Agent类型，不重新设计。

#### 3.2.1 agent_types表结构

```sql
CREATE TABLE agent_types (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,           -- 类型名称
    code VARCHAR(50) NOT NULL,            -- 类型代码（唯一）
    description TEXT,                      -- 类型描述
    icon VARCHAR(50),                      -- 图标
    default_configuration TEXT,            -- 默认配置（JSON格式）
    llm_config_id BIGINT,                  -- 默认LLM配置ID
    is_system BOOLEAN DEFAULT false,       -- 是否系统内置
    is_enabled BOOLEAN DEFAULT true,       -- 是否启用
    sort_order INTEGER DEFAULT 0,          -- 排序
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX ix_agent_types_code ON agent_types(code);
```

#### 3.2.2 默认配置字段说明

`default_configuration`字段存储JSON格式的默认配置：

```json
{
  "systemPrompt": "你是一个智能助手，能够回答问题、提供建议。",
  "temperature": 0.7,
  "maxTokens": 4096
}
```

#### 3.2.3 系统预定义Agent类型

| 类型代码 | 类型名称 | 描述 | 默认系统提示 |
|---------|---------|------|-------------|
| **Assistant** | 通用助手 | 通用AI助手 | 你是一个智能助手，能够回答问题、提供建议。 |
| **Developer** | 开发专家 | 软件开发专家 | 你是一位资深软件工程师，精通多种编程语言和框架。 |
| **Architect** | 架构师 | 系统架构设计 | 你是一位系统架构师，擅长设计可扩展、高性能的系统架构。 |
| **UIDesigner** | UI设计师 | 界面设计专家 | 你是一位专业的UI设计师，精通视觉设计和用户体验。 |
| **ProductManager** | 产品经理 | 产品规划专家 | 你是一位产品经理，擅长需求分析和产品规划。 |
| **Tester** | 测试工程师 | 测试专家 | 你是一位测试工程师，擅长编写测试用例和发现Bug。 |
| **DataAnalyst** | 数据分析师 | 数据分析专家 | 你是一位数据分析师，擅长数据挖掘和可视化。 |

#### 3.2.4 Agent类型与Agent的关系

```
agent_types表（定义Agent类型）
    ↓
agents表（创建Agent时选择类型）
    ├── type字段：存储agent_types.code
    ├── type_name字段：冗余存储agent_types.name（优化查询）
    └── system_prompt字段：可自定义，默认使用agent_types.default_configuration
```

### 3.3 Agent创建流程

```csharp
// MAFStudio.Application/Services/AgentService.cs
public class AgentService : IAgentService
{
    private readonly IAgentRepository _agentRepository;
    private readonly ILlmConfigRepository _llmConfigRepository;
    private readonly IAgentFactoryService _agentFactory;

    /// <summary>
    /// 创建Agent
    /// </summary>
    public async Task<AgentVo> CreateAgentAsync(CreateAgentRequest request)
    {
        // 1. 验证LLM配置
        var llmConfig = await _llmConfigRepository.GetByIdAsync(request.LlmConfigId);
        if (llmConfig == null)
        {
            throw new BusinessException("LLM配置不存在");
        }

        // 2. 验证主模型
        var primaryModel = await _llmModelConfigRepository.GetByIdAsync(request.LlmModelConfigId);
        if (primaryModel == null)
        {
            throw new BusinessException("主模型不存在");
        }

        // 3. 创建Agent实体
        var agent = new Agent
        {
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Avatar = request.Avatar ?? "🤖",
            Status = "Inactive",
            LlmConfigId = request.LlmConfigId,
            LlmModelConfigId = request.LlmModelConfigId,
            LlmConfigName = llmConfig.Name,
            PrimaryModelName = primaryModel.DisplayName,
            SystemPrompt = request.SystemPrompt,
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUser.Id
        };

        // 4. 保存到数据库
        await _agentRepository.InsertAsync(agent);

        // 5. 保存副模型配置
        if (request.FallbackModels != null && request.FallbackModels.Any())
        {
            await SaveFallbackModelsAsync(agent.Id, request.FallbackModels);
        }

        // 6. 返回VO
        return MapToVo(agent);
    }

    /// <summary>
    /// 保存副模型配置
    /// </summary>
    private async Task SaveFallbackModelsAsync(long agentId, List<FallbackModelRequest> fallbackModels)
    {
        var models = fallbackModels.Select((fm, index) => new AgentFallbackModel
        {
            AgentId = agentId,
            LlmConfigId = fm.LlmConfigId,
            LlmConfigName = fm.LlmConfigName,
            LlmModelConfigId = fm.LlmModelConfigId,
            ModelName = fm.ModelName,
            Priority = index + 1
        }).ToList();

        await _fallbackModelRepository.InsertBatchAsync(models);
    }
}
```

### 3.4 Agent工厂服务（基于MAF框架）

**重要说明**：以下实现完全基于Microsoft Agent Framework (MAF)提供的API。

#### 3.4.1 Agent创建流程

Agent通过以下步骤连接大模型：

1. **查询Agent配置** - 从`agents`表获取Agent基本信息
2. **查询LLM配置** - 从`llm_configs`表获取API密钥、端点等连接信息
3. **查询模型配置** - 从`llm_model_configs`表获取模型名称、参数等信息
4. **创建ChatClient** - 使用MAF的API创建客户端
5. **创建AIAgent** - 使用MAF的`AsAIAgent()`方法创建Agent实例

#### 3.4.2 数据库表关系

```
agents表
├── llm_config_id → llm_configs表（获取API密钥、端点、Provider等）
└── llm_model_config_id → llm_model_configs表（获取模型名称、参数等）
```

#### 3.4.3 代码实现

```csharp
// MAFStudio.Application/Services/AgentFactoryService.cs
using Microsoft.Agents.AI;

public class AgentFactoryService : IAgentFactoryService
{
    private readonly IAgentRepository _agentRepository;
    private readonly ILlmConfigRepository _llmConfigRepository;
    private readonly ILlmModelConfigRepository _llmModelConfigRepository;
    private readonly ICapabilityRegistry _capabilityRegistry;
    private readonly ISkillLoader _skillLoader;

    /// <summary>
    /// 创建可执行的AI Agent实例（使用MAF的AsAIAgent方法）
    /// </summary>
    public async Task<AIAgent> CreateAgentAsync(long agentId)
    {
        // 1. 查询Agent配置（从agents表）
        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent == null)
        {
            throw new NotFoundException($"Agent {agentId} not found");
        }

        // 2. 查询LLM配置（从llm_configs表）
        // 获取：API密钥、端点、Provider等信息
        var llmConfig = await _llmConfigRepository.GetByIdAsync(agent.LlmConfigId.Value);
        if (llmConfig == null)
        {
            throw new NotFoundException($"LLM配置 {agent.LlmConfigId} 不存在");
        }

        // 3. 查询模型配置（从llm_model_configs表）
        // 获取：模型名称、温度、MaxToken、上下文窗口等信息
        var primaryModel = await _llmModelConfigRepository.GetByIdAsync(agent.LlmModelConfigId.Value);
        if (primaryModel == null)
        {
            throw new NotFoundException($"模型配置 {agent.LlmModelConfigId} 不存在");
        }

        // 4. 创建ChatClient（使用MAF支持的Provider）
        var chatClient = CreateChatClient(llmConfig, primaryModel);

        // 5. 使用MAF的AsAIAgent方法创建Agent
        var aiAgent = chatClient.AsAIAgent(
            instructions: agent.SystemPrompt,
            name: agent.Name
        );

        // 6. 注册内置能力（使用MAF的WithTools方法）
        var builtInTools = _capabilityRegistry.GetBuiltInTools();
        if (builtInTools.Any())
        {
            aiAgent = aiAgent.WithTools(builtInTools);
        }

        // 7. 加载并注册自定义Skills
        var skills = await _skillLoader.LoadSkillsForAgentAsync(agentId);
        if (skills.Any())
        {
            var skillTools = CreateSkillTools(skills);
            aiAgent = aiAgent.WithTools(skillTools);
        }

        return aiAgent;
    }

    /// <summary>
    /// 创建ChatClient（基于MAF支持的Provider）
    /// </summary>
    private IChatClient CreateChatClient(LlmConfig config, LlmModelConfig model)
    {
        // MAF支持以下Provider：
        // 1. Azure OpenAI - 使用AzureOpenAIClient
        // 2. OpenAI - 使用OpenAIClient
        // 3. Azure AI Foundry - 使用特定的Foundry客户端
        // 4. 自定义 - 实现IChatClient接口
        
        return config.Provider.ToLower() switch
        {
            "azure" => new AzureOpenAIClient(
                new Uri(config.Endpoint),      // 从llm_configs表获取
                new AzureCliCredential()
            ).GetChatClient(model.ModelName),  // 从llm_model_configs表获取
            
            "openai" => new OpenAIClient(
                config.ApiKey                  // 从llm_configs表获取
            ).GetChatClient(model.ModelName),  // 从llm_model_configs表获取
            
            // 对于其他Provider（如通义千问、DeepSeek等）
            // 需要实现自定义的IChatClient
            "qwen" => CreateCustomChatClient(config, model),
            "deepseek" => CreateCustomChatClient(config, model),
            
            _ => throw new NotSupportedException($"不支持的LLM提供商: {config.Provider}")
        };
    }

    /// <summary>
    /// 创建自定义ChatClient（用于非MAF原生支持的Provider）
    /// </summary>
    private IChatClient CreateCustomChatClient(LlmConfig config, LlmModelConfig model)
    {
        // 实现自定义IChatClient接口
        // 这样可以让MAF支持任何兼容OpenAI API的Provider
        return new CustomOpenAICompatibleClient(
            config.ApiKey,        // 从llm_configs表获取
            config.ApiBaseUrl,    // 从llm_configs表获取
            model.ModelName       // 从llm_model_configs表获取
        );
    }
}
```

#### 3.4.4 相关数据库表

**llm_configs表**（存储LLM提供商配置）
```sql
CREATE TABLE llm_configs (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    provider VARCHAR(50) NOT NULL,      -- 提供商：openai, azure, qwen, deepseek等
    api_key TEXT,                        -- API密钥
    endpoint TEXT,                       -- API端点
    api_base_url TEXT,                   -- API基础URL
    user_id BIGINT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

**llm_model_configs表**（存储具体模型配置）
```sql
CREATE TABLE llm_model_configs (
    id BIGSERIAL PRIMARY KEY,
    llm_config_id BIGINT NOT NULL REFERENCES llm_configs(id),
    model_name VARCHAR(100) NOT NULL,    -- 模型名称
    display_name VARCHAR(100),           -- 显示名称
    temperature DECIMAL(3,2) DEFAULT 0.7, -- 温度参数
    max_tokens INTEGER DEFAULT 4096,      -- 最大Token数
    context_window INTEGER DEFAULT 64000, -- 上下文窗口大小
    test_status INTEGER DEFAULT 0,        -- 测试状态：0未测试，1可用，2不可用
    test_time TIMESTAMP,                  -- 最后测试时间
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

---

## 4. 多Agent协作

### 4.1 协作模式

#### 4.1.1 Sequential（顺序执行）

多个Agent按顺序依次执行任务。

```
用户请求 → Agent1 → Agent2 → Agent3 → 最终结果
```

**适用场景**：
- 流水线式任务处理
- 需要逐步加工的场景
- 有明确依赖关系的任务

**示例**：产品文档编写流程
```
产品经理编写需求 → 架构师设计架构 → 开发编写代码 → 测试编写测试用例
```

#### 4.1.2 Concurrent（并发执行）

多个Agent同时执行任务，最后合并结果。

```
用户请求 → Agent1 ↘
                 → 合并结果 → 最终结果
           Agent2 ↗
```

**适用场景**：
- 独立任务并行处理
- 需要多个专业视角的场景
- 提高执行效率

**示例**：代码审查
```
开发A审查代码逻辑 ↘
                   → 综合审查报告
开发B审查代码风格 ↗
```

#### 4.1.3 Handoffs（移交）

Agent之间相互移交任务。

```
Agent1 → Agent2 → Agent1 → Agent3 → 最终结果
```

**适用场景**：
- 需要反复修改的场景
- 协作式问题解决
- 动态任务分配

**示例**：文档审核流程
```
产品经理编写文档 → 技术总监审核 → (不通过) → 产品经理修改 → 技术总监审核 → (通过) → 发布
```

#### 4.1.4 Group Chat（群聊）

多个Agent在群组中自由对话。

```
用户 → 群组 → Agent1, Agent2, Agent3 自由讨论 → 最终结果
```

**适用场景**：
- 头脑风暴
- 多角度分析
- 开放式讨论

**示例**：技术方案讨论
```
用户：如何设计一个高并发系统？
架构师：建议使用微服务架构...
开发：技术栈选择Spring Cloud...
运维：容器化部署...
```

### 4.2 协作数据模型

```sql
-- 协作会话表
CREATE TABLE collaborations (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    workflow_type VARCHAR(50) NOT NULL, -- Sequential, Concurrent, Handoffs, GroupChat
    status VARCHAR(20) DEFAULT 'Active',
    
    -- 配置
    config JSONB, -- 工作流配置
    
    -- 创建信息
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by BIGINT,
    
    INDEX idx_collaborations_status (status)
);

-- 协作成员表
CREATE TABLE collaboration_members (
    id BIGSERIAL PRIMARY KEY,
    collaboration_id BIGINT NOT NULL REFERENCES collaborations(id) ON DELETE CASCADE,
    agent_id BIGINT NOT NULL REFERENCES agents(id),
    role VARCHAR(50), -- Leader, Member
    order_index INT, -- 执行顺序
    
    INDEX idx_members_collaboration (collaboration_id)
);

-- 协作消息表
CREATE TABLE collaboration_messages (
    id BIGSERIAL PRIMARY KEY,
    collaboration_id BIGINT NOT NULL REFERENCES collaborations(id) ON DELETE CASCADE,
    agent_id BIGINT REFERENCES agents(id),
    role VARCHAR(20) NOT NULL, -- User, Assistant, System
    content TEXT NOT NULL,
    metadata JSONB, -- 额外信息
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_messages_collaboration (collaboration_id),
    INDEX idx_messages_created (created_at)
);

-- 协作执行记录表
CREATE TABLE collaboration_executions (
    id BIGSERIAL PRIMARY KEY,
    collaboration_id BIGINT NOT NULL REFERENCES collaborations(id),
    status VARCHAR(20), -- Running, Completed, Failed
    start_time TIMESTAMP,
    end_time TIMESTAMP,
    result TEXT,
    error TEXT,
    
    INDEX idx_executions_collaboration (collaboration_id)
);
```

### 4.3 协作服务实现（基于MAF框架）

**重要说明**：以下实现完全基于Microsoft Agent Framework (MAF)提供的API，不闭门造车。

```csharp
// MAFStudio.Application/Services/CollaborationService.cs
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

public class CollaborationService : ICollaborationService
{
    private readonly IAgentFactoryService _agentFactory;
    private readonly ICollaborationRepository _collaborationRepo;
    private readonly IMessageRepository _messageRepo;

    /// <summary>
    /// 执行顺序工作流（使用MAF的AgentWorkflowBuilder）
    /// </summary>
    public async Task<SequentialResult> ExecuteSequentialAsync(
        long collaborationId,
        string userInput)
    {
        // 1. 加载协作配置
        var collaboration = await _collaborationRepo.GetByIdAsync(collaborationId);
        var members = await _collaborationRepo.GetMembersAsync(collaborationId);

        // 2. 使用MAF的AgentWorkflowBuilder创建工作流
        var builder = AgentWorkflowBuilder.Create();

        // 3. 按顺序添加Agent
        foreach (var member in members.OrderBy(m => m.OrderIndex))
        {
            var agent = await _agentFactory.CreateAgentAsync(member.AgentId);
            builder.AddAgent(agent);
        }

        // 4. 构建并执行工作流
        var workflow = builder.Build();
        var result = await workflow.RunAsync(userInput);

        // 5. 保存消息记录
        await SaveMessageAsync(collaborationId, null, "User", userInput);

        return new SequentialResult
        {
            Success = true,
            FinalResult = result,
            Messages = new List<ChatMessage> { new ChatMessage("assistant", result) }
        };
    }

    /// <summary>
    /// 执行并发工作流（使用MAF的WorkflowMode.Concurrent）
    /// </summary>
    public async Task<ConcurrentResult> ExecuteConcurrentAsync(
        long collaborationId,
        string userInput)
    {
        var collaboration = await _collaborationRepo.GetByIdAsync(collaborationId);
        var members = await _collaborationRepo.GetMembersAsync(collaborationId);

        // 使用MAF的AgentWorkflowBuilder创建并发工作流
        var builder = AgentWorkflowBuilder.Create();

        foreach (var member in members)
        {
            var agent = await _agentFactory.CreateAgentAsync(member.AgentId);
            builder.AddAgent(agent);
        }

        // 设置为并发模式
        var workflow = builder
            .WithMode(WorkflowMode.Concurrent)
            .Build();

        var results = await workflow.RunAsync(userInput);

        return new ConcurrentResult
        {
            Success = true,
            Results = results,
            MergedResult = string.Join("\n\n", results)
        };
    }

    /// <summary>
    /// 执行移交工作流（使用MAF的AddHandoffs）
    /// </summary>
    public async Task<HandoffsResult> ExecuteHandoffsAsync(
        long collaborationId,
        string userInput,
        int maxIterations = 10)
    {
        var collaboration = await _collaborationRepo.GetByIdAsync(collaborationId);
        var members = await _collaborationRepo.GetMembersAsync(collaborationId);

        // 找到主Agent（Leader）
        var leaderMember = members.First(m => m.Role == "Leader");
        var leaderAgent = await _agentFactory.CreateAgentAsync(leaderMember.AgentId);

        // 找到其他Agent
        var otherAgents = new List<AIAgent>();
        foreach (var member in members.Where(m => m.Role != "Leader"))
        {
            otherAgents.Add(await _agentFactory.CreateAgentAsync(member.AgentId));
        }

        // 使用MAF的AddHandoffs方法设置移交
        leaderAgent.AddHandoffs(otherAgents);

        // 执行
        var result = await leaderAgent.RunAsync(userInput);

        return new HandoffsResult
        {
            Success = true,
            FinalResult = result
        };
    }

    /// <summary>
    /// 执行群聊工作流（使用MAF的AgentGroupChat）
    /// </summary>
    public async Task<GroupChatResult> ExecuteGroupChatAsync(
        long collaborationId,
        string userInput,
        int maxRounds = 10)
    {
        var collaboration = await _collaborationRepo.GetByIdAsync(collaborationId);
        var members = await _collaborationRepo.GetMembersAsync(collaborationId);

        // 创建所有Agent实例
        var agents = new List<AIAgent>();
        foreach (var member in members)
        {
            agents.Add(await _agentFactory.CreateAgentAsync(member.AgentId));
        }

        // 使用MAF的AgentGroupChat创建群聊
        var groupChat = new AgentGroupChat(agents: agents);

        // 流式执行群聊
        var messages = new List<GroupChatMessage>();
        await foreach (var message in groupChat.RunStreamingAsync(userInput))
        {
            messages.Add(new GroupChatMessage
            {
                Sender = message.Sender,
                Content = message.Content,
                Timestamp = DateTime.Now
            });

            // 实时保存消息
            await SaveMessageAsync(
                collaborationId,
                null,
                "Assistant",
                $"{message.Sender}: {message.Content}"
            );
        }

        return new GroupChatResult
        {
            Success = true,
            Messages = messages,
            FinalResult = messages.LastOrDefault()?.Content ?? ""
        };
    }
}
```

---

## 5. Skill系统

### 5.1 Skill标准

Skill遵循社区标准的SKILL.md格式：

```
skill-name/
├── SKILL.md          # Skill元数据和说明
├── scripts/          # 执行脚本（可选）
│   ├── main.py
│   └── utils.py
├── references/       # 参考文档（可选）
│   └── docs.md
└── examples/         # 示例（可选）
    └── example.json
```

### 5.2 SKILL.md格式

```markdown
---
name: skill-name
description: Skill description
version: 1.0.0
author: Author Name
environment:
  runtime: python
  version: "3.11"
  dependencies:
    - pandas>=2.0.0
    - requests>=2.31.0
---

# Skill Title

## When to Use
- Scenario 1
- Scenario 2

## Scripts Usage
```bash
python scripts/main.py --param1 "{value1}" --param2 "{value2}"
```

## Example
User: "User request"
Action: Execute skill with parameters
Expected: Expected result
```

### 5.3 Skill加载器

```csharp
// MAFStudio.Infrastructure/Skills/SkillLoader.cs
public class SkillLoader : ISkillLoader
{
    private readonly string _skillsDirectory;
    private readonly ILogger<SkillLoader> _logger;

    public SkillLoader(IConfiguration configuration, ILogger<SkillLoader> logger)
    {
        _skillsDirectory = configuration["Skills:Directory"] ?? "/app/skills";
        _logger = logger;
    }

    /// <summary>
    /// 加载所有Skills
    /// </summary>
    public async Task<List<Skill>> LoadAllSkillsAsync()
    {
        var skills = new List<Skill>();

        if (!Directory.Exists(_skillsDirectory))
        {
            _logger.LogWarning("Skills directory not found: {Directory}", _skillsDirectory);
            return skills;
        }

        var skillDirectories = Directory.GetDirectories(_skillsDirectory);

        foreach (var skillDir in skillDirectories)
        {
            try
            {
                var skill = await LoadSkillAsync(skillDir);
                if (skill != null)
                {
                    skills.Add(skill);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load skill from {Directory}", skillDir);
            }
        }

        return skills;
    }

    /// <summary>
    /// 加载单个Skill
    /// </summary>
    public async Task<Skill?> LoadSkillAsync(string skillDirectory)
    {
        var skillMdPath = Path.Combine(skillDirectory, "SKILL.md");
        if (!File.Exists(skillMdPath))
        {
            _logger.LogWarning("SKILL.md not found in {Directory}", skillDirectory);
            return null;
        }

        var content = await File.ReadAllTextAsync(skillMdPath);
        var skill = ParseSkillMd(content);

        skill.SkillDirectory = skillDirectory;
        skill.HasScripts = Directory.Exists(Path.Combine(skillDirectory, "scripts"));

        return skill;
    }

    /// <summary>
    /// 解析SKILL.md
    /// </summary>
    private Skill ParseSkillMd(string content)
    {
        var skill = new Skill();

        // 解析YAML前置数据
        var frontMatterMatch = Regex.Match(content, @"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline);
        if (frontMatterMatch.Success)
        {
            var yaml = frontMatterMatch.Groups[1].Value;
            var yamlDoc = new YamlStream();
            yamlDoc.Load(new StringReader(yaml));

            var root = (YamlMappingNode)yamlDoc.Documents[0].RootNode;

            skill.Name = root.Children.TryGetValue("name", out var name) ? name.ToString() : "";
            skill.Description = root.Children.TryGetValue("description", out var desc) ? desc.ToString() : "";
            skill.Version = root.Children.TryGetValue("version", out var version) ? version.ToString() : "1.0.0";
            skill.Author = root.Children.TryGetValue("author", out var author) ? author.ToString() : "";

            // 解析环境配置
            if (root.Children.TryGetValue("environment", out var env))
            {
                var envNode = (YamlMappingNode)env;
                skill.Environment = new SkillEnvironment
                {
                    Runtime = envNode.Children.TryGetValue("runtime", out var runtime) ? runtime.ToString() : "python",
                    Version = envNode.Children.TryGetValue("version", out var ver) ? ver.ToString() : null,
                    Dependencies = envNode.Children.TryGetValue("dependencies", out var deps)
                        ? ((YamlSequenceNode)deps).Children.Select(c => c.ToString()).ToList()
                        : new List<string>()
                };
            }
        }

        // 解析Markdown内容
        var markdownContent = frontMatterMatch.Success
            ? content.Substring(frontMatterMatch.Length)
            : content;

        skill.MarkdownContent = markdownContent;

        return skill;
    }
}
```

### 5.4 Skill执行器

```csharp
// MAFStudio.Infrastructure/Skills/SkillExecutor.cs
public class SkillExecutor : ISkillExecutor
{
    private readonly IEnvironmentManager _environmentManager;
    private readonly ILogger<SkillExecutor> _logger;

    /// <summary>
    /// 执行Skill
    /// </summary>
    public async Task<SkillExecutionResult> ExecuteAsync(
        Skill skill,
        Dictionary<string, object> parameters)
    {
        try
        {
            if (skill.HasScripts)
            {
                // 执行脚本
                return await ExecuteScriptsAsync(skill, parameters);
            }
            else
            {
                // 使用LLM执行
                return await ExecuteWithLLMAsync(skill, parameters);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Skill execution failed: {SkillName}", skill.Name);
            return new SkillExecutionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 执行脚本
    /// </summary>
    private async Task<SkillExecutionResult> ExecuteScriptsAsync(
        Skill skill,
        Dictionary<string, object> parameters)
    {
        // 1. 准备环境
        var envInfo = await _environmentManager.PrepareEnvironmentAsync(skill);

        // 2. 构建命令
        var command = BuildCommand(skill, parameters);

        // 3. 执行脚本
        var processInfo = new ProcessStartInfo
        {
            FileName = GetInterpreter(skill.Environment.Runtime, envInfo),
            Arguments = command,
            WorkingDirectory = skill.SkillDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new SkillExecutionResult
        {
            Success = process.ExitCode == 0,
            Output = output,
            Error = error
        };
    }

    /// <summary>
    /// 使用LLM执行
    /// </summary>
    private async Task<SkillExecutionResult> ExecuteWithLLMAsync(
        Skill skill,
        Dictionary<string, object> parameters)
    {
        // 构建提示词
        var prompt = $@"
你是一个执行器，正在执行Skill: {skill.Name}

Skill说明:
{skill.MarkdownContent}

参数:
{JsonSerializer.Serialize(parameters, new JsonSerializerOptions { WriteIndented = true })}

请根据Skill说明和参数执行任务，并返回结果。
";

        // 调用LLM
        var response = await _chatClient.CompleteAsync(prompt);

        return new SkillExecutionResult
        {
            Success = true,
            Output = response.Content
        };
    }
}
```

### 5.5 Skill示例

#### 示例1：数据分析Skill

```
data-analysis/
├── SKILL.md
├── scripts/
│   ├── main.py
│   └── utils.py
└── references/
    └── pandas-docs.md
```

**SKILL.md**:
```markdown
---
name: data-analysis
description: Analyze data files and generate insights
version: 1.0.0
author: MAF Studio
environment:
  runtime: python
  version: "3.11"
  dependencies:
    - pandas>=2.0.0
    - matplotlib>=3.7.0
    - seaborn>=0.12.0
---

# Data Analysis Skill

## When to Use
- User wants to analyze CSV/Excel data
- User needs data visualization
- User wants statistical insights

## Scripts Usage
```bash
python scripts/main.py analyze --file "{file_path}" --output "{output_path}"
```

## Example
User: "分析 /data/sales.csv 文件"
Action: Execute skill with file_path="/data/sales.csv", output_path="/output"
Expected: Statistical summary and visualizations
```

**scripts/main.py**:
```python
#!/usr/bin/env python3
import argparse
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
import json
import os

def analyze_data(file_path, output_path=None):
    """分析数据文件"""
    # 读取数据
    if file_path.endswith('.csv'):
        df = pd.read_csv(file_path)
    elif file_path.endswith('.xlsx'):
        df = pd.read_excel(file_path)
    else:
        return {"success": False, "error": "Unsupported file format"}

    # 统计分析
    summary = df.describe()

    # 数据类型
    dtypes = df.dtypes.to_dict()

    # 缺失值
    missing = df.isnull().sum().to_dict()

    # 生成可视化
    if output_path:
        os.makedirs(output_path, exist_ok=True)

        # 数值列分布
        numeric_cols = df.select_dtypes(include=['number']).columns
        if len(numeric_cols) > 0:
            fig, axes = plt.subplots(len(numeric_cols), 1, figsize=(10, 4*len(numeric_cols)))
            for i, col in enumerate(numeric_cols):
                ax = axes[i] if len(numeric_cols) > 1 else axes
                df[col].hist(ax=ax, bins=30)
                ax.set_title(f'Distribution of {col}')
            plt.tight_layout()
            plt.savefig(f"{output_path}/distributions.png")

        # 相关性矩阵
        if len(numeric_cols) > 1:
            plt.figure(figsize=(10, 8))
            sns.heatmap(df[numeric_cols].corr(), annot=True, cmap='coolwarm')
            plt.title('Correlation Matrix')
            plt.savefig(f"{output_path}/correlation.png")

    return {
        "success": True,
        "summary": summary.to_dict(),
        "dtypes": {k: str(v) for k, v in dtypes.items()},
        "missing": missing,
        "rows": len(df),
        "columns": list(df.columns)
    }

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("command", choices=["analyze"])
    parser.add_argument("--file", required=True)
    parser.add_argument("--output", required=False)
    args = parser.parse_args()

    if args.command == "analyze":
        result = analyze_data(args.file, args.output)
        print(json.dumps(result, indent=2, default=str))
```

---

## 6. 能力系统

### 6.1 内置能力

系统内置以下能力：

| 能力类别 | 功能 | 描述 |
|---------|------|------|
| **文件操作** | ReadFile | 读取文件内容 |
| | WriteFile | 写入文件内容 |
| | ListFiles | 列出目录文件 |
| | DeleteFile | 删除文件 |
| **Git操作** | CloneRepository | 克隆仓库 |
| | CommitChanges | 提交更改 |
| | PushChanges | 推送到远程 |
| | PullChanges | 拉取更新 |
| **文档操作** | ReadWordDocument | 读取Word文档 |
| | CreateWordDocument | 创建Word文档 |
| | ReadExcelDocument | 读取Excel文档 |
| | CreateExcelDocument | 创建Excel文档 |
| **HTTP请求** | HttpGet | 发送GET请求 |
| | HttpPost | 发送POST请求 |
| | HttpPut | 发送PUT请求 |
| | HttpDelete | 发送DELETE请求 |

### 6.2 能力注册表

```csharp
// MAFStudio.Infrastructure/Capabilities/CapabilityRegistry.cs
public class CapabilityRegistry : ICapabilityRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CapabilityRegistry> _logger;

    /// <summary>
    /// 获取所有内置工具
    /// </summary>
    public MethodInfo[] GetBuiltInTools()
    {
        var tools = new List<MethodInfo>();

        // 文件操作
        tools.AddRange(GetMethods<FileOperations>());

        // Git操作
        tools.AddRange(GetMethods<GitOperations>());

        // 文档操作
        tools.AddRange(GetMethods<DocumentOperations>());

        // HTTP操作
        tools.AddRange(GetMethods<HttpOperations>());

        return tools.ToArray();
    }

    private MethodInfo[] GetMethods<T>()
    {
        return typeof(T)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<DescriptionAttribute>() != null)
            .ToArray();
    }
}
```

### 6.3 安全验证

```csharp
// MAFStudio.Infrastructure/Security/SecurityValidator.cs
public class SecurityValidator : ISecurityValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecurityValidator> _logger;

    /// <summary>
    /// 验证文件路径
    /// </summary>
    public async Task<ValidationResult> ValidateFilePathAsync(string filePath, string operation)
    {
        // 1. 路径规范化
        var normalizedPath = Path.GetFullPath(filePath);

        // 2. 检查是否在允许的目录内
        var allowedDirectories = _configuration.GetSection("Security:AllowedDirectories")
            .Get<string[]>() ?? new[] { "/app/data", "/app/output", "/app/uploads" };

        var isAllowed = allowedDirectories.Any(allowed =>
            normalizedPath.StartsWith(Path.GetFullPath(allowed), StringComparison.OrdinalIgnoreCase));

        if (!isAllowed)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = $"路径不在允许的目录内: {filePath}"
            };
        }

        // 3. 检查敏感文件
        var sensitivePatterns = new[] { "*.key", "*.pem", "*.env", "*.config" };
        var fileName = Path.GetFileName(normalizedPath);

        if (sensitivePatterns.Any(pattern => 
            Regex.IsMatch(fileName, pattern.Replace("*", ".*"))))
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = $"不允许访问敏感文件: {fileName}"
            };
        }

        // 4. 检查操作权限
        var permissions = _configuration.GetSection($"Security:Permissions:{operation}")
            .Get<string[]>();

        if (permissions != null && !permissions.Contains("*"))
        {
            // 检查用户是否有权限
            // TODO: 实现权限检查
        }

        return new ValidationResult
        {
            IsValid = true
        };
    }
}
```

---

## 7. 环境管理

### 7.1 环境管理架构

```
┌─────────────────────────────────────────┐
│         Web界面 - 环境管理                │
│  - 安装Python/Node.js/Bash              │
│  - 管理全局包                            │
│  - 查看已安装环境                        │
└─────────────────────────────────────────┘
                    ↕
┌─────────────────────────────────────────┐
│         环境管理服务                      │
│  - 运行时检测                            │
│  - 包管理                                │
│  - 代理配置                              │
└─────────────────────────────────────────┘
                    ↕
┌─────────────────────────────────────────┐
│         Docker容器                       │
│  - Python 3.11                          │
│  - Node.js 18                           │
│  - 全局包                                │
└─────────────────────────────────────────┘
```

### 7.2 镜像源配置

```json
// appsettings.json
{
  "Environment": {
    "Proxy": {
      "Enabled": false,
      "Http": "http://127.0.0.1:7890",
      "Https": "http://127.0.0.1:7890"
    },
    "Pip": {
      "IndexUrl": "https://pypi.tuna.tsinghua.edu.cn/simple",
      "TrustedHost": "pypi.tuna.tsinghua.edu.cn"
    },
    "Npm": {
      "Registry": "https://registry.npmmirror.com"
    },
    "Apt": {
      "Source": "http://mirrors.aliyun.com/ubuntu/"
    }
  }
}
```

### 7.3 环境管理服务

```csharp
// MAFStudio.Application/Services/EnvironmentManagementService.cs
public class EnvironmentManagementService : IEnvironmentManagementService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnvironmentManagementService> _logger;

    /// <summary>
    /// 获取已安装的运行时
    /// </summary>
    public async Task<List<RuntimeInfo>> GetInstalledRuntimesAsync()
    {
        var runtimes = new List<RuntimeInfo>();

        // 检查Python
        var pythonVersion = await GetCommandOutputAsync("python --version");
        if (!string.IsNullOrEmpty(pythonVersion))
        {
            runtimes.Add(new RuntimeInfo
            {
                Name = "Python",
                Version = ExtractVersion(pythonVersion),
                Path = await GetCommandOutputAsync("which python"),
                Status = "Installed"
            });
        }

        // 检查Node.js
        var nodeVersion = await GetCommandOutputAsync("node --version");
        if (!string.IsNullOrEmpty(nodeVersion))
        {
            runtimes.Add(new RuntimeInfo
            {
                Name = "Node.js",
                Version = ExtractVersion(nodeVersion),
                Path = await GetCommandOutputAsync("which node"),
                Status = "Installed"
            });
        }

        return runtimes;
    }

    /// <summary>
    /// 安装Python包
    /// </summary>
    public async Task<InstallResult> InstallPythonPackageAsync(string packageName, string? version)
    {
        var pipIndexUrl = _configuration["Environment:Pip:IndexUrl"];
        var pipTrustedHost = _configuration["Environment:Pip:TrustedHost"];

        var installCmd = string.IsNullOrEmpty(version)
            ? $"pip install {packageName} -i {pipIndexUrl} --trusted-host {pipTrustedHost}"
            : $"pip install {packageName}=={version} -i {pipIndexUrl} --trusted-host {pipTrustedHost}";

        var result = await ExecuteCommandAsync(installCmd);

        return new InstallResult
        {
            Success = result.ExitCode == 0,
            Message = result.ExitCode == 0 ? "安装成功" : result.Error
        };
    }

    /// <summary>
    /// 执行命令
    /// </summary>
    public async Task<CommandResult> ExecuteCommandAsync(string command)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new CommandResult
        {
            ExitCode = process.ExitCode,
            Output = output,
            Error = error
        };
    }
}
```

---

## 8. 工作流引擎

### 8.1 工作流定义

```csharp
// MAFStudio.Application/Workflows/WorkflowDefinition.cs
public class WorkflowDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public WorkflowType Type { get; set; }
    public List<WorkflowStep> Steps { get; set; }
    public Dictionary<string, object> Variables { get; set; }
}

public class WorkflowStep
{
    public string Id { get; set; }
    public string Name { get; set; }
    public long AgentId { get; set; }
    public string InputTemplate { get; set; }
    public List<string> DependsOn { get; set; }
    public WorkflowCondition? Condition { get; set; }
}

public class WorkflowCondition
{
    public string Expression { get; set; }
    public string TrueBranch { get; set; }
    public string FalseBranch { get; set; }
}
```

### 8.2 工作流执行器

```csharp
// MAFStudio.Application/Workflows/WorkflowExecutor.cs
public class WorkflowExecutor : IWorkflowExecutor
{
    private readonly IAgentFactoryService _agentFactory;
    private readonly ILogger<WorkflowExecutor> _logger;

    /// <summary>
    /// 执行工作流
    /// </summary>
    public async Task<WorkflowResult> ExecuteAsync(
        WorkflowDefinition workflow,
        Dictionary<string, object> inputs)
    {
        var context = new WorkflowContext
        {
            WorkflowId = workflow.Id,
            Variables = new Dictionary<string, object>(workflow.Variables),
            Inputs = inputs,
            Outputs = new Dictionary<string, object>(),
            ExecutionLog = new List<StepExecutionLog>()
        };

        try
        {
            // 根据工作流类型执行
            switch (workflow.Type)
            {
                case WorkflowType.Sequential:
                    await ExecuteSequentialAsync(workflow, context);
                    break;
                case WorkflowType.Concurrent:
                    await ExecuteConcurrentAsync(workflow, context);
                    break;
                case WorkflowType.Conditional:
                    await ExecuteConditionalAsync(workflow, context);
                    break;
                default:
                    throw new NotSupportedException($"不支持的工作流类型: {workflow.Type}");
            }

            return new WorkflowResult
            {
                Success = true,
                Outputs = context.Outputs,
                ExecutionLog = context.ExecutionLog
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "工作流执行失败: {WorkflowId}", workflow.Id);
            return new WorkflowResult
            {
                Success = false,
                Error = ex.Message,
                ExecutionLog = context.ExecutionLog
            };
        }
    }

    /// <summary>
    /// 执行顺序工作流
    /// </summary>
    private async Task ExecuteSequentialAsync(
        WorkflowDefinition workflow,
        WorkflowContext context)
    {
        foreach (var step in workflow.Steps)
        {
            var log = await ExecuteStepAsync(step, context);
            context.ExecutionLog.Add(log);

            if (!log.Success)
            {
                throw new Exception($"步骤执行失败: {step.Name}");
            }
        }
    }

    /// <summary>
    /// 执行步骤
    /// </summary>
    private async Task<StepExecutionLog> ExecuteStepAsync(
        WorkflowStep step,
        WorkflowContext context)
    {
        var log = new StepExecutionLog
        {
            StepId = step.Id,
            StepName = step.Name,
            StartTime = DateTime.Now
        };

        try
        {
            // 创建Agent
            var agent = await _agentFactory.CreateAgentAsync(step.AgentId);

            // 构建输入
            var input = BuildInput(step.InputTemplate, context);

            // 执行
            var response = await agent.InvokeAsync(input);

            log.EndTime = DateTime.Now;
            log.Success = true;
            log.Output = response.Content;

            // 保存输出
            context.Outputs[step.Id] = response.Content;

            return log;
        }
        catch (Exception ex)
        {
            log.EndTime = DateTime.Now;
            log.Success = false;
            log.Error = ex.Message;

            return log;
        }
    }
}
```

---

## 9. 对话流程可视化

### 9.1 消息记录

```csharp
// MAFStudio.Application/Services/MessageService.cs
public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepo;

    /// <summary>
    /// 保存消息
    /// </summary>
    public async Task SaveMessageAsync(
        long collaborationId,
        long? agentId,
        string role,
        string content,
        Dictionary<string, object>? metadata = null)
    {
        var message = new CollaborationMessage
        {
            CollaborationId = collaborationId,
            AgentId = agentId,
            Role = role,
            Content = content,
            Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
            CreatedAt = DateTime.Now
        };

        await _messageRepo.InsertAsync(message);
    }

    /// <summary>
    /// 获取对话历史
    /// </summary>
    public async Task<List<MessageVo>> GetConversationHistoryAsync(long collaborationId)
    {
        var messages = await _messageRepo.GetByCollaborationIdAsync(collaborationId);

        return messages.Select(m => new MessageVo
        {
            Id = m.Id,
            AgentId = m.AgentId,
            AgentName = m.Agent?.Name,
            Role = m.Role,
            Content = m.Content,
            Metadata = m.Metadata != null 
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(m.Metadata)
                : null,
            CreatedAt = m.CreatedAt
        }).ToList();
    }
}
```

### 9.2 实时推送

```csharp
// MAFStudio.Api/Hubs/CollaborationHub.cs
public class CollaborationHub : Hub
{
    private readonly IMessageService _messageService;

    /// <summary>
    /// 加入协作组
    /// </summary>
    public async Task JoinCollaboration(long collaborationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"collaboration_{collaborationId}");
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public async Task SendMessage(long collaborationId, MessageDto message)
    {
        // 保存消息
        await _messageService.SaveMessageAsync(
            collaborationId,
            message.AgentId,
            message.Role,
            message.Content,
            message.Metadata
        );

        // 推送给所有客户端
        await Clients.Group($"collaboration_{collaborationId}")
            .SendAsync("ReceiveMessage", message);
    }
}
```

### 9.3 前端可视化

```typescript
// frontend/src/components/ConversationFlow.tsx
import React, { useState, useEffect } from 'react';
import { Card, Timeline, Avatar, Tag, Empty } from 'antd';
import { UserOutlined, RobotOutlined } from '@ant-design/icons';

interface Message {
  id: number;
  agentId?: number;
  agentName?: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  createdAt: string;
}

const ConversationFlow: React.FC<{ collaborationId: number }> = ({ collaborationId }) => {
  const [messages, setMessages] = useState<Message[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadMessages();
    
    // 连接WebSocket
    const socket = new WebSocket(`ws://localhost:5000/hubs/collaboration`);
    
    socket.onmessage = (event) => {
      const message = JSON.parse(event.data);
      setMessages(prev => [...prev, message]);
    };

    return () => socket.close();
  }, [collaborationId]);

  const loadMessages = async () => {
    try {
      const response = await api.get(`/collaborations/${collaborationId}/messages`);
      setMessages(response.data);
    } catch (error) {
      console.error('加载消息失败', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card title="对话流程" loading={loading}>
      {messages.length === 0 ? (
        <Empty description="暂无对话记录" />
      ) : (
        <Timeline>
          {messages.map((msg, index) => (
            <Timeline.Item
              key={msg.id}
              dot={
                msg.role === 'user' ? (
                  <Avatar icon={<UserOutlined />} />
                ) : (
                  <Avatar 
                    style={{ backgroundColor: '#87d068' }}
                    icon={<RobotOutlined />}
                  />
                )
              }
            >
              <div>
                <div style={{ marginBottom: 8 }}>
                  <Tag color={msg.role === 'user' ? 'blue' : 'green'}>
                    {msg.agentName || '用户'}
                  </Tag>
                  <span style={{ marginLeft: 8, color: '#999' }}>
                    {new Date(msg.createdAt).toLocaleString()}
                  </span>
                </div>
                <div style={{ 
                  padding: 12, 
                  background: '#f5f5f5', 
                  borderRadius: 4,
                  whiteSpace: 'pre-wrap'
                }}>
                  {msg.content}
                </div>
              </div>
            </Timeline.Item>
          ))}
        </Timeline>
      )}
    </Card>
  );
};

export default ConversationFlow;
```

---

## 10. 数据库设计

### 10.1 核心表结构（使用现有表）

**重要说明**：以下使用项目现有的数据库表结构，不重新设计。

#### agents表
```sql
CREATE TABLE agents (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    type VARCHAR(50) NOT NULL,
    avatar VARCHAR(10),
    status VARCHAR(20) DEFAULT 'Inactive',
    llm_config_id BIGINT REFERENCES llm_configs(id),
    llm_model_config_id BIGINT REFERENCES llm_model_configs(id),
    llm_config_name VARCHAR(100),  -- 冗余字段
    llm_model_name VARCHAR(100),   -- 冗余字段
    fallback_models TEXT,  -- JSON格式存储副模型，包含优先级
    system_prompt TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by BIGINT
);

CREATE INDEX idx_agents_type ON agents(type);
CREATE INDEX idx_agents_status ON agents(status);
```

#### collaborations表
```sql
CREATE TABLE collaborations (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    workflow_type VARCHAR(50) NOT NULL,
    status VARCHAR(20) DEFAULT 'Active',
    config JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by BIGINT
);

CREATE INDEX idx_collaborations_status ON collaborations(status);
```

#### collaboration_messages表
```sql
CREATE TABLE collaboration_messages (
    id BIGSERIAL PRIMARY KEY,
    collaboration_id BIGINT NOT NULL REFERENCES collaborations(id) ON DELETE CASCADE,
    agent_id BIGINT REFERENCES agents(id),
    role VARCHAR(20) NOT NULL,
    content TEXT NOT NULL,
    metadata JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_messages_collaboration ON collaboration_messages(collaboration_id);
CREATE INDEX idx_messages_created ON collaboration_messages(created_at);
```

#### skills表
```sql
CREATE TABLE skills (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    version VARCHAR(20),
    author VARCHAR(100),
    skill_directory VARCHAR(500),
    is_enabled BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_skills_name ON skills(name);
CREATE INDEX idx_skills_enabled ON skills(is_enabled);
```

### 10.2 索引优化

```sql
-- 为常用查询添加索引
CREATE INDEX idx_agents_created ON agents(created_at DESC);
CREATE INDEX idx_collaborations_created ON collaborations(created_at DESC);
CREATE INDEX idx_messages_collaboration_created ON collaboration_messages(collaboration_id, created_at DESC);

-- 为JSONB字段添加GIN索引
CREATE INDEX idx_collaborations_config ON collaborations USING GIN(config);
CREATE INDEX idx_messages_metadata ON collaboration_messages USING GIN(metadata);
```

---

## 11. API设计

### 11.1 Agent API

#### 创建Agent
```
POST /api/agents
Content-Type: application/json

{
  "name": "开发助手",
  "description": "帮助编写代码",
  "type": "Developer",
  "avatar": "👨‍💻",
  "llmConfigId": 1001,
  "llmModelConfigId": 1186,
  "systemPrompt": "你是一位资深开发工程师...",
  "fallbackModels": [
    {
      "llmConfigId": 1001,
      "llmConfigName": "通义千问",
      "llmModelConfigId": 1187,
      "modelName": "qwen-plus",
      "priority": 1
    }
  ]
}
```

#### 获取Agent列表
```
GET /api/agents

Response:
{
  "success": true,
  "data": [
    {
      "id": 1001,
      "name": "开发助手",
      "description": "帮助编写代码",
      "type": "Developer",
      "avatar": "👨‍💻",
      "status": "Active",
      "llmConfigName": "通义千问",
      "primaryModelName": "qwen-max",
      "createdAt": "2026-03-29T10:00:00"
    }
  ]
}
```

### 11.2 协作API

#### 创建协作
```
POST /api/collaborations
Content-Type: application/json

{
  "name": "代码审查流程",
  "description": "多Agent协作进行代码审查",
  "workflowType": "Sequential",
  "members": [
    { "agentId": 1001, "role": "Leader", "orderIndex": 1 },
    { "agentId": 1002, "role": "Member", "orderIndex": 2 },
    { "agentId": 1003, "role": "Member", "orderIndex": 3 }
  ]
}
```

#### 执行协作
```
POST /api/collaborations/{id}/execute
Content-Type: application/json

{
  "input": "请审查这段代码：\n```python\ndef hello():\n    print('hello')\n```"
}

Response:
{
  "success": true,
  "executionId": "exec_123",
  "status": "Running"
}
```

#### 获取对话历史
```
GET /api/collaborations/{id}/messages

Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "agentId": 1001,
      "agentName": "开发助手",
      "role": "assistant",
      "content": "我来审查这段代码...",
      "createdAt": "2026-03-29T10:05:00"
    }
  ]
}
```

### 11.3 Skill API

#### 上传Skill
```
POST /api/skills/upload
Content-Type: multipart/form-data

file: skill.zip

Response:
{
  "success": true,
  "data": {
    "id": 1,
    "name": "data-analysis",
    "description": "数据分析Skill",
    "version": "1.0.0"
  }
}
```

#### 执行Skill
```
POST /api/skills/{name}/execute
Content-Type: application/json

{
  "parameters": {
    "filePath": "/data/sales.csv",
    "outputPath": "/output"
  }
}

Response:
{
  "success": true,
  "output": {
    "rows": 1000,
    "columns": ["date", "product", "sales"]
  }
}
```

### 11.4 环境API

#### 获取已安装运行时
```
GET /api/environment/runtimes

Response:
{
  "success": true,
  "data": [
    {
      "name": "Python",
      "version": "3.11.0",
      "path": "/usr/bin/python",
      "status": "Installed"
    },
    {
      "name": "Node.js",
      "version": "18.17.0",
      "path": "/usr/bin/node",
      "status": "Installed"
    }
  ]
}
```

#### 安装Python包
```
POST /api/environment/python/packages/install
Content-Type: application/json

{
  "packageName": "pandas",
  "version": "2.0.0"
}

Response:
{
  "success": true,
  "message": "安装成功"
}
```

---

## 12. 前端设计

### 12.1 页面结构

```
frontend/
├── src/
│   ├── pages/
│   │   ├── Dashboard.tsx          # 仪表板
│   │   ├── Agents.tsx             # Agent管理
│   │   ├── AgentFormModal.tsx     # Agent表单
│   │   ├── Collaborations.tsx     # 协作管理
│   │   ├── CollaborationChat.tsx  # 协作对话
│   │   ├── Skills.tsx             # Skill管理
│   │   ├── Environment.tsx        # 环境管理
│   │   ├── LlmConfigs.tsx         # LLM配置
│   │   └── Settings.tsx           # 系统设置
│   ├── components/
│   │   ├── ConversationFlow.tsx   # 对话流程
│   │   ├── AgentSelector.tsx      # Agent选择器
│   │   ├── WorkflowEditor.tsx     # 工作流编辑器
│   │   └── SkillCard.tsx          # Skill卡片
│   ├── services/
│   │   ├── agentService.ts        # Agent服务
│   │   ├── collaborationService.ts # 协作服务
│   │   ├── skillService.ts        # Skill服务
│   │   └── environmentService.ts  # 环境服务
│   └── utils/
│       ├── api.ts                 # API工具
│       └── websocket.ts           # WebSocket工具
```

### 12.2 核心组件

#### Agent管理页面
```typescript
// frontend/src/pages/Agents.tsx
import React, { useState, useEffect } from 'react';
import { Card, Table, Button, Space, Tag, Modal } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import AgentFormModal from '../components/AgentFormModal';

const AgentsPage: React.FC = () => {
  const [agents, setAgents] = useState([]);
  const [modalVisible, setModalVisible] = useState(false);
  const [selectedAgent, setSelectedAgent] = useState(null);

  useEffect(() => {
    loadAgents();
  }, []);

  const loadAgents = async () => {
    const response = await api.get('/agents');
    setAgents(response.data);
  };

  const handleCreate = () => {
    setSelectedAgent(null);
    setModalVisible(true);
  };

  const handleEdit = (agent: any) => {
    setSelectedAgent(agent);
    setModalVisible(true);
  };

  const handleDelete = async (id: number) => {
    Modal.confirm({
      title: '确认删除',
      content: '确定要删除这个Agent吗？',
      onOk: async () => {
        await api.delete(`/agents/${id}`);
        loadAgents();
      }
    });
  };

  const columns = [
    { title: 'ID', dataIndex: 'id', key: 'id' },
    { title: '名称', dataIndex: 'name', key: 'name' },
    { title: '类型', dataIndex: 'type', key: 'type' },
    { 
      title: '状态', 
      dataIndex: 'status', 
      key: 'status',
      render: (status: string) => (
        <Tag color={status === 'Active' ? 'green' : 'default'}>
          {status}
        </Tag>
      )
    },
    { title: '主模型', dataIndex: 'primaryModelName', key: 'primaryModelName' },
    { 
      title: '操作', 
      key: 'actions',
      render: (_: any, record: any) => (
        <Space>
          <Button 
            icon={<EditOutlined />} 
            onClick={() => handleEdit(record)}
          >
            编辑
          </Button>
          <Button 
            icon={<DeleteOutlined />} 
            danger
            onClick={() => handleDelete(record.id)}
          >
            删除
          </Button>
        </Space>
      )
    }
  ];

  return (
    <div>
      <Card 
        title="智能体管理" 
        extra={
          <Button 
            type="primary" 
            icon={<PlusOutlined />}
            onClick={handleCreate}
          >
            创建智能体
          </Button>
        }
      >
        <Table 
          columns={columns} 
          dataSource={agents} 
          rowKey="id"
        />
      </Card>

      <AgentFormModal
        visible={modalVisible}
        agent={selectedAgent}
        onClose={() => setModalVisible(false)}
        onSuccess={() => {
          setModalVisible(false);
          loadAgents();
        }}
      />
    </div>
  );
};

export default AgentsPage;
```

#### 协作对话页面
```typescript
// frontend/src/pages/CollaborationChat.tsx
import React, { useState, useEffect } from 'react';
import { Card, Input, Button, Space, message } from 'antd';
import { SendOutlined } from '@ant-design/icons';
import ConversationFlow from '../components/ConversationFlow';

const CollaborationChatPage: React.FC<{ collaborationId: number }> = ({ collaborationId }) => {
  const [input, setInput] = useState('');
  const [sending, setSending] = useState(false);

  const handleSend = async () => {
    if (!input.trim()) {
      message.warning('请输入内容');
      return;
    }

    setSending(true);
    try {
      await api.post(`/collaborations/${collaborationId}/execute`, {
        input: input.trim()
      });
      setInput('');
      message.success('已发送');
    } catch (error) {
      message.error('发送失败');
    } finally {
      setSending(false);
    }
  };

  return (
    <div style={{ display: 'flex', gap: 16, height: 'calc(100vh - 120px)' }}>
      {/* 左侧：对话流程 */}
      <div style={{ flex: 1 }}>
        <ConversationFlow collaborationId={collaborationId} />
      </div>

      {/* 右侧：输入框 */}
      <Card title="发送消息" style={{ width: 400 }}>
        <Space direction="vertical" style={{ width: '100%' }}>
          <Input.TextArea
            value={input}
            onChange={e => setInput(e.target.value)}
            placeholder="输入您的请求..."
            rows={6}
          />
          <Button 
            type="primary" 
            icon={<SendOutlined />}
            loading={sending}
            onClick={handleSend}
            block
          >
            发送
          </Button>
        </Space>
      </Card>
    </div>
  );
};

export default CollaborationChatPage;
```

---

## 13. 部署方案

### 13.1 Docker部署

#### Dockerfile
```dockerfile
# MAFStudio.Api/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# 安装基础工具和运行时
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    git \
    bash \
    python3 \
    python3-pip \
    python3-venv \
    && curl -fsSL https://deb.nodesource.com/setup_18.x | bash - \
    && apt-get install -y nodejs \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# 创建目录
RUN mkdir -p /app/skills \
    && mkdir -p /app/data \
    && mkdir -p /app/uploads \
    && mkdir -p /app/output

# 设置Python别名
RUN ln -s /usr/bin/python3 /usr/bin/python

# 复制应用
COPY . /app
WORKDIR /app

EXPOSE 5000
ENTRYPOINT ["dotnet", "MAFStudio.Api.dll"]
```

#### docker-compose.yml
```yaml
version: '3.8'

services:
  maf-studio:
    build:
      context: ./backend
      dockerfile: Dockerfile
    container_name: maf-studio
    ports:
      - "5000:5000"
    volumes:
      # 应用数据
      - ./skills:/app/skills
      - ./data:/app/data
      - ./uploads:/app/uploads
      - ./output:/app/output
      
      # 系统环境持久化
      - python-packages:/usr/local/lib/python3.11/dist-packages
      - node-modules:/usr/lib/node_modules
      - npm-cache:/root/.npm
      - pip-cache:/root/.cache/pip
      
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=mafstudio;Username=maf;Password=maf123
    privileged: true
    depends_on:
      - postgres
    restart: unless-stopped

  postgres:
    image: postgres:15
    container_name: maf-postgres
    environment:
      POSTGRES_DB: mafstudio
      POSTGRES_USER: maf
      POSTGRES_PASSWORD: maf123
    volumes:
      - postgres-data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    restart: unless-stopped

volumes:
  postgres-data:
  python-packages:
  node-modules:
  npm-cache:
  pip-cache:
```

### 13.2 部署步骤

```bash
# 1. 克隆代码
git clone https://github.com/your-org/maf-studio.git
cd maf-studio

# 2. 配置环境变量
cp .env.example .env
# 编辑.env文件，设置必要的配置

# 3. 构建并启动
docker-compose up -d

# 4. 查看日志
docker-compose logs -f maf-studio

# 5. 初始化数据库
docker-compose exec maf-studio dotnet ef database update
```

### 13.3 生产环境配置

#### Nginx反向代理
```nginx
upstream maf_studio {
    server localhost:5000;
}

server {
    listen 80;
    server_name your-domain.com;

    client_max_body_size 100M;

    location / {
        proxy_pass http://maf_studio;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /hubs/ {
        proxy_pass http://maf_studio/hubs/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
    }
}
```

---

## 14. 安全设计

### 14.1 认证授权

```csharp
// MAFStudio.Api/Program.cs
var builder = WebApplication.CreateBuilder(args);

// 添加JWT认证
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// 添加授权策略
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => 
        policy.RequireRole("Admin"));
    options.AddPolicy("RequireUserRole", policy => 
        policy.RequireRole("User", "Admin"));
});
```

### 14.2 路径安全

```csharp
// MAFStudio.Infrastructure/Security/PathValidator.cs
public class PathValidator
{
    private static readonly string[] AllowedDirectories = 
    {
        "/app/data",
        "/app/output",
        "/app/uploads",
        "/app/skills"
    };

    public static bool IsPathAllowed(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return AllowedDirectories.Any(allowed => 
            fullPath.StartsWith(Path.GetFullPath(allowed), StringComparison.OrdinalIgnoreCase));
    }
}
```

### 14.3 SQL注入防护

```csharp
// 使用参数化查询
public async Task<Agent?> GetByIdAsync(long id)
{
    const string sql = @"
        SELECT id, name, description, type, avatar, status,
               llm_config_id, llm_model_config_id, llm_config_name,
               llm_model_name, fallback_models, system_prompt, created_at, updated_at, created_by
        FROM agents
        WHERE id = @Id";

    using