# MAF Studio - 多智能体协作平台

## 项目简介

MAF Studio 是一个基于 .NET 10.0 的多智能体协作平台，支持创建、管理和协调多个 AI 智能体进行协作任务。平台采用 DDD（领域驱动设计）架构，使用 Dapper 作为 ORM，PostgreSQL 作为数据库。

## 技术栈

- **后端框架**: .NET 10.0
- **ORM**: Dapper (轻量级高性能 ORM)
- **数据库**: PostgreSQL 15+
- **认证**: JWT Bearer Token
- **架构模式**: DDD (领域驱动设计) + CQRS

## 更新日志

### v1.1.0 (2024-03-26)

#### 重构：ID生成策略优化

**1. 雪花算法ID生成器**
- 新增 `SnowflakeIdGenerator` 单例类，用于生成全局唯一ID
- 替换原有的 UUID/Guid 方案，提升ID可读性和索引效率
- 支持：每毫秒4096个ID、32个数据中心、每数据中心32台机器

**2. 实体类重构**
- 所有实体类ID类型从 `Guid` 改为 `long`
- 新增 `BaseEntityWithUpdate` 基类，统一管理 `UpdatedAt` 字段
- 受影响实体：`Agent`、`Collaboration`、`LlmConfig`、`User`、`SystemConfig`

**3. 数据库迁移**
- 新增 `V3__CleanupAndMigrateToSnowflake.sql` 迁移脚本
- 主键类型从 `UUID` 改为 `BIGINT`
- 移除数据库触发器，改为应用层维护 `UpdatedAt`

**4. Repository层优化**
- 所有Repository的 `UpdateAsync` 方法调用 `MarkAsUpdated()` 自动更新时间戳
- `UpdateStatusAsync` 方法同步更新 `updated_at` 字段

#### 优势对比

| 特性 | 雪花算法 | UUID |
|------|----------|------|
| 长度 | 19位数字 | 36位字符串 |
| 可读性 | 高 | 低 |
| 索引效率 | 高 | 低 |
| 排序性 | 时间有序 | 无序 |
| 存储空间 | 8字节 | 16字节 |

---

## 项目结构

```
MAFStudio/
├── src/
│   ├── MAFStudio.Core/              # 核心层 - 实体、接口、枚举
│   │   ├── Entities/                # 领域实体
│   │   │   ├── BaseEntityWithUpdate.cs  # 更新时间追踪基类
│   │   │   ├── Agent.cs
│   │   │   ├── Collaboration.cs
│   │   │   ├── LlmConfig.cs
│   │   │   └── ...
│   │   ├── Enums/                   # 枚举定义
│   │   ├── Interfaces/              # 接口定义
│   │   │   ├── Repositories/        # 仓储接口
│   │   │   └── Services/            # 服务接口
│   │   └── Utils/                   # 工具类
│   │       └── SnowflakeIdGenerator.cs  # 雪花算法ID生成器
│   │
│   ├── MAFStudio.Infrastructure/    # 基础设施层
│   │   ├── Data/
│   │   │   ├── Repositories/        # 仓储实现
│   │   │   └── Scripts/             # 数据库脚本
│   │   └── DependencyInjection.cs   # 依赖注入配置
│   │
│   ├── MAFStudio.Application/       # 应用层
│   │   ├── Services/                # 应用服务
│   │   ├── DTOs/                    # 数据传输对象
│   │   ├── VOs/                     # 值对象
│   │   └── Mappers/                 # 对象映射
│   │
│   └── MAFStudio.Api/               # API层
│       ├── Controllers/             # 控制器
│       ├── Middleware/              # 中间件
│       └── Program.cs               # 程序入口
│
└── tests/
    └── MAFStudio.Tests/             # 单元测试
```

## 核心功能

### 1. 智能体管理 (Agent)
- 创建、编辑、删除智能体
- 配置智能体类型和参数
- 关联 LLM 配置
- 状态管理（激活/停用）

### 2. LLM 配置管理
- 支持多种 LLM 提供商（OpenAI、Azure、本地模型等）
- API Key 加密存储
- 模型配置管理
- 测试连接功能

### 3. 协作项目管理 (Collaboration)
- 创建协作项目
- 添加/移除智能体
- 任务分配与跟踪
- Git 仓库集成

### 4. 消息系统
- 智能体间消息传递
- 用户与智能体交互
- 流式消息支持

### 5. RAG 文档管理
- 文档上传与处理
- 文档分块存储
- 向量检索支持

## 数据库设计

### ID 生成策略

项目使用**雪花算法 (Snowflake)** 生成唯一 ID。

### 主要数据表

```sql
-- 用户表（ID来自认证系统，使用VARCHAR）
users (id VARCHAR(36) PRIMARY KEY)

-- 智能体表
agents (
    id BIGINT PRIMARY KEY,          -- 雪花算法ID
    name VARCHAR(100),
    type VARCHAR(50),
    configuration JSONB,
    llm_config_id BIGINT,           -- 外键关联LLM配置
    ...
)

-- LLM配置表
llm_configs (
    id BIGINT PRIMARY KEY,
    name VARCHAR(100),
    provider VARCHAR(50),
    api_key TEXT,
    ...
)

-- 协作项目表
collaborations (
    id BIGINT PRIMARY KEY,
    name VARCHAR(200),
    user_id VARCHAR(36),
    git_repository_url VARCHAR(500),
    ...
)
```

### PostgreSQL 最佳实践

1. **命名规范**: 表名和字段名使用小写 + 下划线 (如 `agent_messages`)
2. **主键类型**: 使用 `BIGINT` 存储雪花算法 ID
3. **字符串类型**: 优先使用 `TEXT`，除非有明确长度限制
4. **JSON 字段**: 使用 `JSONB` 支持索引和快速查询
5. **金额类型**: 使用 `NUMERIC` 确保精度
6. **UpdatedAt字段**: 由应用层维护，不使用数据库触发器

## 快速开始

### 环境要求

- .NET 10.0 SDK
- PostgreSQL 15+
- Docker (可选)

### 配置数据库

1. 创建 PostgreSQL 数据库：
```sql
CREATE DATABASE maf_studio;
```

2. 执行数据库迁移脚本：
```bash
# 执行 V1__InitialSchema.sql
# 执行 V2__AddLlmModelConfig.sql
# 执行 V3__CleanupAndMigrateToSnowflake.sql (清理旧表并迁移到雪花ID)
```

### 配置应用

1. 修改 `appsettings.json`：
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=maf_studio;Username=postgres;Password=your_password"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-at-least-32-characters-long",
    "Issuer": "MAFStudio",
    "Audience": "MAFStudio",
    "ExpirationMinutes": 1440
  }
}
```

### 运行应用

```bash
cd backend/src/MAFStudio.Api
dotnet run
```

应用将在 `http://localhost:5000` 启动。

## API 接口

### 认证接口

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | /api/auth/register | 用户注册 |
| POST | /api/auth/login | 用户登录 |
| GET | /api/auth/me | 获取当前用户信息 |

### 智能体接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/agents | 获取智能体列表 |
| GET | /api/agents/{id} | 获取智能体详情 |
| POST | /api/agents | 创建智能体 |
| PUT | /api/agents/{id} | 更新智能体 |
| DELETE | /api/agents/{id} | 删除智能体 |
| PATCH | /api/agents/{id}/status | 更新智能体状态 |

### LLM 配置接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/llmconfigs | 获取 LLM 配置列表 |
| GET | /api/llmconfigs/{id} | 获取配置详情 |
| POST | /api/llmconfigs | 创建配置 |
| PUT | /api/llmconfigs/{id} | 更新配置 |
| DELETE | /api/llmconfigs/{id} | 删除配置 |

### 协作项目接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | /api/collaborations | 获取项目列表 |
| GET | /api/collaborations/{id} | 获取项目详情 |
| POST | /api/collaborations | 创建项目 |
| DELETE | /api/collaborations/{id} | 删除项目 |
| POST | /api/collaborations/{id}/agents | 添加智能体 |
| DELETE | /api/collaborations/{id}/agents/{agentId} | 移除智能体 |
| POST | /api/collaborations/{id}/tasks | 创建任务 |

## 雪花算法 ID 生成器

```csharp
// 使用示例
var id = SnowflakeIdGenerator.Instance.NextId();
// 输出: 1234567890123456789 (19位数字)

// 配置参数
public class SnowflakeIdGenerator
{
    private const long Twepoch = 1288834974657L;  // 起始时间戳
    private const int WorkerIdBits = 5;           // 工作机器ID位数
    private const int DatacenterIdBits = 5;       // 数据中心ID位数
    private const int SequenceBits = 12;          // 序列号位数
    
    // 每毫秒可生成 4096 个 ID
    // 支持部署 32 个数据中心，每个数据中心 32 台机器
}
```

## 开发指南

### 添加新实体

1. 在 `MAFStudio.Core/Entities` 创建实体类：
```csharp
[Dapper.Contrib.Extensions.Table("your_table")]
public class YourEntity : BaseEntityWithUpdate  // 继承基类获得UpdatedAt支持
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }
    
    // 其他属性...
    
    public void GenerateId()
    {
        Id = SnowflakeIdGenerator.Instance.NextId();
    }
}
```

2. 创建仓储接口和实现
3. 创建服务接口和实现
4. 添加控制器

### 运行测试

```bash
cd tests/MAFStudio.Tests
dotnet test
```

## 部署

### Docker 部署

```bash
# 构建镜像
docker build -t maf-studio:latest .

# 运行容器
docker run -d \
  --name maf-studio \
  -p 5000:80 \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=maf_studio;Username=postgres;Password=password" \
  maf-studio:latest
```

### 环境变量

| 变量名 | 说明 |
|--------|------|
| ConnectionStrings__DefaultConnection | 数据库连接字符串 |
| JwtSettings__SecretKey | JWT 密钥 |
| JwtSettings__ExpirationMinutes | Token 过期时间 |

## 常见问题

### Q: 为什么使用雪花算法而不是 UUID？
A: 雪花算法生成的 ID 更短、可读性更好、索引效率更高，且具有时间有序性。

### Q: 为什么选择 Dapper 而不是 EF Core？
A: Dapper 更轻量、性能更高，适合需要精细控制 SQL 的场景。

### Q: 为什么移除数据库触发器？
A: 将业务逻辑集中在应用层，提高可维护性、可测试性和数据库可移植性。

### Q: 如何清理测试数据？
A: 执行 `V3__CleanupAndMigrateToSnowflake.sql` 脚本会删除所有旧表并重建。

## 许可证

MIT License

## 贡献指南

1. Fork 项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request
