using System.Net;
using System.Text.Json;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Services;

namespace MAFStudio.Backend.Middleware;

/// <summary>
/// 全局异常处理中间件 - 类似 Java AOP 的切面机制
/// 拦截所有请求中的异常，记录到系统日志并返回统一格式的错误响应
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = context.TraceIdentifier;
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var userId = context.User?.FindFirst("sub")?.Value ?? context.User?.FindFirst("id")?.Value;
        var userName = context.User?.FindFirst("name")?.Value ?? context.User?.Identity?.Name;
        var ipAddress = context.Connection?.RemoteIpAddress?.ToString();

        _logger.LogError(exception, 
            "全局异常捕获 - 请求ID: {RequestId}, 路径: {Path}, 方法: {Method}, 用户: {UserName}", 
            requestId, requestPath, requestMethod, userName);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var systemLog = new SystemLog
            {
                Id = Guid.NewGuid(),
                Level = "Error",
                Category = "GlobalException",
                Message = $"全局异常: {exception.Message}",
                Exception = exception.ToString(),
                StackTrace = exception.StackTrace,
                RequestPath = requestPath,
                RequestMethod = requestMethod,
                UserId = userId,
                UserName = userName,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow,
                ExtraData = JsonSerializer.Serialize(new
                {
                    RequestId = requestId,
                    QueryString = context.Request.QueryString.ToString(),
                    ExceptionType = exception.GetType().Name,
                    InnerException = exception.InnerException?.Message
                })
            };

            dbContext.SystemLogs.Add(systemLog);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception logEx)
        {
            _logger.LogError(logEx, "保存异常日志到数据库失败");
        }

        var response = context.Response;
        response.ContentType = "application/json; charset=utf-8";

        var (statusCode, errorMessage) = exception switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "未授权访问"),
            ArgumentException argEx => (HttpStatusCode.BadRequest, argEx.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "请求的资源不存在"),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            TimeoutException => (HttpStatusCode.GatewayTimeout, "请求超时"),
            _ => (HttpStatusCode.InternalServerError, "服务器内部错误，请稍后重试")
        };

        response.StatusCode = (int)statusCode;

        var errorResponse = new
        {
            success = false,
            message = errorMessage,
            detail = exception.Message,
            requestId = requestId,
            timestamp = DateTime.UtcNow,
            path = requestPath
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
    }
}
