# MAF Studio - 多智能体协作平台

基于 Microsoft Agent Framework (MAF) 的多智能体协作平台，提供智能体之间的协作、通信和配置管理功能。

## 项目结构

```
maf-studio/
├── backend/                     # ASP.NET Core Web API 后端
│   ├── Abstractions/            # 接口定义层
│   │   ├── IAgentService.cs             # 智能体服务接口
│   │   ├── IAgentRuntimeService.cs      # 智能体运行时服务接口
│   │   ├── ILLMProvider.cs              # LLM供应商接口
│   │   ├── ILLMConfigService.cs         # LLM配置服务接口
│   │   ├── IRagService.cs               # RAG服务接口
│   │   ├── IAuthService.cs              # 认证服务接口
│   │   └── ...                          # 其他接口
│   │
│   ├── Models/                  # 数据模型层
│   │   ├── Requests/             # 请求模型
│   │   │   ├── AgentRequests.cs          # 智能体请求模型
│   │   │   ├── LLMConfigRequests.cs      # LLM配置请求模型
│   │   │   ├── RagRequests.cs            # RAG请求模型
│   │   │   └── CollaborationRequests.cs  # 协作请求模型
│   │   ├── Responses/            # 响应模型
│   │   │   └── AgentRuntimeResponses.cs  # 智能体运行时响应模型
│   │   ├── AgentRuntimeModels.cs         # 智能体运行时模型
│   │   ├── RagModels.cs                  # RAG模型
│   │   └── ProviderInfo.cs               # 供应商信息模型
│   │
│   ├── Providers/               # LLM供应商实现层
│   │   ├── BaseLLMProvider.cs            # LLM供应商抽象基类
│   │   ├── QwenProvider.cs               # 阿里千问供应商
│   │   ├── OpenAIProvider.cs             # OpenAI供应商
│   │   ├── ZhipuProvider.cs              # 智谱AI供应商
│   │   └── LLMProviderFactory.cs         # LLM供应商工厂
│   │
│   ├── Services/                # 服务实现层
│   │   ├── AgentRuntimeService.cs        # 智能体运行时服务
│   │   ├── AgentService.cs               # 智能体管理服务
│   │   ├── LLMConfigService.cs           # LLM配置服务
│   │   ├── RagService.cs                 # RAG服务
│   │   ├── AuthService.cs                # 认证服务
│   │   ├── CollaborationService.cs       # 协作服务
│   │   ├── MessageService.cs             # 消息服务
│   │   ├── SystemConfigService.cs        # 系统配置服务
│   │   ├── OperationLogService.cs        # 操作日志服务
│   │   ├── DatabaseLogger.cs             # 数据库日志记录器
│   │   ├── DocumentParsing/              # 文档解析服务
│   │   │   ├── BaseDocumentParser.cs     # 文档解析器基类
│   │   │   ├── DocumentParserFactory.cs  # 文档解析器工厂
│   │   │   └── Parsers/                  # 具体解析器实现
│   │   └── LLMProviders/                 # LLM供应商工厂(旧版兼容)
│   │
│   ├── Controllers/             # API控制器层
│   │   ├── AgentsController.cs           # 智能体管理API
│   │   ├── AgentRuntimeController.cs     # 智能体运行时API
│   │   ├── AgentTypesController.cs       # 智能体类型API
│   │   ├── LLMConfigsController.cs       # LLM配置API
│   │   ├── RagController.cs              # RAG服务API
│   │   ├── AuthController.cs             # 认证API
│   │   ├── CollaborationsController.cs   # 协作管理API
│   │   ├── MessagesController.cs         # 消息API
│   │   ├── SystemConfigsController.cs    # 系统配置API
│   │   ├── SystemLogsController.cs       # 系统日志API
│   │   └── LogsController.cs             # 操作日志API
│   │
│   ├── Data/                    # 数据访问层
│   │   ├── ApplicationDbContext.cs       # EF Core数据库上下文
│   │   ├── Agent.cs                      # 智能体实体
│   │   ├── LLMConfig.cs                  # LLM配置实体
│   │   ├── LLMModelConfig.cs             # LLM模型配置实体
│   │   ├── Collaboration.cs              # 协作实体
│   │   ├── Message.cs                    # 消息实体
│   │   ├── User.cs                       # 用户实体
│   │   ├── SystemLog.cs                  # 系统日志实体
│   │   └── ...                           # 其他实体
│   │
│   ├── Hubs/                    # SignalR集线器
│   │   └── AgentHub.cs                   # 智能体实时通信集线器
│   │
│   ├── Migrations/              # 数据库迁移
│   └── Program.cs               # 应用程序入口
│
├── frontend/                    # React 前端
│   ├── src/
│   │   ├── components/          # 组件
│   │   ├── pages/               # 页面
│   │   ├── services/            # API服务
│   │   ├── hooks/               # 自定义Hooks
│   │   └── utils/               # 工具函数
│   └── package.json
│
├── docs/                        # 文档
└── docker-compose.yml           # Docker Compose 配置
```

## 架构设计

### 分层架构

项目采用清晰的分层架构设计：

```
┌─────────────────────────────────────────────────────────────┐
│                      Controllers (API层)                     │
│                    处理HTTP请求和响应                         │
├─────────────────────────────────────────────────────────────┤
│                      Services (服务层)                        │
│                    业务逻辑处理                               │
├─────────────────────────────────────────────────────────────┤
│                      Providers (供应商层)                     │
│                    LLM供应商抽象和实现                         │
├─────────────────────────────────────────────────────────────┤
│                      Data (数据访问层)                        │
│                    数据库操作和实体定义                        │
└─────────────────────────────────────────────────────────────┘
```

### 设计模式应用

1. **工厂模式 (Factory Pattern)**
   - `LLMProviderFactory`: 根据供应商标识创建对应的LLM供应商实例
   - `DocumentParserFactory`: 根据文件类型选择合适的文档解析器

2. **策略模式 (Strategy Pattern)**
   - `BaseLLMProvider`: 抽象基类定义统一接口
   - `QwenProvider`, `OpenAIProvider`, `ZhipuProvider`: 具体策略实现

3. **依赖注入 (Dependency Injection)**
   - 所有服务通过接口注入
   - 便于单元测试和模块替换

4. **单例模式 (Singleton)**
   - `AgentRuntimeService`: 单例管理所有智能体运行时实例

5. **模板方法模式 (Template Method)**
   - `BaseDocumentParser`: 定义文档解析的骨架流程

### 智能体生命周期

```
┌──────────────┐     初始化      ┌──────────────┐
│ Uninitialized │ ──────────────→ │     Ready    │
│   (未初始化)   │                 │    (就绪)    │
└──────────────┘                 └──────────────┘
                                       │
                          执行任务      │      任务完成
                                 ↓      │      ↓
                            ┌──────────────┐
                            │     Busy     │
                            │    (忙碌)    │
                            └──────────────┘
                                       │
                          空闲超时      │
                                 ↓      │
                            ┌──────────────┐
                            │   Sleeping   │
                            │    (休眠)    │
                            └──────────────┘
                                       │
                          休眠超时      │
                                 ↓      │
                            ┌──────────────┐
                            │  Destroyed   │
                            │    (销毁)    │
                            └──────────────┘
```

## 功能特性

- **多智能体协作**: 支持多个智能体之间的消息传递和协作
- **智能体生命周期管理**: 初始化、激活、休眠、销毁等状态管理
- **LLM供应商抽象**: 支持多种大模型供应商（OpenAI、阿里千问、智谱AI等）
- **RAG知识库**: 文档上传、分割、向量入库和检索
- **实时通信**: 基于SignalR的智能体实时消息传递
- **配置管理**: 智能体和大模型配置的动态管理
- **系统日志**: 数据库日志记录，便于问题追踪

## 技术栈

### 后端
- ASP.NET Core 10.0
- Microsoft Agent Framework (MAF)
- Microsoft.Extensions.AI
- OpenAI SDK
- Entity Framework Core
- PostgreSQL
- SignalR

### 前端
- React 18
- TypeScript
- Ant Design
- Axios

## 快速开始

### 环境要求

- .NET SDK 10.0+
- Node.js 18+
- PostgreSQL 15+

### 后端开发

```bash
cd backend
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

## Docker 部署

### 环境要求

- Docker 20.10+
- Docker Compose 2.0+

### 部署步骤

#### 1. 克隆项目

```bash
git clone <repository-url>
cd maf-studio
```

#### 2. 配置环境变量

```bash
# 复制环境变量示例文件
cp .env.example .env

# 编辑环境变量（生产环境必须修改）
# 重要：请修改以下敏感配置
# - POSTGRES_PASSWORD: 数据库密码
# - JWT_SECRET: JWT密钥（建议使用强随机字符串）
vim .env
```

#### 3. 启动服务

```bash
# 启动所有服务（后台运行）
docker-compose up -d

# 查看服务状态
docker-compose ps

# 查看服务日志
docker-compose logs -f

# 仅查看特定服务日志
docker-compose logs -f backend
docker-compose logs -f frontend
```

#### 4. 验证部署

```bash
# 检查服务健康状态
docker-compose ps

# 测试后端API
curl http://localhost:5000/health

# 测试前端
curl http://localhost:80
```

#### 5. 访问应用

- 前端: http://localhost:80
- 后端 API: http://localhost:5000
- Swagger 文档: http://localhost:5000/swagger

### 常用命令

```bash
# 停止所有服务
docker-compose down

# 停止并删除数据卷（清除所有数据）
docker-compose down -v

# 重启特定服务
docker-compose restart backend
docker-compose restart frontend

# 重新构建镜像
docker-compose build --no-cache

# 重新构建并启动
docker-compose up -d --build

# 查看资源使用情况
docker-compose top

# 进入容器调试
docker-compose exec backend bash
docker-compose exec frontend sh
```

### 开发模式部署

开发模式下，代码修改会自动同步到容器：

```bash
# 启动开发环境（自动加载 docker-compose.override.yml）
docker-compose up -d

# 后端代码修改后自动重新编译（dotnet watch）
# 前端代码修改后自动热更新（npm start）
```

### 生产环境部署

#### 1. 安全配置

```bash
# 修改 .env 文件中的敏感配置
POSTGRES_PASSWORD=<强密码>
JWT_SECRET=<强随机密钥，至少32字符>

# 确保文件权限正确
chmod 600 .env
```

#### 2. 使用外部数据库

如果使用外部 PostgreSQL 数据库，修改 `docker-compose.yml`：

```yaml
services:
  backend:
    environment:
      - ConnectionStrings__DefaultConnection=Host=<外部数据库地址>;Port=5432;Database=mafstudio;Username=<用户名>;Password=<密码>
    # 移除 postgres 依赖
    depends_on: []
    
# 删除 postgres 服务和数据卷定义
```

#### 3. 配置 HTTPS

生产环境建议配置 HTTPS：

```yaml
services:
  frontend:
    ports:
      - "443:443"
    volumes:
      - ./ssl/cert.pem:/etc/nginx/ssl/cert.pem:ro
      - ./ssl/key.pem:/etc/nginx/ssl/key.pem:ro
```

#### 4. 资源限制

为服务配置资源限制：

```yaml
services:
  backend:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '0.5'
          memory: 512M
```

### 服务架构

```
┌─────────────────────────────────────────────────────────────┐
│                         用户请求                             │
└─────────────────────────────────────────────────────────────┘
                              │
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    Frontend (Nginx)                          │
│                    端口: 80                                   │
│              静态资源 + 反向代理                              │
└─────────────────────────────────────────────────────────────┘
                              │
                    /api/* → │
                    /hubs/* →│
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    Backend (.NET)                            │
│                    端口: 5000                                 │
│                 Web API + SignalR                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    PostgreSQL                                │
│                    端口: 5432                                 │
│                      数据存储                                 │
└─────────────────────────────────────────────────────────────┘
```

### 故障排查

#### 后端无法连接数据库

```bash
# 检查数据库是否就绪
docker-compose exec postgres pg_isready

# 检查网络连接
docker-compose exec backend ping postgres

# 查看后端日志
docker-compose logs backend
```

#### 前端无法访问后端API

```bash
# 检查后端服务状态
docker-compose ps backend

# 检查nginx配置
docker-compose exec frontend nginx -t

# 查看nginx日志
docker-compose logs frontend
```

#### 容器内存不足

```bash
# 查看容器资源使用
docker stats

# 增加Docker内存限制（Docker Desktop设置）
# 或在docker-compose.yml中配置资源限制
```

### 数据备份与恢复

#### 备份数据库

```bash
# 创建备份
docker-compose exec postgres pg_dump -U mafuser mafstudio > backup_$(date +%Y%m%d).sql

# 或使用docker命令
docker exec maf-studio-postgres pg_dump -U mafuser mafstudio > backup.sql
```

#### 恢复数据库

```bash
# 恢复数据
cat backup.sql | docker-compose exec -T postgres psql -U mafuser mafstudio
```

## API 概览

### 智能体管理
- `GET /api/agents` - 获取智能体列表
- `POST /api/agents` - 创建智能体
- `PUT /api/agents/{id}` - 更新智能体
- `DELETE /api/agents/{id}` - 删除智能体

### 智能体运行时
- `POST /api/agentruntime/{id}/activate` - 激活智能体
- `POST /api/agentruntime/{id}/test` - 测试智能体连接
- `POST /api/agentruntime/{id}/sleep` - 休眠智能体
- `POST /api/agentruntime/{id}/destroy` - 销毁智能体
- `GET /api/agentruntime/status` - 获取运行时状态

### LLM配置
- `GET /api/llmconfigs` - 获取LLM配置列表
- `POST /api/llmconfigs` - 创建LLM配置
- `POST /api/llmconfigs/{id}/test` - 测试LLM连接
- `GET /api/llmconfigs/providers` - 获取供应商列表

### RAG服务
- `POST /api/rag/upload` - 上传文档
- `POST /api/rag/query` - RAG检索查询
- `GET /api/rag/documents` - 获取文档列表

## 许可证

MIT License
