# MAF Studio - 多智能体协作平台

基于 Microsoft Agent Framework (MAF) 的多智能体协作平台，提供智能体之间的协作、通信和配置管理功能。

## 项目结构

```
maf-studio/
├── backend/                           # ASP.NET Core Web API 后端
│   ├── src/
│   │   ├── MAFStudio.Core/           # 核心层
│   │   │   ├── Entities/             # 实体定义
│   │   │   ├── Enums/                # 枚举定义
│   │   │   └── Interfaces/           # 接口定义
│   │   │       ├── Repositories/     # 仓储接口
│   │   │       └── Services/         # 服务接口
│   │   │
│   │   ├── MAFStudio.Infrastructure/ # 基础设施层
│   │   │   ├── Data/                 # 数据访问
│   │   │   │   ├── Repositories/     # 仓储实现
│   │   │   │   ├── Scripts/          # SQL脚本
│   │   │   │   │   ├── V1__Initial.sql    # 数据库初始化
│   │   │   │   │   └── V2__SeedData.sql   # 基础数据
│   │   │   │   └── DapperContext.cs  # Dapper上下文
│   │   │   └── Services/             # 基础服务实现
│   │   │
│   │   ├── MAFStudio.Application/    # 应用层
│   │   │   ├── Services/             # 业务服务实现
│   │   │   ├── DTOs/                 # 数据传输对象
│   │   │   ├── VOs/                  # 视图对象
│   │   │   └── Mappers/              # 对象映射
│   │   │
│   │   └── MAFStudio.Api/            # API层
│   │       ├── Controllers/          # API控制器
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
│   │   ├── pages/                    # 页面
│   │   ├── services/                 # API服务
│   │   ├── hooks/                    # 自定义Hooks
│   │   └── utils/                    # 工具函数
│   └── package.json
│
├── tests/                            # 单元测试
│   └── MAFStudio.Tests/
│
├── docs/                             # 文档
└── docker-compose.yml                # Docker Compose 配置
```

## 架构设计

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

### PostgreSQL 最佳实践

项目严格遵循 PostgreSQL 数据库最佳实践：

1. **命名规范**: 小写 + 下划线（如 `user_info`, `created_at`）
2. **主键**: 使用 `BIGSERIAL` 或 `IDENTITY`（UUID）
3. **字符串**: 优先使用 `TEXT` 类型
4. **金额**: 使用 `NUMERIC` 或 `DECIMAL`（禁止 FLOAT/DOUBLE）
5. **JSON**: 使用 `JSONB`（支持索引，查询更快）
6. **约束**: `NOT NULL`, `UNIQUE`, `CHECK`, `REFERENCES`

### 设计模式应用

1. **仓储模式 (Repository Pattern)**
   - 抽象数据访问逻辑
   - 便于单元测试和切换数据源

2. **工厂模式 (Factory Pattern)**
   - `LLMProviderFactory`: 根据供应商标识创建对应的LLM供应商实例

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

## 功能特性

- **多智能体协作**: 支持多个智能体之间的消息传递和协作
- **A2A协议**: 智能体之间的通信协议支持
- **@提及功能**: 在协作聊天中@特定智能体
- **LLM供应商抽象**: 支持多种大模型供应商（OpenAI、阿里千问、智谱AI等）
- **RAG知识库**: 文档上传、分割、向量入库和检索
- **实时通信**: 基于SignalR的智能体实时消息传递
- **流式输出**: 大模型响应流式返回
- **配置管理**: 智能体和大模型配置的动态管理
- **系统日志**: 数据库日志记录，用户隔离
- **操作日志**: 记录用户操作行为

## 技术栈

### 后端
- .NET 10.0
- ASP.NET Core Web API
- Dapper (轻量级ORM)
- Npgsql (PostgreSQL驱动)
- SignalR (实时通信)
- BCrypt.Net (密码哈希)
- JWT (身份认证)

### 前端
- React 18
- TypeScript
- Ant Design
- Axios

### 数据库
- PostgreSQL 15+

## 快速开始

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

## Docker 部署

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
```

#### 4. 访问应用

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

### 数据备份与恢复

#### 备份数据库

```bash
# 创建备份
docker-compose exec postgres pg_dump -U mafuser mafstudio > backup_$(date +%Y%m%d).sql
```

#### 恢复数据库

```bash
# 恢复数据
cat backup.sql | docker-compose exec -T postgres psql -U mafuser mafstudio
```

## API 概览

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

### 协作管理
- `GET /api/collaborations` - 获取协作项目列表
- `GET /api/collaborations/{id}` - 获取协作项目详情
- `POST /api/collaborations` - 创建协作项目
- `DELETE /api/collaborations/{id}` - 删除协作项目
- `POST /api/collaborations/{id}/agents` - 添加智能体到协作
- `DELETE /api/collaborations/{id}/agents/{agentId}` - 移除智能体
- `POST /api/collaborations/{id}/tasks` - 创建任务
- `PATCH /api/collaborations/tasks/{taskId}/status` - 更新任务状态

### RAG服务
- `POST /api/rag/upload` - 上传文档
- `POST /api/rag/query` - RAG检索查询
- `GET /api/rag/documents` - 获取文档列表

### 系统管理
- `GET /api/systemlogs` - 获取系统日志
- `GET /api/operationlogs` - 获取操作日志
- `GET /api/systemconfigs` - 获取系统配置

## 许可证

MIT License

## 作者

pekingsot <北京醉鬼>
