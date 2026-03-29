using System.Security.Claims;
using System.Text.Json;

namespace MAFStudio.Api.Middleware;

/// <summary>
/// 全局授权中间件
/// </summary>
public class GlobalAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalAuthorizationMiddleware> _logger;
    private readonly PermissionConfig _permissionConfig;

    public GlobalAuthorizationMiddleware(
        RequestDelegate next,
        ILogger<GlobalAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        // 加载权限配置
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "permission_config.json");
        if (File.Exists(configPath))
        {
            var configJson = File.ReadAllText(configPath);
            _permissionConfig = JsonSerializer.Deserialize<PermissionConfig>(configJson) ?? new PermissionConfig();
        }
        else
        {
            _permissionConfig = new PermissionConfig();
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
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
            _logger.LogWarning($"未登录访问: {path}");
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
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
                _logger.LogWarning($"权限不足: {path}, 需要权限: {requiredPermission}, 用户权限: {string.Join(", ", userPermissions)}");
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { message = "权限不足" });
                return;
            }
        }

        await _next(context);
    }

    /// <summary>
    /// 检查是否允许匿名访问
    /// </summary>
    private bool IsAllowAnonymous(string path)
    {
        return _permissionConfig.AllowAnonymous.Any(p =>
            p.Equals(path, StringComparison.OrdinalIgnoreCase) ||
            (p.EndsWith("*") && path.StartsWith(p.TrimEnd('*'), StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// 获取所需权限
    /// </summary>
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

    /// <summary>
    /// 获取用户权限列表
    /// </summary>
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

/// <summary>
/// 权限配置
/// </summary>
public class PermissionConfig
{
    public List<string> AllowAnonymous { get; set; } = new();
    public List<string> RequireAuthentication { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> RequirePermission { get; set; } = new();
}
