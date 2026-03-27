using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using MAFStudio.Core.Interfaces.Services;

namespace MAFStudio.Api.Middleware;

/// <summary>
/// 全局调用记录中间件 - 类似Java AOP，记录所有API调用到数据库
/// </summary>
public class ApiCallLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiCallLoggingMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/health",
        "/api/llmconfigs/providers",
        "/favicon.ico"
    };

    private static readonly HashSet<string> ExcludedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "OPTIONS", "HEAD"
    };

    private const string ErrorMessagesKey = "ApiCallLogging_ErrorMessages";

    public ApiCallLoggingMiddleware(
        RequestDelegate next,
        ILogger<ApiCallLoggingMiddleware> logger,
        IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkipLogging(context))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var ipAddress = GetClientIpAddress(context);
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var requestPath = context.Request.Path.Value ?? "";
        var requestMethod = context.Request.Method;

        string? errorMessage = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            errorMessage = $"{ex.Message}\n{ex.StackTrace}";
            _logger.LogError(ex, "请求处理异常: {Method} {Path}", requestMethod, requestPath);
            throw;
        }
        finally
        {
            stopwatch.Stop();

            if (string.IsNullOrEmpty(errorMessage) && context.Response.StatusCode >= 400)
            {
                errorMessage = context.Items.TryGetValue(ErrorMessagesKey, out var errorMsg)
                    ? errorMsg?.ToString()
                    : $"HTTP {context.Response.StatusCode}";
            }

            try
            {
                await SaveOperationLogAsync(
                    userId,
                    requestMethod,
                    requestPath,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    ipAddress,
                    userAgent,
                    errorMessage
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存操作日志失败");
            }
        }
    }

    /// <summary>
    /// 设置错误信息（供其他中间件或过滤器调用）
    /// </summary>
    public static void SetErrorMessage(HttpContext context, string errorMessage)
    {
        context.Items[ErrorMessagesKey] = errorMessage;
    }

    private bool ShouldSkipLogging(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;

        if (ExcludedMethods.Contains(method))
            return true;

        if (ExcludedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return true;

        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
            return true;

        if (path.StartsWith("/api/agenttypes", StringComparison.OrdinalIgnoreCase) && method == "GET")
            return true;

        return false;
    }

    private string GetClientIpAddress(HttpContext context)
    {
        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ipAddress))
        {
            var ips = ipAddress.Split(',', StringSplitOptions.TrimEntries);
            if (ips.Length > 0)
                return ips[0];
        }

        ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(ipAddress))
            return ipAddress;

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private async Task SaveOperationLogAsync(
        string userId,
        string method,
        string path,
        int statusCode,
        long durationMs,
        string? ipAddress,
        string? userAgent,
        string? errorMessage)
    {
        using var scope = _serviceProvider.CreateScope();
        var logService = scope.ServiceProvider.GetRequiredService<IOperationLogService>();

        var resourceType = GetResourceType(path);
        var action = GetAction(method);
        var description = $"{action} {resourceType}";

        await logService.LogApiCallAsync(
            userId,
            action,
            resourceType,
            description,
            details: null,
            ipAddress,
            userAgent,
            path,
            method,
            statusCode,
            durationMs,
            errorMessage
        );
    }

    private static string GetResourceType(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 2 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            return segments[1].ToLowerInvariant() switch
            {
                "agents" => "智能体",
                "agenttypes" => "智能体类型",
                "collaborations" => "协作",
                "llmconfigs" => "LLM配置",
                "users" => "用户",
                "auth" => "认证",
                "operationlogs" => "操作日志",
                _ => segments[1]
            };
        }
        return "未知资源";
    }

    private static string GetAction(string method)
    {
        return method.ToUpperInvariant() switch
        {
            "GET" => "查询",
            "POST" => "创建",
            "PUT" => "更新",
            "PATCH" => "部分更新",
            "DELETE" => "删除",
            _ => method
        };
    }
}
