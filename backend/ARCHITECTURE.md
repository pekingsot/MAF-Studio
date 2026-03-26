# MAF Studio 对象分层架构设计

## 架构概述

本项目采用清晰的分层架构，严格遵循对象转换原则，避免直接将数据库实体暴露给前端。

## 对象分层说明

### 1. **DO (Data Object) - 数据对象**
- **位置**: `Data/` 目录
- **用途**: 数据库实体映射
- **特点**: 
  - 与数据库表一一对应
  - 包含导航属性和 EF Core 注解
  - 仅在数据访问层使用
- **示例**: `Agent`, `Collaboration`, `AgentMessage`

### 2. **DTO (Data Transfer Object) - 数据传输对象**
- **位置**: `Models/DTOs/` 目录
- **用途**: 服务间数据传输、API 请求/响应
- **特点**:
  - 扁平化结构
  - 只包含必要字段
  - 用于跨层传输
- **示例**: 
  - `UserMessageDto` - 用户消息传输
  - `AgentStartDto` - 智能体开始响应
  - `ApiResponseDto<T>` - 统一 API 响应包装

### 3. **VO (View Object) - 视图对象**
- **位置**: `Models/VOs/` 目录
- **用途**: 返回给前端的数据结构
- **特点**:
  - 格式化后的数据（如时间戳转字符串）
  - 脱敏处理（移除敏感字段）
  - 前端友好的字段命名
- **示例**:
  - `AgentVo` - 智能体详情
  - `AgentListItemVo` - 智能体列表项
  - `CollaborationVo` - 协作项目详情

### 4. **BO (Business Object) - 业务对象**
- **位置**: `Services/` 目录
- **用途**: 封装业务逻辑
- **特点**:
  - 包含业务规则
  - 聚合多个 DO
  - 服务层内部使用

### 5. **POJO (Plain Old CLR Object)**
- **位置**: `Models/` 目录
- **用途**: 简单的数据结构
- **特点**:
  - 纯 C# 对象
  - 无框架依赖
  - 用于配置、请求等

## 对象转换流程

```
前端请求 → DTO → Service 处理 → DO (数据库操作)
                ↓
前端响应 ← VO ← Service 转换 ← DO (查询结果)
```

## 转换工具

### EntityMapper (手动映射)
- **位置**: `Models/Mappers/EntityMapper.cs`
- **特点**: 
  - 扩展方法实现
  - 类型安全
  - 编译时检查
  - 无运行时开销

**示例**:
```csharp
// DO 转 VO
var agentVo = agent.ToVo();

// 列表转换
var agentListVos = agents.Select(a => a.ToListItemVo()).ToList();
```

## 设计原则

### 1. 单一职责原则
- 每种对象只负责一个职责
- DO 负责数据持久化
- DTO 负责数据传输
- VO 负责视图展示

### 2. 依赖倒置原则
- Controller 依赖 VO/DTO，不依赖 DO
- Service 依赖 DO/DTO，返回 VO/DTO
- Repository 依赖 DO

### 3. 接口隔离原则
- 不同层使用不同的对象
- 避免暴露不需要的字段

### 4. 开闭原则
- 新增功能时扩展对象类型
- 不修改现有对象结构

## 序列化策略

### JSON 序列化配置
```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
```

### 时间格式化
- **DO**: `DateTime` 类型
- **VO**: `string` 类型 (ISO 8601 格式)
- **转换**: `BaseVo.FormatDateTime()`

## 最佳实践

### ✅ 正确做法
```csharp
// Controller 返回 VO
[HttpGet]
public async Task<ActionResult<AgentVo>> GetAgent(Guid id)
{
    var agent = await _agentService.GetAgentByIdAsync(id);
    return Ok(agent.ToVo());
}
```

### ❌ 错误做法
```csharp
// 直接返回 DO (会导致序列化问题)
[HttpGet]
public async Task<ActionResult<Agent>> GetAgent(Guid id)
{
    var agent = await _agentService.GetAgentByIdAsync(id);
    return Ok(agent); // 错误！包含导航属性
}
```

## 架构优势

1. **解耦合**: 各层对象独立，降低耦合度
2. **安全性**: 避免暴露敏感字段和导航属性
3. **性能**: 减少不必要的数据传输
4. **可维护性**: 清晰的对象职责，易于维护
5. **可扩展性**: 易于添加新功能
6. **序列化安全**: 避免循环引用和复杂对象序列化问题

## 文件结构

```
backend/
├── Data/                    # DO - 数据对象
│   ├── Agent.cs
│   ├── Collaboration.cs
│   └── AgentMessage.cs
├── Models/
│   ├── DTOs/               # DTO - 数据传输对象
│   │   ├── BaseDto.cs
│   │   └── UserMessageDto.cs
│   ├── VOs/                # VO - 视图对象
│   │   ├── BaseVo.cs
│   │   ├── AgentVo.cs
│   │   └── CollaborationVo.cs
│   ├── Mappers/            # 对象映射
│   │   └── EntityMapper.cs
│   └── Requests/           # 请求对象
│       └── CreateAgentRequest.cs
├── Services/               # 业务逻辑层
└── Controllers/            # API 控制器
```

## 总结

通过严格的分层架构和对象转换，我们实现了：
- 清晰的代码结构
- 安全的数据传输
- 高效的序列化
- 易于维护的代码库

**记住**: 永远不要直接将 DO 返回给前端！
