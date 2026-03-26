using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using MAFStudio.Backend.Data;

namespace MAFStudio.Backend.Filters;

/// <summary>
/// 全局异常过滤器 - 类似 Java AOP 的 @Around 注解
/// 拦截 Controller 中所有方法的异常
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter, IAsyncExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IServiceProvider _serviceProvider;

    public GlobalExceptionFilter(
        ILogger<GlobalExceptionFilter> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public void OnException(ExceptionContext context)
    {
        LogExceptionAsync(context).GetAwaiter().GetResult();
        HandleException(context);
    }

    public Task OnExceptionAsync(ExceptionContext context)
    {
        return LogExceptionAsync(context).ContinueWith(_ => HandleException(context));
    }

    private async Task LogExceptionAsync(ExceptionContext context)
    {
        var exception = context.Exception;
        var httpContext = context.HttpContext;
        var actionName = context.ActionDescriptor.DisplayName;
        var controllerName = context.RouteData.Values["controller"]?.ToString();
        var actionMethodName = context.RouteData.Values["action"]?.ToString();

        var userId = httpContext.User?.FindFirst("sub")?.Value ?? httpContext.User?.FindFirst("id")?.Value;
        var userName = httpContext.User?.FindFirst("name")?.Value ?? httpContext.User?.Identity?.Name;
        var ipAddress = httpContext.Connection?.RemoteIpAddress?.ToString();

        _logger.LogError(exception,
            "Controller异常捕获 - Controller: {Controller}, Action: {Action}, 用户: {UserName}, 消息: {Message}",
            controllerName, actionMethodName, userName, exception.Message);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var systemLog = new SystemLog
            {
                Id = Guid.NewGuid(),
                Level = "Error",
                Category = $"Controller.{controllerName}.{actionMethodName}",
                Message = $"Controller异常: {exception.Message}",
                Exception = exception.ToString(),
                StackTrace = exception.StackTrace,
                RequestPath = httpContext.Request.Path,
                RequestMethod = httpContext.Request.Method,
                UserId = userId,
                UserName = userName,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow,
                ExtraData = JsonSerializer.Serialize(new
                {
                    RequestId = httpContext.TraceIdentifier,
                    Controller = controllerName,
                    Action = actionMethodName,
                    ActionDescriptor = actionName,
                    QueryString = httpContext.Request.QueryString.ToString(),
                    ExceptionType = exception.GetType().Name,
                    InnerException = exception.InnerException?.Message,
                    RequestBody = await GetRequestBodyAsync(httpContext)
                })
            };

            dbContext.SystemLogs.Add(systemLog);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception logEx)
        {
            _logger.LogError(logEx, "保存异常日志到数据库失败");
        }
    }

    private static async Task<string?> GetRequestBodyAsync(HttpContext context)
    {
        try
        {
            if (context.Request.ContentLength > 0 && context.Request.Body.CanSeek)
            {
                context.Request.Body.Position = 0;
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
                
                if (body.Length > 2000)
                {
                    body = body.Substring(0, 2000) + "...[truncated]";
                }
                return body;
            }
        }
        catch
        {
        }
        return null;
    }

    private void HandleException(ExceptionContext context)
    {
        var exception = context.Exception;
        var requestId = context.HttpContext.TraceIdentifier;

        var (statusCode, errorMessage) = exception switch
        {
            UnauthorizedAccessException => (401, "未授权访问"),
            ArgumentNullException argNullEx => (400, $"参数不能为空: {argNullEx.ParamName}"),
            ArgumentException argEx => (400, argEx.Message),
            KeyNotFoundException => (404, "请求的资源不存在"),
            InvalidOperationException => (400, exception.Message),
            TimeoutException => (504, "请求超时"),
            NotImplementedException => (501, "功能尚未实现"),
            _ => (500, "服务器内部错误")
        };

        var errorResponse = new
        {
            success = false,
            message = errorMessage,
            detail = exception.Message,
            requestId = requestId,
            timestamp = DateTime.UtcNow,
            path = context.HttpContext.Request.Path.ToString(),
            exceptionType = exception.GetType().Name
        };

        context.Result = new ObjectResult(errorResponse)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/json" }
        };

        context.ExceptionHandled = true;
    }
}
