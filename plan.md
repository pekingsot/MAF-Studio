# MAF Studio 架构优化方案

## 一、当前架构问题分析

### 1. 权限管理问题
**现状**：
- 每个 Controller 都需要手动判断权限（如 `IsAdminAsync`）
- 没有统一的权限管理机制
- 没有角色管理功能
- 代码重复，维护困难

**问题**：
```csharp
// 当前方式：每个 Controller 都需要手动判断
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var isAdmin = await _authService.IsAdminAsync(userId!);
if (!isAdmin)
{
    return Forbid();
}
```

### 2. 登录拦截问题
**现状**：
- 没有全局的登录拦截
- 没有配置化的权限白名单
- 每个接口都需要手动添加 `[Authorize]` 特性

**问题**：
- 容易遗漏权限验证
- 无法统一管理公开接口

### 3. DDD 架构问题
**现状**：
- 底层 Repository 直接组装数据
- 查询逻辑混乱，职责不清晰
- 违反了 DDD 的分层原则

**问题示例**：
```csharp
// 当前方式：Repository 层直接组装数据
public async Task<List<Agent>> GetByUserIdAsync(long userId, bool isAdmin)
{
    var agents = await QueryAsync(
        "SELECT * FROM agents WHERE user_id = @UserId OR @IsAdmin = true",
        new { UserId = userId, IsAdmin = isAdmin });
    
    // 在 Repository 层组装 LlmConfig 数据
    foreach (var agent in agents)
    {
        if (agent.LlmConfigId.HasValue)
        {
            agent.AllLlmConfigs = await _llmConfigRepository.GetAllAsync();
        }
    }
    
    return agents;
}
```

### 4. JWT 优化问题
**现状**：
- 每次请求都查询数据库获取角色信息
- 没有充分利用 JWT 的优势
- 性能浪费

**问题**：
```csharp
// 当前方式：每次都查询数据库
var isAdmin = await _authService.IsAdminAsync(userId!);
```

---

## 二、解决方案设计

### 1. 权限管理架构（RBAC 模型）

#### 1.1 数据库设计

**用户表（users）**：已存在

**角色表（roles）**：
```sql
CREATE TABLE roles (
    id BIGINT PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    code VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    is_system BOOLEAN DEFAULT FALSE,
    is_enabled BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 初始数据
INSERT INTO roles (id, name, code, description, is_system) VALUES
(1000000000000001, '超级管理员', 'SUPER_ADMIN', '系统超级管理员，拥有所有权限', TRUE),
(1000000000000002, '管理员', 'ADMIN', '系统管理员', TRUE),
(1000000000000003, '普通用户', 'USER', '普通用户', TRUE);
```

**权限表（permissions）**：
```sql
CREATE TABLE permissions (
    id BIGINT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    code VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    resource VARCHAR(100) NOT NULL,
    action VARCHAR(50) NOT NULL,
    is_enabled BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 初始数据
INSERT INTO permissions (id, name, code, resource, action) VALUES
(1000000000000001, '查看智能体', 'agent:read', 'agent', 'read'),
(1000000000000002, '创建智能体', 'agent:create', 'agent', 'create'),
(1000000000000003, '编辑智能体', 'agent:update', 'agent', 'update'),
(1000000000000004, '删除智能体', 'agent:delete', 'agent', 'delete'),
(1000000000000005, '查看大模型配置', 'llmconfig:read', 'llmconfig', 'read'),
(1000000000000006, '创建大模型配置', 'llmconfig:create', 'llmconfig', 'create'),
(1000000000000007, '编辑大模型配置', 'llmconfig:update', 'llmconfig', 'update'),
(1000000000000008, '删除大模型配置', 'llmconfig:delete', 'llmconfig', 'delete'),
(1000000000000009, '查看系统日志', 'log:read', 'log', 'read'),
(1000000000000010, '管理用户', 'user:manage', 'user', 'manage');
```

**用户角色关联表（user_roles）**：
```sql
CREATE TABLE user_roles (
    id BIGINT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, role_id)
);
```

**角色权限关联表（role_permissions）**：
```sql
CREATE TABLE role_permissions (
    id BIGINT PRIMARY KEY,
    role_id BIGINT NOT NULL,
    permission_id BIGINT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(role_id, permission_id)
);

-- 初始数据：超级管理员拥有所有权限
INSERT INTO role_permissions (id, role_id, permission_id)
SELECT nextval('snowflake_seq'), 1000000000000001, id FROM permissions;

-- 管理员权限
INSERT INTO role_permissions (id, role_id, permission_id) VALUES
(2000000000000001, 1000000000000002, 1000000000000001),
(2000000000000002, 1000000000000002, 1000000000000002),
(2000000000000003, 1000000000000002, 1000000000000003),
(2000000000000004, 1000000000000002, 1000000000000004),
(2000000000000005, 1000000000000002, 1000000000000005),
(2000000000000006, 1000000000000002, 1000000000000006),
(2000000000000007, 1000000000000002, 1000000000000007),
(2000000000000008, 1000000000000002, 1000000000000008),
(2000000000000009, 1000000000000002, 1000000000000009);

-- 普通用户权限
INSERT INTO role_permissions (id, role_id, permission_id) VALUES
(3000000000000001, 1000000000000003, 1000000000000001),
(3000000000000002, 1000000000000003, 1000000000000002),
(3000000000000003, 1000000000000003, 1000000000000003),
(3000000000000004, 1000000000000003, 1000000000000005);
```

#### 1.2 核心实体设计

**Role 实体**：
```csharp
namespace MAFStudio.Core.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsEnabled { get; set; }
}
```

**Permission 实体**：
```csharp
namespace MAFStudio.Core.Entities;

public class Permission : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
```

**UserRole 实体**：
```csharp
namespace MAFStudio.Core.Entities;

public class UserRole : BaseEntity
{
    public long UserId { get; set; }
    public long RoleId { get; set; }
}
```

**RolePermission 实体**：
```csharp
namespace MAFStudio.Core.Entities;

public class RolePermission : BaseEntity
{
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
}
```

#### 1.3 JWT Claims 设计

**将角色和权限信息存储在 JWT 中**：
```csharp
public class JwtService
{
    public string GenerateToken(User user, List<string> roles, List<string> permissions)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim("roles", string.Join(",", roles)),
            new Claim("permissions", string.Join(",", permissions))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

---

### 2. 全局登录拦截设计

#### 2.1 权限配置文件

**创建权限配置文件（permission_config.json）**：
```json
{
  "AllowAnonymous": [
    "/api/auth/login",
    "/api/auth/register",
    "/api/health",
    "/api/swagger",
    "/api/index.html"
  ],
  "RequireAuthentication": [
    "/api/*"
  ],
  "RequirePermission": {
    "/api/agents": {
      "GET": "agent:read",
      "POST": "agent:create",
      "PUT": "agent:update",
      "DELETE": "agent:delete"
    },
    "/api/llmconfigs": {
      "GET": "llmconfig:read",
      "POST": "llmconfig:create",
      "PUT": "llmconfig:update",
      "DELETE": "llmconfig:delete"
    },
    "/api/logs": {
      "GET": "log:read"
    },
    "/api/users": {
      "*": "user:manage"
    }
  }
}
```

#### 2.2 全局授权中间件

**创建全局授权中间件**：
```csharp
namespace MAFStudio.Api.Middleware;

public class GlobalAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GlobalAuthorizationMiddleware> _logger;
    private readonly PermissionConfig _permissionConfig;

    public GlobalAuthorizationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<GlobalAuthorizationMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
        
        // 加载权限配置
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "permission_config.json");
        var configJson = File.ReadAllText(configPath);
        _permissionConfig = JsonSerializer.Deserialize<PermissionConfig>(configJson);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        var method = context.Request.Method;

        // 检查是否在白名单中
        if (IsAllowAnonymous(path))
        {
            await _next(context);
            return;
        }

        // 检查是否已登录
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "未登录，请先登录" });
            return;
        }

        // 检查权限
        var requiredPermission = GetRequiredPermission(path, method);
        if (!string.IsNullOrEmpty(requiredPermission))
        {
            var userPermissions = GetUserPermissions(context.User);
            if (!userPermissions.Contains(requiredPermission))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { message = "权限不足" });
                return;
            }
        }

        await _next(context);
    }

    private bool IsAllowAnonymous(string path)
    {
        return _permissionConfig.AllowAnonymous.Any(p => 
            p.Equals(path, StringComparison.OrdinalIgnoreCase) ||
            (p.EndsWith("*") && path.StartsWith(p.TrimEnd('*'), StringComparison.OrdinalIgnoreCase)));
    }

    private string? GetRequiredPermission(string path, string method)
    {
        foreach (var config in _permissionConfig.RequirePermission)
        {
            if (path.StartsWith(config.Key, StringComparison.OrdinalIgnoreCase))
            {
                if (config.Value.TryGetValue(method, out var permission))
                {
                    return permission;
                }
                if (config.Value.TryGetValue("*", out var wildcardPermission))
                {
                    return wildcardPermission;
                }
            }
        }
        return null;
    }

    private List<string> GetUserPermissions(ClaimsPrincipal user)
    {
        var permissionsClaim = user.FindFirst("permissions")?.Value;
        if (string.IsNullOrEmpty(permissionsClaim))
        {
            return new List<string>();
        }
        return permissionsClaim.Split(',').ToList();
    }
}

public class PermissionConfig
{
    public List<string> AllowAnonymous { get; set; } = new();
    public List<string> RequireAuthentication { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> RequirePermission { get; set; } = new();
}
```

#### 2.3 注册中间件

**在 Program.cs 中注册**：
```csharp
// 添加认证服务
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// 添加授权服务
builder.Services.AddAuthorization();

// 注册全局授权中间件
app.UseMiddleware<GlobalAuthorizationMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
```

---

### 3. DDD 架构优化设计

#### 3.1 分层职责明确

**Infrastructure 层（Repository）**：
- **职责**：单表查询，不组装数据
- **原则**：只负责数据持久化，不包含业务逻辑

```csharp
namespace MAFStudio.Infrastructure.Data.Repositories;

public class AgentRepository : IAgentRepository
{
    // ✅ 正确：单表查询
    public async Task<Agent?> GetByIdAsync(long id)
    {
        const string sql = "SELECT * FROM agents WHERE id = @Id";
        return await QueryFirstOrDefaultAsync<Agent>(sql, new { Id = id });
    }

    // ✅ 正确：单表查询，带条件
    public async Task<List<Agent>> GetByUserIdAsync(long userId)
    {
        const string sql = "SELECT * FROM agents WHERE user_id = @UserId";
        return await QueryAsync<Agent>(sql, new { UserId = userId });
    }

    // ❌ 错误：不要在 Repository 层组装数据
    // public async Task<List<Agent>> GetByUserIdWithLlmConfigAsync(long userId)
    // {
    //     var agents = await GetByUserIdAsync(userId);
    //     foreach (var agent in agents)
    //     {
    //         agent.LlmConfig = await _llmConfigRepository.GetByIdAsync(agent.LlmConfigId);
    //     }
    //     return agents;
    // }
}
```

**Application 层（Service）**：
- **职责**：组装数据，调用多个 Repository
- **原则**：包含业务逻辑，协调多个 Repository

```csharp
namespace MAFStudio.Application.Services;

public class AgentService : IAgentService
{
    private readonly IAgentRepository _agentRepository;
    private readonly ILlmConfigRepository _llmConfigRepository;
    private readonly ILlmModelConfigRepository _llmModelConfigRepository;

    // ✅ 正确：在 Service 层组装数据
    public async Task<List<Agent>> GetByUserIdAsync(long userId, bool isAdmin)
    {
        // 1. 查询智能体列表（单表查询）
        var agents = isAdmin
            ? await _agentRepository.GetAllAsync()
            : await _agentRepository.GetByUserIdAsync(userId);

        // 2. 查询所有大模型配置（单表查询）
        var allLlmConfigs = await _llmConfigRepository.GetAllAsync();

        // 3. 在内存中组装数据
        foreach (var agent in agents)
        {
            agent.AllLlmConfigs = allLlmConfigs;
        }

        return agents;
    }
}
```

**Api 层（Controller）**：
- **职责**：接收请求，调用 Service，返回响应
- **原则**：不包含业务逻辑，只做请求转发

```csharp
namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _agentService;

    // ✅ 正确：Controller 只做请求转发
    [HttpGet]
    public async Task<ActionResult<AgentListVo>> GetAllAgents()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole("SUPER_ADMIN") || User.IsInRole("ADMIN");

        var agents = await _agentService.GetByUserIdAsync(long.Parse(userId!), isAdmin);
        var agentTypes = await _agentTypeRepository.GetEnabledAsync();

        var result = new AgentListVo
        {
            Agents = agents.Select(a => a.ToListItemVo()).ToList(),
            AgentTypes = agentTypes.Select(at => at.ToVo()).ToList()
        };

        return Ok(result);
    }
}
```

#### 3.2 查询优化策略

**使用 VO（View Object）组装数据**：
```csharp
namespace MAFStudio.Application.VOs;

public class AgentListVo
{
    public List<AgentListItemVo> Agents { get; set; } = new();
    public List<AgentTypeVo> AgentTypes { get; set; } = new();
}

public class AgentListItemVo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LlmConfigName { get; set; }
    public string? PrimaryModelName { get; set; }
    public List<FallbackModelVo>? FallbackModels { get; set; }
}
```

**在 Service 层组装 VO**：
```csharp
public async Task<AgentListVo> GetAgentListAsync(long userId, bool isAdmin)
{
    // 1. 并行查询多个单表数据
    var agentsTask = isAdmin
        ? _agentRepository.GetAllAsync()
        : _agentRepository.GetByUserIdAsync(userId);
    var agentTypesTask = _agentTypeRepository.GetEnabledAsync();
    var llmConfigsTask = _llmConfigRepository.GetAllAsync();

    await Task.WhenAll(agentsTask, agentTypesTask, llmConfigsTask);

    var agents = await agentsTask;
    var agentTypes = await agentTypesTask;
    var llmConfigs = await llmConfigsTask;

    // 2. 在内存中组装 VO
    var result = new AgentListVo
    {
        Agents = agents.Select(a => a.ToListItemVo(llmConfigs)).ToList(),
        AgentTypes = agentTypes.Select(at => at.ToVo()).ToList()
    };

    return result;
}
```

---

### 4. JWT 优化设计

#### 4.1 将角色和权限存储在 JWT 中

**登录时生成 JWT**：
```csharp
public async Task<LoginResponse> LoginAsync(LoginRequest request)
{
    // 1. 验证用户
    var user = await _userRepository.GetByUsernameAsync(request.Username);
    if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
    {
        throw new UnauthorizedAccessException("用户名或密码错误");
    }

    // 2. 查询用户角色
    var roles = await _roleRepository.GetUserRolesAsync(user.Id);

    // 3. 查询用户权限
    var permissions = await _permissionRepository.GetUserPermissionsAsync(user.Id);

    // 4. 生成 JWT（包含角色和权限）
    var token = _jwtService.GenerateToken(user, roles, permissions);

    return new LoginResponse
    {
        Token = token,
        User = user.ToVo(),
        Roles = roles,
        Permissions = permissions
    };
}
```

#### 4.2 从 JWT 读取角色和权限

**创建 ClaimsPrincipal 扩展方法**：
```csharp
namespace MAFStudio.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static long GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.Parse(userIdClaim!);
    }

    public static string GetUsername(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value ?? "";
    }

    public static List<string> GetRoles(this ClaimsPrincipal user)
    {
        var rolesClaim = user.FindFirst("roles")?.Value;
        return string.IsNullOrEmpty(rolesClaim) 
            ? new List<string>() 
            : rolesClaim.Split(',').ToList();
    }

    public static List<string> GetPermissions(this ClaimsPrincipal user)
    {
        var permissionsClaim = user.FindFirst("permissions")?.Value;
        return string.IsNullOrEmpty(permissionsClaim) 
            ? new List<string>() 
            : permissionsClaim.Split(',').ToList();
    }

    public static bool HasPermission(this ClaimsPrincipal user, string permission)
    {
        return user.GetPermissions().Contains(permission);
    }

    public static bool IsInAnyRole(this ClaimsPrincipal user, params string[] roles)
    {
        var userRoles = user.GetRoles();
        return roles.Any(r => userRoles.Contains(r));
    }
}
```

#### 4.3 在 Controller 中使用

```csharp
[HttpGet]
public async Task<ActionResult<AgentListVo>> GetAllAgents()
{
    // ✅ 从 JWT 读取用户信息，不查询数据库
    var userId = User.GetUserId();
    var isAdmin = User.IsInAnyRole("SUPER_ADMIN", "ADMIN");
    var hasPermission = User.HasPermission("agent:read");

    if (!hasPermission)
    {
        return Forbid();
    }

    var result = await _agentService.GetAgentListAsync(userId, isAdmin);
    return Ok(result);
}
```

---

## 三、实施计划

### 阶段一：权限管理基础（1-2 天）
1. 创建数据库表（roles, permissions, user_roles, role_permissions）
2. 创建实体类（Role, Permission, UserRole, RolePermission）
3. 创建 Repository 接口和实现
4. 创建初始数据迁移脚本

### 阶段二：JWT 优化（1 天）
1. 修改 JwtService，将角色和权限存储在 JWT 中
2. 创建 ClaimsPrincipal 扩展方法
3. 修改登录逻辑，查询用户角色和权限
4. 测试 JWT Claims

### 阶段三：全局授权中间件（1-2 天）
1. 创建权限配置文件（permission_config.json）
2. 创建全局授权中间件
3. 注册中间件
4. 测试权限拦截

### 阶段四：DDD 架构优化（2-3 天）
1. 重构 Repository 层，移除数据组装逻辑
2. 重构 Service 层，添加数据组装逻辑
3. 优化查询性能，使用并行查询
4. 测试所有功能

### 阶段五：前端适配（1-2 天）
1. 修改前端登录逻辑，存储角色和权限
2. 添加前端权限控制（按钮、菜单）
3. 处理 401、403 错误，跳转登录页面
4. 测试前端权限控制

### 阶段六：单元测试（1-2 天）
1. 编写权限管理单元测试
2. 编写授权中间件单元测试
3. 编写 Service 层单元测试
4. 确保所有测试通过

---

## 四、技术要点总结

### 1. 权限管理
- 采用 RBAC 模型（用户-角色-权限）
- 权限粒度：资源+操作（如 agent:read）
- 支持角色继承和权限组合

### 2. 全局拦截
- 使用中间件实现全局拦截
- 配置化管理公开接口
- 统一处理 401、403 错误

### 3. DDD 架构
- Infrastructure 层：单表查询，不组装数据
- Application 层：组装数据，包含业务逻辑
- Api 层：请求转发，不包含业务逻辑

### 4. JWT 优化
- 将角色和权限存储在 JWT Claims 中
- 从 JWT 读取用户信息，不查询数据库
- 只在需要详细权限信息时才查询数据库

### 5. 性能优化
- 使用并行查询提升性能
- 使用 VO 组装数据，减少数据传输
- 使用缓存优化权限查询

---

## 五、注意事项

1. **安全性**：
   - JWT 密钥必须足够复杂
   - 权限配置文件必须加密存储
   - 敏感操作必须二次验证

2. **性能**：
   - 避免在 Repository 层组装数据
   - 使用并行查询提升性能
   - 合理使用缓存

3. **可维护性**：
   - 权限配置必须文档化
   - 代码必须有注释
   - 单元测试必须覆盖

4. **扩展性**：
   - 权限系统必须支持扩展
   - 支持动态添加权限
   - 支持权限组合和继承

---

## 六、参考资料

1. [ASP.NET Core 授权文档](https://docs.microsoft.com/zh-cn/aspnet/core/security/authorization/introduction)
2. [JWT 最佳实践](https://datatracker.ietf.org/doc/html/rfc8725)
3. [DDD 分层架构](https://www.domainlanguage.com/)
4. [RBAC 权限模型](https://en.wikipedia.org/wiki/Role-based_access_control)
