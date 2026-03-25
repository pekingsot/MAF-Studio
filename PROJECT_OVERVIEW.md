# MAF Studio - 项目概览

## 项目简介

MAF Studio 是一个基于 Microsoft Agent Framework 的多智能体协作平台，提供智能体之间的协作、通信和配置管理功能。

## 项目结构

```
maf-studio/
├── backend/                      # ASP.NET Core 后端
│   ├── Data/                    # 数据模型和数据库上下文
│   │   ├── Agent.cs            # 智能体模型
│   │   ├── AgentMessage.cs     # 消息模型
│   │   ├── Collaboration.cs    # 协作模型
│   │   └── ApplicationDbContext.cs
│   ├── Services/               # 业务逻辑服务
│   │   ├── AgentService.cs     # 智能体服务
│   │   ├── MessageService.cs   # 消息服务
│   │   └── CollaborationService.cs
│   ├── Hubs/                   # SignalR 实时通信
│   │   └── AgentHub.cs
│   ├── Controllers/            # API 控制器
│   │   ├── AgentsController.cs
│   │   ├── MessagesController.cs
│   │   └── CollaborationsController.cs
│   ├── Program.cs             # 应用程序入口
│   ├── appsettings.json       # 配置文件
│   ├── MAFStudio.Backend.csproj
│   └── Dockerfile
├── frontend/                   # React 前端
│   ├── src/
│   │   ├── components/       # React 组件
│   │   │   └── Layout/
│   │   │       └── MainLayout.tsx
│   │   ├── pages/            # 页面组件
│   │   │   ├── Dashboard.tsx
│   │   │   ├── Agents.tsx
│   │   │   ├── AgentDetail.tsx
│   │   │   ├── Collaborations.tsx
│   │   │   ├── CollaborationDetail.tsx
│   │   │   └── Messages.tsx
│   │   ├── services/         # API 服务
│   │   │   ├── api.ts
│   │   │   ├── agentService.ts
│   │   │   ├── collaborationService.ts
│   │   │   └── socketService.ts
│   │   ├── App.tsx
│   │   └── index.tsx
│   ├── public/
│   ├── package.json
│   ├── tsconfig.json
│   ├── Dockerfile
│   └── nginx.conf
├── docker-compose.yml        # Docker Compose 配置
├── start.bat                 # Windows 启动脚本
├── stop.bat                  # Windows 停止脚本
├── README.md                 # 项目说明
├── DEPLOYMENT.md            # 部署指南
└── .gitignore
```

## 核心功能

### 1. 智能体管理
- 创建、编辑、删除智能体
- 智能体状态管理（活跃、忙碌、错误等）
- 智能体配置管理
- 智能体类型分类（助手、工作者、监督者、协调者）

### 2. 消息传递
- 智能体之间的点对点消息传递
- 实时消息推送（SignalR）
- 消息历史记录
- 消息类型支持（文本、命令、查询、响应、错误）

### 3. 协作管理
- 创建协作项目
- 添加/移除协作智能体
- 任务创建和状态管理
- 协作项目状态跟踪

### 4. 实时通信
- SignalR 实时通信
- 智能体状态实时更新
- 消息实时推送
- 连接管理

## 技术栈

### 后端
- **框架**: ASP.NET Core 8.0
- **ORM**: Entity Framework Core
- **数据库**: PostgreSQL 15
- **实时通信**: SignalR
- **AI 框架**: Microsoft Semantic Kernel
- **API 文档**: Swagger/OpenAPI

### 前端
- **框架**: React 18
- **语言**: TypeScript
- **UI 库**: Ant Design 5
- **路由**: React Router 6
- **HTTP 客户端**: Axios
- **实时通信**: Socket.IO Client

### 部署
- **容器化**: Docker
- **编排**: Docker Compose
- **Web 服务器**: Nginx
- **反向代理**: Nginx

## 数据模型

### Agent（智能体）
```csharp
- Id: Guid
- Name: string
- Description: string?
- Type: string
- Configuration: string (JSON)
- Status: AgentStatus
- CreatedAt: DateTime
- UpdatedAt: DateTime?
- LastActiveAt: DateTime?
```

### AgentMessage（消息）
```csharp
- Id: Guid
- FromAgentId: Guid
- ToAgentId: Guid
- Content: string
- Type: MessageType
- Status: MessageStatus
- CreatedAt: DateTime
- ProcessedAt: DateTime?
```

### Collaboration（协作）
```csharp
- Id: Guid
- Name: string
- Description: string?
- Status: CollaborationStatus
- CreatedAt: DateTime
- UpdatedAt: DateTime?
- Agents: List<CollaborationAgent>
- Tasks: List<CollaborationTask>
```

## API 端点

### 智能体 API
- `GET /api/agents` - 获取所有智能体
- `GET /api/agents/{id}` - 获取智能体详情
- `POST /api/agents` - 创建智能体
- `PUT /api/agents/{id}` - 更新智能体
- `DELETE /api/agents/{id}` - 删除智能体
- `PATCH /api/agents/{id}/status` - 更新智能体状态

### 消息 API
- `GET /api/messages/agent/{agentId}` - 获取智能体消息
- `GET /api/messages/conversation/{agent1Id}/{agent2Id}` - 获取对话记录
- `POST /api/messages` - 发送消息
- `PATCH /api/messages/{id}/status` - 更新消息状态

### 协作 API
- `GET /api/collaborations` - 获取所有协作
- `GET /api/collaborations/{id}` - 获取协作详情
- `POST /api/collaborations` - 创建协作
- `POST /api/collaborations/{id}/agents` - 添加智能体到协作
- `DELETE /api/collaborations/{id}/agents/{agentId}` - 移除智能体
- `POST /api/collaborations/{id}/tasks` - 创建任务
- `PATCH /api/collaborations/tasks/{taskId}/status` - 更新任务状态

### SignalR Hub
- `/hubs/agent` - 智能体实时通信 Hub

## 快速开始

### 使用 Docker Compose（推荐）

1. 启动服务：
```bash
start.bat
```

2. 访问应用：
- 前端: http://localhost:3000
- 后端 API: http://localhost:5000
- Swagger: http://localhost:5000/swagger

### 本地开发

1. 启动 PostgreSQL：
```bash
docker-compose up -d postgres
```

2. 启动后端：
```bash
cd backend
dotnet run
```

3. 启动前端：
```bash
cd frontend
npm start
```

## 配置说明

### 后端配置（appsettings.json）
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Database=mafstudio;Username=postgres;Password=postgres"
  },
  "AgentSettings": {
    "MaxAgents": 100,
    "DefaultTimeout": 30000,
    "EnableAutoDiscovery": true
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000"]
  }
}
```

### 前端配置
```bash
REACT_APP_API_URL=http://localhost:5000/api
REACT_APP_SOCKET_URL=http://localhost:5000
```

## 扩展功能

### 添加新的智能体类型
1. 在前端 `Agents.tsx` 中添加新的类型选项
2. 在后端实现对应的智能体逻辑
3. 更新智能体配置 schema

### 集成 AI 模型
1. 配置 Semantic Kernel API Key
2. 在智能体服务中集成 AI 能力
3. 实现智能体与 AI 模型的交互

### 添加新的协作模式
1. 扩展 `Collaboration` 模型
2. 实现新的协作逻辑
3. 在前端添加对应的 UI

## 性能优化

### 数据库优化
- 添加适当的索引
- 使用连接池
- 实现缓存策略

### 前端优化
- 代码分割和懒加载
- 虚拟滚动（大数据列表）
- 优化渲染性能

### 后端优化
- 异步处理
- 批量操作
- 信号压缩

## 安全考虑

1. **认证和授权**: 实现用户认证和权限控制
2. **数据加密**: 敏感数据加密存储
3. **输入验证**: 严格的输入验证和清理
4. **CORS 配置**: 限制跨域访问
5. **速率限制**: 防止 API 滥用
6. **日志审计**: 记录关键操作日志

## 监控和日志

### 应用监控
- 健康检查端点
- 性能指标收集
- 错误追踪

### 日志管理
- 结构化日志
- 日志级别配置
- 日志轮转和归档

## 故障排除

### 常见问题
1. **数据库连接失败**: 检查 PostgreSQL 容器状态
2. **前端无法连接后端**: 检查 CORS 配置
3. **实时通信失败**: 检查 SignalR 配置
4. **端口冲突**: 修改 docker-compose.yml 端口映射

## 贡献指南

1. Fork 项目
2. 创建功能分支
3. 提交更改
4. 推送到分支
5. 创建 Pull Request

## 许可证

MIT License

## 联系方式

如有问题或建议，请通过以下方式联系：
- 提交 Issue
- 发送邮件
- 参与讨论