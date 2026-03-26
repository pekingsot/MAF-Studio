using Microsoft.AspNetCore.Mvc.Filters;
using MAFStudio.Core.Interfaces.Services;

namespace MAFStudio.Api.Filters;

public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ISystemLogService _systemLogService;
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ISystemLogService systemLogService, ILogger<GlobalExceptionFilter> logger)
    {
        _systemLogService = systemLogService;
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var userId = context.HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var requestPath = context.HttpContext.Request.Path;
        
        _logger.LogError(context.Exception, "Controller异常捕获 - Controller: {Controller}, Action: {Action}, 用户: {User}, 消息: {Message}",
            context.RouteData.Values["controller"],
            context.RouteData.Values["action"],
            userId ?? "anonymous",
            context.Exception.Message);

        _ = _systemLogService.LogAsync(
            "Error",
            context.RouteData.Values["controller"]?.ToString() ?? "Unknown",
            context.Exception.Message,
            context.Exception.ToString(),
            context.Exception.StackTrace,
            userId,
            requestPath
        );

        context.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(new
        {
            error = "服务器内部错误",
            message = context.Exception.Message,
            detail = context.Exception.InnerException?.Message
        })
        {
            StatusCode = 500
        };
    }
}
