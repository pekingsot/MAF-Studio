# MAF Studio 项目架构文档

> 本文档为AI助手提供项目的全局规范和架构概览，便于快速理解项目结构。

---

## 📋 目录

- [技术栈概览](#技术栈概览)
- [目录结构](#目录结构)
- [核心开发规范](#核心开发规范)
- [数据库设计](#数据库设计)
- [API设计规范](#api设计规范)
- [前端开发指南](#前端开发指南)
- [后端开发指南](#后端开发指南)
- [部署与运维](#部署与运维)

---

## 技术栈概览

### 前端技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| React | 18.2 | 前端框架 |
| TypeScript | 4.9 | 类型系统 |
| Ant Design | 5.0 | UI组件库 |
| React Router | 6.8 | 路由管理 |
| Axios | 1.4 | HTTP客户端 |
| Socket.io-client | 4.6 | WebSocket客户端 |
| react-app-rewired | 2.2.1 | 构建工具 |

### 后端技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 10.0 | 后端框架 |
| ASP.NET Core | 10.0 | Web API框架 |
| Dapper | 2.1.66 | ORM框架 |
| PostgreSQL | - | 关系型数据库 |
| Npgsql | 10.0.2 | PostgreSQL驱动 |
| JWT Bearer | 10.0.0-* | 身份认证 |
| SignalR | - | 实时通信 |
| Swagger | 7.3.2 | API文档 |

---

## 目录结构

### 前端目录结构 (`frontend/src/`)

```
src/
├── components/              # 组件目录
│   ├── Layout/             # 布局组件
│   │   └── MainLayout.tsx  # 主布局(侧边栏+顶部导航)
│   ├── common/             # 通用组件
│   │   ├── ConfirmDialog.tsx    # 确认对话框
│   │   ├── ErrorBoundary.tsx    # 错误边界
│   │   ├── PageHeader.tsx       # 页面头部
│   │   └── StatusTag.tsx        # 状态标签
│   └── PermissionGuard.tsx # 权限守卫组件
│
├── pages/                  # 页面组件(按功能模块划分)
│   ├── agents/            # 智能体管理模块
│   │   ├── AgentFormModal.tsx   # 智能体表单弹窗
│   │   ├── AgentTable.tsx       # 智能体表格
│   │   ├── useAgents.tsx        # 智能体逻辑Hook
│   │   └── types.ts             # 类型定义
│   ├── llm-configs/       # 大模型配置模块
│   │   ├── BatchAddModelsModal.tsx  # 批量添加模型弹窗
│   │   ├── ConfigFormModal.tsx      # 配置表单弹窗
│   │   ├── ModelFormModal.tsx       # 模型表单弹窗
│   │   ├── ModelList.tsx            # 模型列表
│   │   ├── useLLMConfigs.ts         # 配置逻辑Hook
│   │   └── types.ts                 # 类型定义
│   ├── collaboration-detail/  # 协作详情模块
│   ├── Dashboard.tsx      # 仪表盘
│   ├── Login.tsx          # 登录页
│   └── ...                # 其他页面
│
├── services/              # API服务层
│   ├── api.ts            # Axios实例+拦截器(核心)
│   ├── authService.ts    # 认证服务
│   ├── agentService.ts   # 智能体服务
│   ├── collaborationService.ts  # 协作服务
│   └── socketService.ts  # WebSocket服务
│
├── contexts/             # 全局状态管理
│   ├── AuthContext.tsx   # 认证状态(用户信息、权限)
│   └── I18nContext.tsx   # 国际化状态
│
├── hooks/                # 自定义Hooks
│   ├── usePermission.ts  # 权限检查Hook
│   └── useAbortController.ts  # 请求取消Hook
│
├── locales/              # 国际化资源
│   ├── zh-CN.ts         # 中文
│   └── en-US.ts         # 英文
│
├── constants/            # 常量定义
├── utils/                # 工具函数
├── types/                # TypeScript类型声明
├── styles/               # 全局样式
│   └── variables.css     # CSS变量
│
├── App.tsx               # 根组件(路由配置)
└── index.tsx             # 入口文件(Provider嵌套)
```

### 后端目录结构 (`backend/src/`)

采用**分层架构** + **依赖注入**设计模式：

```
src/
├── MAFStudio.Api/                    # API层(表现层)
│   ├── Controllers/                  # 控制器
│   │   ├── AgentsController.cs      # 智能体API
│   │   ├── LlmConfigsController.cs  # 大模型配置API
│   │   ├── AuthController.cs        # 认证API
│   │   └── ...                      # 其他控制器
│   ├── Hubs/                        # SignalR Hub
│   │   └── AgentHub.cs              # 智能体实时通信
│   ├── Middleware/                  # 中间件
│   │   ├── GlobalAuthorizationMiddleware.cs  # 全局授权
│   │   └── ApiCallLoggingMiddleware.cs       # API调用日志
│   ├── Filters/                     # 过滤器
│   │   └── GlobalExceptionFilter.cs # 全局异常处理
│   ├── Converters/                  # JSON转换器
│   │   └── LongToStringConverter.cs # Long转String(解决JS精度问题)
│   ├── Services/                    # API层服务
│   │   └── DatabaseInitializer.cs   # 数据库初始化
│   └── Program.cs                   # 程序入口(依赖注入配置)
│
├── MAFStudio.Application/            # 应用层(业务逻辑)
│   ├── Services/                    # 业务服务
│   │   ├── AgentService.cs          # 智能体业务逻辑
│   │   ├── LlmConfigService.cs      # 大模型配置业务逻辑
│   │   ├── AuthService.cs           # 认证业务逻辑
│   │   └── ...                      # 其他服务
│   ├── Mappers/                     # 对象映射
│   │   └── EntityMapper.cs          # Entity -> VO映射
│   ├── VOs/                         # 视图对象
│   │   ├── AgentVo.cs               # 智能体VO
│   │   ├── LlmConfigVo.cs           # 大模型配置VO
│   │   └── BaseVo.cs                # 基础VO
│   ├── DTOs/                        # 数据传输对象
│   │   └── Requests/                # 请求DTO
│   └── DependencyInjection.cs       # 应用层依赖注入配置
│
├── MAFStudio.Core/                   # 核心层(领域模型)
│   ├── Entities/                    # 实体类
│   │   ├── Agent.cs                 # 智能体实体
│   │   ├── LlmConfig.cs             # 大模型配置实体
│   │   ├── User.cs                  # 用户实体
│   │   └── ...                      # 其他实体
│   ├── Enums/                       # 枚举
│   │   ├── AgentStatus.cs           # 智能体状态
│   │   └── CollaborationStatus.cs   # 协作状态
│   ├── Interfaces/                  # 接口定义
│   │   ├── Repositories/            # 仓储接口
│   │   └── Services/                # 服务接口
│   └── Utils/                       # 工具类
│       └── SnowflakeIdGenerator.cs  # 雪花ID生成器(已弃用)
│
├── MAFStudio.Infrastructure/         # 基础设施层(数据访问)
│   ├── Data/
│   │   ├── Repositories/            # 仓储实现
│   │   │   ├── AgentRepository.cs   # 智能体仓储
│   │   │   ├── LlmConfigRepository.cs  # 大模型配置仓储
│   │   │   └── ...                  # 其他仓储
│   │   ├── Scripts/                 # 数据库迁移脚本
│   │   │   ├── V1__Initial.sql      # 初始化脚本
│   │   │   ├── V28__RemoveLlmConfigTestFields.sql  # 最新迁移
│   │   │   └── ...                  # 其他迁移脚本
│   │   └── DapperContext.cs         # Dapper数据库上下文
│   └── DependencyInjection.cs       # 基础设施层依赖注入配置
│
└── MAFStudio.Tests/                  # 测试层
    ├── Controllers/                 # 控制器测试
    ├── Services/                    # 服务测试
    └── TestBase.cs                  # 测试基类
```

---

## 核心开发规范

### 1. 数据请求规范

#### Axios拦截器配置 (`frontend/src/services/api.ts`)

```typescript
// 请求拦截器:自动添加Token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// 响应拦截器:自动刷新Token
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401 && !originalRequest._retry) {
      // Token过期,自动刷新
      const refreshToken = localStorage.getItem('refreshToken');
      const response = await axios.post('/auth/refresh', { refreshToken });
      const { token } = response.data;
      localStorage.setItem('token', token);
      originalRequest.headers.Authorization = `Bearer ${token}`;
      return api(originalRequest);
    }
    return Promise.reject(error);
  }
);
```

#### 跨域配置 (`backend/src/MAFStudio.Api/Program.cs`)

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

### 2. 状态管理规范

#### 全局状态 (`frontend/src/contexts/`)

- **AuthContext**: 用户信息、认证状态、权限检查
- **I18nContext**: 国际化语言切换

#### 本地状态

- 使用自定义Hook管理页面状态 (如 `useAgents`, `useLLMConfigs`)
- 使用 `useState` 管理组件内部状态

### 3. 样式与组件规范

#### 样式方案

- **主要方式**: Ant Design组件库 + 内联样式
- **CSS变量**: `frontend/src/styles/variables.css`
- **主题定制**: 通过Ant Design的ConfigProvider

#### 组件划分

- **通用组件** (`components/common/`): 可复用的基础组件
- **业务组件** (`pages/`): 特定业务功能的页面组件
- **布局组件** (`components/Layout/`): 页面布局结构

### 4. 命名约定

| 类型 | 命名规范 | 示例 |
|------|---------|------|
| 前端组件 | PascalCase | `AgentTable.tsx` |
| 前端Hook | camelCase + use前缀 | `useAgents.tsx` |
| 后端类 | PascalCase | `AgentService.cs` |
| 后端方法 | PascalCase | `GetAllAsync()` |
| 数据库表 | snake_case | `llm_model_configs` |
| 数据库字段 | snake_case | `llm_config_id` |

---

## 数据库设计

### ID策略

- **当前**: 数据库自增ID (从1000开始)
- **历史**: 曾使用Snowflake ID，已迁移

### 迁移脚本系统

```
V1__Initial.sql                    # 初始化
V23__MigrateToAutoIncrementIdAndCleanData.sql  # 迁移到自增ID
V24__ResetSequencesTo1000.sql      # 重置序列
V25__InsertLLMModelConfigs.sql     # 批量插入模型
V28__RemoveLlmConfigTestFields.sql # 最新迁移
```

### 冗余字段设计

为了优化查询性能，避免JOIN操作，部分表添加了冗余字段：

```sql
-- agents表示例
llm_config_name VARCHAR,      -- 冗余:大模型配置名称
llm_model_name VARCHAR,       -- 冗余:模型名称
type_name VARCHAR,            -- 冗余:智能体类型名称
fallback_models TEXT          -- JSON格式,包含冗余字段
```

### 核心表结构

#### agents (智能体表)

| 字段 | 类型 | 说明 |
|------|------|------|
| id | BIGINT | 主键(自增) |
| name | VARCHAR | 智能体名称 |
| type | VARCHAR | 智能体类型 |
| type_name | VARCHAR | 类型名称(冗余) |
| llm_config_id | BIGINT | 主模型配置ID |
| llm_config_name | VARCHAR | 配置名称(冗余) |
| llm_model_config_id | BIGINT | 主模型ID |
| llm_model_name | VARCHAR | 模型名称(冗余) |
| fallback_models | TEXT | 副模型配置(JSON) |
| status | VARCHAR | 状态 |
| user_id | BIGINT | 用户ID |

#### llm_configs (大模型配置表)

| 字段 | 类型 | 说明 |
|------|------|------|
| id | BIGINT | 主键(自增) |
| name | VARCHAR | 配置名称 |
| provider | VARCHAR | 提供商 |
| api_key | VARCHAR | API密钥(加密) |
| endpoint | VARCHAR | API端点 |
| is_enabled | BOOLEAN | 是否启用 |
| user_id | BIGINT | 用户ID |

#### llm_model_configs (模型配置表)

| 字段 | 类型 | 说明 |
|------|------|------|
| id | BIGINT | 主键(自增) |
| llm_config_id | BIGINT | 大模型配置ID |
| model_name | VARCHAR | 模型名称 |
| display_name | VARCHAR | 显示名称 |
| temperature | DECIMAL | 温度参数 |
| max_tokens | INT | 最大Token数 |
| context_window | INT | 上下文窗口 |
| last_test_time | TIMESTAMP | 最后测试时间 |
| availability_status | INT | 可用状态(0:不可用,1:可用) |
| test_result | TEXT | 测试结果 |

---

## API设计规范

### RESTful API设计

| HTTP方法 | 路径 | 说明 |
|---------|------|------|
| GET | /api/agents | 获取智能体列表 |
| GET | /api/agents/{id} | 获取单个智能体 |
| POST | /api/agents | 创建智能体 |
| PUT | /api/agents/{id} | 更新智能体 |
| DELETE | /api/agents/{id} | 删除智能体 |

### 统一响应格式

#### 成功响应

```json
{
  "id": 1001,
  "name": "智能助手",
  "type": "Assistant",
  "status": "Active"
}
```

#### 错误响应

```json
{
  "success": false,
  "error": "错误类型",
  "message": "错误详情",
  "detail": "堆栈信息(开发环境)",
  "path": "/api/agents/1001",
  "timestamp": "2026-03-29T10:00:00Z"
}
```

### 特殊API设计

#### 批量操作

```http
POST /api/llmconfigs/{id}/models/batch
Content-Type: application/json

{
  "modelNames": "qwen-max\nqwen-plus\nqwen-turbo",
  "temperature": 0.7,
  "maxTokens": 4096,
  "contextWindow": 64000
}
```

#### 并行测试

```http
POST /api/llmconfigs/{id}/test-all
```

后端使用 `Task.WhenAll` 并行测试所有模型：

```csharp
var testTasks = models.Select(async model =>
{
    var result = await _llmConfigService.TestModelConnectionAsync(id, model.Id);
    return new { modelId = model.Id, success = result.Success };
}).ToArray();

var results = await Task.WhenAll(testTasks);
```

---

## 前端开发指南

### 如何添加新页面

#### 1. 创建页面组件

在 `frontend/src/pages/` 下创建新目录：

```typescript
// frontend/src/pages/new-feature/index.tsx
import React from 'react';
import { useNewFeature } from './useNewFeature';

const NewFeature: React.FC = () => {
  const { data, loading, handleAction } = useNewFeature();

  return (
    <div>
      {/* 页面内容 */}
    </div>
  );
};

export default NewFeature;
```

#### 2. 创建自定义Hook

```typescript
// frontend/src/pages/new-feature/useNewFeature.ts
import { useState, useCallback } from 'react';
import api from '../../services/api';

export const useNewFeature = () => {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(false);

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const response = await api.get('/new-feature');
      setData(response.data);
    } finally {
      setLoading(false);
    }
  }, []);

  return { data, loading, loadData };
};
```

#### 3. 添加路由

在 `frontend/src/App.tsx` 中添加路由：

```typescript
const NewFeature = lazy(() => import('./pages/new-feature'));

// 在Routes中添加
<Route path="/new-feature" element={lazyLoad(NewFeature)} />
```

#### 4. 添加菜单项

在 `frontend/src/components/Layout/MainLayout.tsx` 中添加菜单：

```tsx
<Menu.Item key="/new-feature" icon={<StarOutlined />}>
  <Link to="/new-feature">新功能</Link>
</Menu.Item>
```

### 如何调用API

#### 基本调用

```typescript
import api from '../services/api';

// GET请求
const response = await api.get('/agents');
const agents = response.data;

// POST请求
const response = await api.post('/agents', {
  name: '新智能体',
  type: 'Assistant'
});

// PUT请求
await api.put('/agents/1001', {
  name: '更新后的名称'
});

// DELETE请求
await api.delete('/agents/1001');
```

#### 带Token的请求

Token会自动通过拦截器添加到请求头，无需手动处理：

```typescript
// 自动添加Authorization: Bearer {token}
const response = await api.get('/protected-resource');
```

#### 取消请求

```typescript
import { useAbortController } from '../hooks/useAbortController';

const MyComponent: React.FC = () => {
  const { signal, abort } = useAbortController();

  useEffect(() => {
    api.get('/agents', { signal })
      .then(response => setData(response.data))
      .catch(error => {
        if (error.name === 'AbortError') {
          console.log('请求已取消');
        }
      });

    return () => abort();
  }, []);
};
```

---

## 后端开发指南

### 如何添加新功能模块

#### 1. 创建实体类 (`MAFStudio.Core/Entities/`)

```csharp
[Dapper.Contrib.Extensions.Table("new_features")]
public class NewFeature : BaseEntity
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

#### 2. 创建仓储接口 (`MAFStudio.Core/Interfaces/Repositories/`)

```csharp
public interface INewFeatureRepository
{
    Task<List<NewFeature>> GetAllAsync();
    Task<NewFeature?> GetByIdAsync(long id);
    Task<NewFeature> CreateAsync(NewFeature entity);
    Task UpdateAsync(NewFeature entity);
    Task DeleteAsync(long id);
}
```

#### 3. 实现仓储 (`MAFStudio.Infrastructure/Data/Repositories/`)

```csharp
public class NewFeatureRepository : INewFeatureRepository
{
    private readonly IDapperContext _context;

    public NewFeatureRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<List<NewFeature>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT * FROM new_features ORDER BY created_at DESC";
        var result = await connection.QueryAsync<NewFeature>(sql);
        return result.ToList();
    }

    // 实现其他方法...
}
```

#### 4. 创建服务接口 (`MAFStudio.Core/Interfaces/Services/`)

```csharp
public interface INewFeatureService
{
    Task<List<NewFeature>> GetAllAsync();
    Task<NewFeature?> GetByIdAsync(long id);
    Task<NewFeature> CreateAsync(string name, string? description);
}
```

#### 5. 实现服务 (`MAFStudio.Application/Services/`)

```csharp
public class NewFeatureService : INewFeatureService
{
    private readonly INewFeatureRepository _repository;

    public NewFeatureService(INewFeatureRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<NewFeature>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    // 实现其他方法...
}
```

#### 6. 创建控制器 (`MAFStudio.Api/Controllers/`)

```csharp
[ApiController]
[Route("api/[controller]")]
public class NewFeaturesController : ControllerBase
{
    private readonly INewFeatureService _service;

    public NewFeaturesController(INewFeatureService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<NewFeature>>> GetAll()
    {
        var features = await _service.GetAllAsync();
        return Ok(features);
    }

    [HttpPost]
    public async Task<ActionResult<NewFeature>> Create([FromBody] CreateNewFeatureRequest request)
    {
        var feature = await _service.CreateAsync(request.Name, request.Description);
        return CreatedAtAction(nameof(GetById), new { id = feature.Id }, feature);
    }
}
```

#### 7. 注册依赖注入

在 `MAFStudio.Infrastructure/DependencyInjection.cs` 中：

```csharp
services.AddScoped<INewFeatureRepository, NewFeatureRepository>();
```

在 `MAFStudio.Application/DependencyInjection.cs` 中：

```csharp
services.AddScoped<INewFeatureService, NewFeatureService>();
```

#### 8. 创建数据库迁移脚本

在 `MAFStudio.Infrastructure/Data/Scripts/` 中创建 `V29__CreateNewFeaturesTable.sql`：

```sql
CREATE TABLE new_features (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_new_features_created_at ON new_features(created_at);
```

### 如何使用事务

```csharp
public async Task DeleteWithRelatedDataAsync(long id)
{
    using var connection = _context.CreateConnection();
    await connection.OpenAsync(); // 必须先打开连接
    
    using var transaction = await connection.BeginTransactionAsync();
    
    try
    {
        // 删除关联数据
        await connection.ExecuteAsync(
            "DELETE FROM related_data WHERE feature_id = @Id",
            new { Id = id },
            transaction
        );

        // 删除主数据
        await connection.ExecuteAsync(
            "DELETE FROM new_features WHERE id = @Id",
            new { Id = id },
            transaction
        );

        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

---

## 部署与运维

### 本地开发环境

#### 启动后端

```bash
cd backend
dotnet run --project src/MAFStudio.Api/MAFStudio.Api.csproj
```

后端运行在: `http://localhost:5000`

#### 启动前端

```bash
cd frontend
npm start
```

前端运行在: `http://localhost:3000`

### Docker部署

使用 `docker-compose.yml` 一键部署：

```bash
docker-compose up -d
```

### 数据库迁移

迁移脚本会自动执行，无需手动运行。

### 环境变量

#### 后端环境变量 (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=mafstudio;Username=user;Password=password"
  },
  "Jwt": {
    "Key": "your-secret-key",
    "Issuer": "MAFStudio",
    "Audience": "MAFStudio"
  }
}
```

#### 前端环境变量 (`.env`)

```
REACT_APP_API_URL=http://localhost:5000/api
```

---

## 性能优化

### 后端优化

1. **冗余字段**: 避免JOIN查询
2. **并行处理**: 使用 `Task.WhenAll` 并行执行任务
3. **异步操作**: 所有IO操作使用异步方法
4. **数据库索引**: 为常用查询字段创建索引

### 前端优化

1. **懒加载**: 路由组件懒加载
2. **请求取消**: 使用 AbortController 取消未完成的请求
3. **局部更新**: 只更新变化的数据，避免全页面刷新
4. **防抖节流**: 对频繁操作使用防抖/节流

---

## 安全性

### 认证与授权

- **JWT认证**: 无状态Token认证
- **Token刷新**: 自动刷新过期Token
- **权限控制**: 基于角色的权限控制(RBAC)
- **全局授权中间件**: 统一权限检查

### 数据安全

- **密码加密**: 使用BCrypt加密存储
- **API密钥加密**: 敏感信息加密存储
- **SQL注入防护**: 使用参数化查询
- **XSS防护**: React自动转义

---

## 测试

### 后端测试

```bash
cd backend
dotnet test
```

### 前端测试

```bash
cd frontend
npm test
```

---

## 常见问题

### 1. JavaScript大数字精度问题

**问题**: JavaScript的Number类型无法精确表示大于 `Number.MAX_SAFE_INTEGER` 的整数。

**解决方案**: 后端使用 `LongToStringConverter` 将long类型转为string返回前端。

### 2. 跨域问题

**解决方案**: 后端配置CORS，允许前端域名访问。

### 3. Token过期

**解决方案**: 前端拦截器自动刷新Token。

### 4. 数据库迁移失败

**解决方案**: 检查迁移脚本版本号是否连续，确保脚本语法正确。

---

## 技术债务

### 已知问题

1. **Snowflake ID遗留代码**: 虽然已迁移到自增ID，但部分代码仍保留Snowflake ID生成器
2. **测试覆盖率不足**: 部分新功能缺少单元测试
3. **国际化不完整**: 部分页面文本未国际化

### 改进建议

1. 增加集成测试
2. 完善API文档
3. 添加性能监控
4. 优化前端状态管理(考虑引入Redux或Zustand)

---

## 联系方式

如有问题，请联系项目维护者。

---

**最后更新**: 2026-03-29
**版本**: 1.0.0
