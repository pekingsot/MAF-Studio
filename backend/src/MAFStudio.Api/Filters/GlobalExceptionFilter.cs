using Microsoft.AspNetCore.Mvc.Filters;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Api.Middleware;
using MAFStudio.Api.Extensions;

namespace MAFStudio.Api.Filters;

/// <summary>
/// 全局异常过滤器 - 捕获所有Controller异常并记录到系统日志
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(IServiceProvider serviceProvider, ILogger<GlobalExceptionFilter> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        long? userId = null;
        try
        {
            userId = context.HttpContext.User?.GetUserId();
        }
        catch
        {
            // 用户未登录
        }
        
        var requestPath = context.HttpContext.Request.Path.Value ?? "";
        var requestMethod = context.HttpContext.Request.Method;
        var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
        var actionName = context.RouteData.Values["action"]?.ToString() ?? "Unknown";

        var fullErrorMessage = $"{context.Exception.Message}";
        if (context.Exception.InnerException != null)
        {
            fullErrorMessage += $"\n内部异常: {context.Exception.InnerException.Message}";
        }

        _logger.LogError(context.Exception, 
            "Controller异常捕获 - Controller: {Controller}, Action: {Action}, 用户: {User}, 路径: {Path}, 消息: {Message}",
            controllerName,
            actionName,
            userId?.ToString() ?? "anonymous",
            requestPath,
            context.Exception.Message);

        // 同步等待日志保存完成
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var systemLogService = scope.ServiceProvider.GetRequiredService<ISystemLogService>();
            
            systemLogService.LogAsync(
                "Error",
                controllerName,
                context.Exception.Message,
                context.Exception.ToString(),
                context.Exception.StackTrace,
                userId,
                requestPath,
                requestMethod,
                $"{controllerName}.{actionName}"
            ).GetAwaiter().GetResult();
            
            _logger.LogInformation("系统日志已保存到数据库");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存系统日志失败");
        }

        ApiCallLoggingMiddleware.SetErrorMessage(context.HttpContext, 
            $"{fullErrorMessage}\nStackTrace: {context.Exception.StackTrace}");

        context.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(new
        {
            success = false,
            error = "服务器内部错误",
            message = context.Exception.Message,
            detail = context.Exception.InnerException?.Message,
            path = requestPath,
            timestamp = DateTime.UtcNow
        })
        {
            StatusCode = 500
        };

        context.ExceptionHandled = true;
    }
}
