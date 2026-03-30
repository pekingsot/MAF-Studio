# MAF Studio - 多智能体协作平台

基于 Microsoft Agent Framework (MAF) 的多智能体协作平台，提供智能体之间的协作、通信和配置管理功能。

## 🎯 项目简介

MAF Studio 是一个功能强大的多智能体协作平台，支持：

- ✅ **智能体管理** - 创建、配置和管理多个AI智能体
- ✅ **协作工作流** - 支持顺序、并发、任务移交、群聊协作、审阅迭代等多种工作流模式
- ✅ **工作流模板系统** - 保存、管理和复用工作流模板
- ✅ **Magentic 工作流** - 智能Agent自动编排工作流，支持人工审核和修改
- ✅ **可视化设计器** - 拖拽式工作流设计器，类似n8n、Dify、Coze
- ✅ **实时通信** - 基于SignalR的实时消息传递
- ✅ **RAG知识库** - 文档上传、分割、向量入库和检索
- ✅ **多LLM支持** - 支持OpenAI、阿里千问、智谱AI等多种大模型

## 📁 项目结构

```
maf-studio/
├── backend/                           # ASP.NET Core Web API 后端
│   ├── src/
│   │   ├── MAFStudio.Core/           # 核心层
│   │   │   ├── Entities/             # 实体定义
│   │   │   │   ├── Agent.cs          # 智能体实体
│   │   │   │   ├── AgentType.cs      # 智能体类型
│   │   │   │   ├── LlmConfig.cs      # LLM配置
│   │   │   │   ├── Collaboration.cs  # 协作项目
│   │   │   │   ├── CollaborationAgent.cs  # 协作智能体
│   │   │   │   ├── CollaborationTask.cs   # 协作任务
│   │   │   │   └── WorkflowTemplate.cs    # 工作流模板
│   │   │   ├── Enums/                # 枚举定义
│   │   │   └── Interfaces/           # 接口定义
│   │   │       ├── Repositories/     # 仓储接口
│   │   │       └── Services/         # 服务接口
│   │   │
│   │   ├── MAFStudio.Infrastructure/ # 基础设施层
│   │   │   ├── Data/                 # 数据访问
│   │   │   │   ├── Repositories/     # 仓储实现
│   │   │   │   ├── Scripts/          # SQL迁移脚本
│   │   │   │   │   ├── V1__Initial.sql
│   │   │   │   │   ├── V29__FixCollaborationAgentsColumnTypes.sql
│   │   │   │   │   ├── V30__FixCollaborationTasksColumnType.sql
│   │   │   │   │   ├── V31__FixCollaborationTasksAssignedToType.sql
│   │   │   │   │   └── ...
│   │   │   │   └── DapperContext.cs  # Dapper上下文
│   │   │   └── Services/             # 基础服务实现
│   │   │
│   │   ├── MAFStudio.Application/    # 应用层
│   │   │   ├── Services/             # 业务服务实现
│   │   │   │   ├── CollaborationService.cs
│   │   │   │   ├── CollaborationWorkflowService.cs
│   │   │   │   ├── CollaborationWorkflowService.ReviewIterative.cs
│   │   │   │   ├── CollaborationWorkflowService.MagenticPlan.cs  # Magentic工作流
│   │   │   │   ├── WorkflowTemplateService.cs  # 工作流模板服务
│   │   │   │   └── ...
│   │   │   ├── Clients/              # LLM客户端
│   │   │   │   └── CustomOpenAICompatibleChatClient.cs
│   │   │   ├── Capabilities/         # 智能体能力
│   │   │   │   └── GitCapability.cs
│   │   │   ├── DTOs/                 # 数据传输对象
│   │   │   ├── VOs/                  # 视图对象
│   │   │   └── Mappers/              # 对象映射
│   │   │
│   │   └── MAFStudio.Api/            # API层
│   │       ├── Controllers/          # API控制器
│   │       │   ├── AgentsController.cs
│   │       │   ├── CollaborationsController.cs
│   │       │   ├── CollaborationWorkflowController.cs
│   │       │   ├── LlmConfigsController.cs
│   │       │   └── ...
│   │       ├── Filters/              # 过滤器
│   │       ├── Middleware/           # 中间件
│   │       ├── Services/             # API服务
│   │       └── Hubs/                 # SignalR集线器
│   │
│   └── MAFStudio.sln                 # 解决方案文件
│
├── frontend/                         # React 前端
│   ├── src/
│   │   ├── components/               # 组件
│   │   │   ├── WorkflowConfigModal.tsx  # 工作流配置组件
│   │   │   └── ...
│   │   ├── pages/                    # 页面
│   │   │   ├── Agents.tsx            # 智能体管理
│   │   │   ├── Collaborations.tsx    # 协作管理
│   │   │   ├── CollaborationChat.tsx # 协作聊天
│   │   │   ├── WorkflowEditor.tsx    # 工作流编辑器
│   │   │   ├── WorkflowTemplateManagement.tsx  # 工作流模板管理
│   │   │   ├── WorkflowExecute.tsx   # 工作流执行
│   │   │   ├── MagenticWorkflow.tsx  # Magentic工作流
│   │   │   ├── collaboration-detail/ # 协作详情
│   │   │   └── ...
│   │   ├── services/                 # API服务
│   │   │   ├── agentService.ts
│   │   │   ├── collaborationService.ts
│   │   │   └── ...
│   │   ├── hooks/                    # 自定义Hooks
│   │   └── utils/                    # 工具函数
│   └── package.json
│
├── tests/                            # 单元测试
│   └── MAFStudio.Tests/
│
├── docs/                             # 文档
│   ├── 协作工作流设计.md
│   ├── 协作工作流问题解答.md
│   ├── 审阅迭代工作流详解.md
│   ├── 可视化工作流设计器方案.md
│   ├── Magentic工作流方案.md         # Magentic工作流方案
│   └── 工作流模板系统设计.md          # 工作流模板系统设计
│
└── docker-compose.yml                # Docker Compose 配置
```

## 🏗️ 架构设计

### 分层架构

项目采用清晰的分层架构设计（DDD思想）：

```
┌─────────────────────────────────────────────────────────────┐
│                      API Layer (MAFStudio.Api)              │
│                    处理HTTP请求和响应                         │
│                    Controllers, Filters, Middleware          │
├─────────────────────────────────────────────────────────────┤
│                  Application Layer (MAFStudio.Application)   │
│                    业务逻辑处理                               │
│                    Services, DTOs, VOs, Mappers              │
├─────────────────────────────────────────────────────────────┤
│                     Core Layer (MAFStudio.Core)              │
│                    实体和接口定义                             │
│                    Entities, Enums, Interfaces               │
├─────────────────────────────────────────────────────────────┤
│              Infrastructure Layer (MAFStudio.Infrastructure) │
│                    数据访问和外部服务                         │
│                    Repositories, Dapper, SQL Scripts         │
└─────────────────────────────────────────────────────────────┘
```

### 技术选型

| 层级 | 技术选型 | 说明 |
|------|----------|------|
| API层 | ASP.NET Core 10.0 | Web API + SignalR |
| 应用层 | C# 10 | 业务逻辑处理 |
| 核心层 | C# 10 | 实体和接口定义 |
| 基础设施层 | Dapper + Npgsql | 轻量级ORM |
| 数据库 | PostgreSQL | 遵循PG最佳实践 |

### 设计模式应用

1. **仓储模式 (Repository Pattern)**
   - 抽象数据访问逻辑
   - 便于单元测试和切换数据源

2. **工厂模式 (Factory Pattern)**
   - `AgentFactoryService`: 创建智能体实例
   - `LLMProviderFactory`: 创建LLM供应商实例

3. **策略模式 (Strategy Pattern)**
   - `BaseLLMProvider`: 抽象基类定义统一接口
   - `QwenProvider`, `OpenAIProvider`, `ZhipuProvider`: 具体策略实现

4. **依赖注入 (Dependency Injection)**
   - 所有服务通过接口注入
   - 便于单元测试和模块替换

5. **DTO/VO模式**
   - DTO: 数据传输对象，用于API请求
   - VO: 视图对象，用于API响应
   - 分离内部实体和外部接口

## ✨ 功能特性

### 核心功能

- **智能体管理**
  - 创建、配置和管理多个AI智能体
  - 支持多种智能体类型（Assistant、UIDesigner、Coder等）
  - 智能体状态监控和管理

- **协作工作流** 🆕
  - **顺序执行**: Agent按顺序依次处理任务
  - **并发执行**: 多个Agent同时处理同一任务
  - **任务移交**: Agent之间灵活移交任务
  - **群聊协作**: 多Agent群聊讨论
  - **审阅迭代**: A写文档 → B审阅 → 打回修改 → 循环直到满意

- **工作流模板系统** 🆕
  - **模板管理**: 创建、编辑、删除工作流模板
  - **模板分类**: 按分类、标签组织模板
  - **模板搜索**: 根据关键词、标签搜索模板
  - **模板复用**: 执行保存的工作流模板
  - **使用统计**: 记录模板使用次数

- **Magentic 工作流** 🆕
  - **智能编排**: Manager Agent 自动分析任务并制定执行计划
  - **人工审核**: 生成的计划可人工审核和修改
  - **计划保存**: 将优化后的计划保存为模板
  - **自动学习**: 系统记住优秀的流程，下次类似任务直接使用

- **可视化工作流设计器** 🆕
  - **拖拽设计**: 类似n8n、Dify、Coze的拖拽式设计
  - **自定义节点**: Start、Agent、Aggregator、Condition、Loop等节点
  - **边类型**: Sequential、FanOut、FanIn、Conditional、Loop等边
  - **实时预览**: 实时预览工作流结构
  - **参数配置**: 为节点配置参数和输入模板

### 其他功能

- **A2A协议**: 智能体之间的通信协议支持
- **@提及功能**: 在协作聊天中@特定智能体
- **LLM供应商抽象**: 支持多种大模型供应商
- **RAG知识库**: 文档上传、分割、向量入库和检索
- **实时通信**: 基于SignalR的智能体实时消息传递
- **流式输出**: 大模型响应流式返回
- **配置管理**: 智能体和大模型配置的动态管理
- **系统日志**: 数据库日志记录，用户隔离
- **操作日志**: 记录用户操作行为

## 🚀 快速开始

### 环境要求

- .NET SDK 10.0+
- Node.js 18+
- PostgreSQL 15+

### 后端开发

```bash
cd backend/src/MAFStudio.Api
dotnet restore
dotnet build
dotnet run
```

### 前端开发

```bash
cd frontend
npm install
npm start
```

### 访问应用

- 前端: http://localhost:3000
- 后端 API: http://localhost:5000
- Swagger 文档: http://localhost:5000/swagger

### 默认账号

| 用户名 | 密码 | 角色 |
|--------|------|------|
| admin | admin123 | 管理员 |
| pekingsot | pekingsot123 | 普通用户 |

## 📚 API 概览

### 认证
- `POST /api/auth/login` - 用户登录
- `POST /api/auth/register` - 用户注册
- `GET /api/auth/me` - 获取当前用户信息

### 智能体管理
- `GET /api/agents` - 获取智能体列表
- `POST /api/agents` - 创建智能体
- `PUT /api/agents/{id}` - 更新智能体
- `DELETE /api/agents/{id}` - 删除智能体

### 智能体类型
- `GET /api/agenttypes` - 获取智能体类型列表
- `GET /api/agenttypes/{id}` - 获取智能体类型详情

### LLM配置
- `GET /api/llmconfigs` - 获取LLM配置列表
- `POST /api/llmconfigs` - 创建LLM配置
- `PUT /api/llmconfigs/{id}` - 更新LLM配置
- `DELETE /api/llmconfigs/{id}` - 删除LLM配置
- `POST /api/llmconfigs/{id}/test` - 测试LLM连接
- `GET /api/llmconfigs/providers` - 获取供应商列表

### 协作管理 🆕
- `GET /api/collaborations` - 获取协作项目列表（包含智能体和任务）
- `GET /api/collaborations/{id}` - 获取协作项目详情
- `POST /api/collaborations` - 创建协作项目
- `DELETE /api/collaborations/{id}` - 删除协作项目
- `POST /api/collaborations/{id}/agents` - 添加智能体到协作
- `DELETE /api/collaborations/{id}/agents/{agentId}` - 移除智能体
- `POST /api/collaborations/{id}/tasks` - 创建任务
- `PATCH /api/collaborations/tasks/{taskId}/status` - 更新任务状态

### 协作工作流 🆕
- `POST /api/collaborations/{id}/workflow/execute` - 执行工作流
- `POST /api/collaborations/{id}/workflow/sequential` - 顺序执行
- `POST /api/collaborations/{id}/workflow/concurrent` - 并发执行
- `POST /api/collaborations/{id}/workflow/handoffs` - 任务移交
- `POST /api/collaborations/{id}/workflow/groupchat` - 群聊协作（流式）
- `POST /api/collaborations/{id}/workflow/review-iterative` - 审阅迭代

### 工作流模板 🆕
- `GET /api/workflow-templates` - 获取模板列表
- `GET /api/workflow-templates/{id}` - 获取模板详情
- `POST /api/workflow-templates` - 创建模板
- `PUT /api/workflow-templates/{id}` - 更新模板
- `DELETE /api/workflow-templates/{id}` - 删除模板
- `POST /api/workflow-templates/{id}/execute` - 执行模板
- `POST /api/workflow-templates/generate-magentic` - 生成Magentic计划
- `POST /api/workflow-templates/save-magentic` - 保存Magentic计划
- `GET /api/workflow-templates/search` - 搜索模板
- `GET /api/workflow-templates/categories` - 获取分类列表

### RAG服务
- `POST /api/rag/upload` - 上传文档
- `POST /api/rag/query` - RAG检索查询
- `GET /api/rag/documents` - 获取文档列表

### 系统管理
- `GET /api/systemlogs` - 获取系统日志
- `GET /api/operationlogs` - 获取操作日志
- `GET /api/systemconfigs` - 获取系统配置

## 🎨 协作工作流详解

### 1. 顺序执行

**适用场景**：流水线式任务，如需求分析 → 设计 → 开发 → 测试

```
输入 → Agent1 → Agent2 → Agent3 → 输出
```

**API调用**：
```json
POST /api/collaborations/{id}/workflow/sequential
{
  "input": "设计一个登录页面"
}
```

---

### 2. 并发执行

**适用场景**：需要多个方案或视角，如多个设计师同时设计

```
       ┌→ Agent1 → 结果1 ┐
输入 → ├→ Agent2 → 结果2 ├→ 汇总 → 输出
       └→ Agent3 → 结果3 ┘
```

**API调用**：
```json
POST /api/collaborations/{id}/workflow/concurrent
{
  "input": "设计一个登录页面"
}
```

---

### 3. 任务移交

**适用场景**：Agent之间需要协作和交接

```
Agent1处理 → [HANDOFF:Agent2] → Agent2处理 → [HANDOFF:Agent3] → 完成
```

**API调用**：
```json
POST /api/collaborations/{id}/workflow/handoffs
{
  "input": "开发登录功能"
}
```

---

### 4. 群聊协作

**适用场景**：复杂问题的多轮讨论，如头脑风暴

```
轮次1: Agent1 → Agent2 → Agent3
轮次2: Agent1 → Agent2 → Agent3
...
直到有人输出 [END]
```

**API调用**：
```json
POST /api/collaborations/{id}/workflow/groupchat
{
  "input": "讨论技术方案"
}
```

---

### 5. 审阅迭代 🆕

**适用场景**：A写文档 → B审阅 → 不满意 → 打回去 → A修改 → 循环直到满意

```
迭代1: A编写 → B审阅 → 不满意
迭代2: A修改 → B审阅 → 满意 [APPROVED] → 结束
```

**API调用**：
```json
POST /api/collaborations/{id}/workflow/review-iterative
{
  "input": "编写一份系统架构设计文档",
  "maxIterations": 10,
  "reviewCriteria": "请从架构合理性、可扩展性、安全性等方面审阅"
}
```

## 📖 文档

- [协作工作流设计](docs/协作工作流设计.md)
- [协作工作流问题解答](docs/协作工作流问题解答.md)
- [审阅迭代工作流详解](docs/审阅迭代工作流详解.md)
- [可视化工作流设计器方案](docs/可视化工作流设计器方案.md)
- [Magentic工作流方案](docs/Magentic工作流方案.md)
- [工作流模板系统设计](docs/工作流模板系统设计.md)

## 🐳 Docker 部署

### 环境要求

- Docker 20.10+
- Docker Compose 2.0+

### 部署步骤

#### 1. 克隆项目

```bash
git clone https://github.com/pekingsot/MAF-Studio.git
cd MAF-Studio
```

#### 2. 配置环境变量

```bash
# 复制环境变量示例文件
cp .env.example .env

# 编辑环境变量（生产环境必须修改）
vim .env
```

#### 3. 启动服务

```bash
# 启动所有服务（后台运行）
docker-compose up -d

# 查看服务状态
docker-compose ps
```

#### 4. 访问应用

- 前端: http://localhost:80
- 后端 API: http://localhost:5000
- Swagger 文档: http://localhost:5000/swagger

### 常用命令

```bash
# 停止所有服务
docker-compose down

# 重启特定服务
docker-compose restart backend

# 重新构建镜像
docker-compose build --no-cache
```

## 🛠️ 技术栈

### 后端
- .NET 10.0
- ASP.NET Core Web API
- Dapper (轻量级ORM)
- Npgsql (PostgreSQL驱动)
- SignalR (实时通信)
- BCrypt.Net (密码哈希)
- JWT (身份认证)
- Microsoft.Extensions.AI (MAF框架)

### 前端
- React 18
- TypeScript
- Ant Design
- Axios
- React Router

### 数据库
- PostgreSQL 15+

## 📝 开发规范

### PostgreSQL 最佳实践

项目严格遵循 PostgreSQL 数据库最佳实践：

1. **命名规范**: 小写 + 下划线（如 `user_info`, `created_at`）
2. **主键**: 使用 `BIGSERIAL`（自增ID）
3. **字符串**: 优先使用 `TEXT` 类型
4. **金额**: 使用 `NUMERIC` 或 `DECIMAL`（禁止 FLOAT/DOUBLE）
5. **JSON**: 使用 `JSONB`（支持索引，查询更快）
6. **约束**: `NOT NULL`, `UNIQUE`, `CHECK`, `REFERENCES`

### 代码规范

- 所有代码注释使用中文
- 多用设计模式，避免临时实现
- 禁止数据库JOIN连接
- 根据查询创建相关索引
- 使用抽象类和接口

## 📄 许可证

MIT License

## 👨‍💻 作者

pekingsot <北京醉鬼>

## 🙏 致谢

感谢 Microsoft Agent Framework (MAF) 团队提供的优秀框架！
