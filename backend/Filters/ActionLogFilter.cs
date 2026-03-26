using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using System.Text.Json;
using MAFStudio.Backend.Data;

namespace MAFStudio.Backend.Filters;

/// <summary>
/// Action日志过滤器 - 类似 Java AOP 的 @Before/@After/@Around 注解
/// 记录方法调用前后的日志，包括执行时间、参数、返回值等
/// </summary>
public class ActionLogFilter : IAsyncActionFilter, IAsyncResultFilter
{
    private readonly ILogger<ActionLogFilter> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ActionLogFilter(
        ILogger<ActionLogFilter> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var httpContext = context.HttpContext;
        var controllerName = context.RouteData.Values["controller"]?.ToString();
        var actionName = context.RouteData.Values["action"]?.ToString();
        var userId = httpContext.User?.FindFirst("sub")?.Value ?? httpContext.User?.FindFirst("id")?.Value;
        var userName = httpContext.User?.FindFirst("name")?.Value ?? httpContext.User?.Identity?.Name;

        var parameters = context.ActionArguments.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.GetType().IsValueType == true || kvp.Value is string
                ? kvp.Value
                : kvp.Value != null ? JsonSerializer.Serialize(kvp.Value) : null
        );

        _logger.LogInformation(
            ">>> 请求开始 - Controller: {Controller}, Action: {Action}, 用户: {UserName}, 参数: {Params}",
            controllerName, actionName, userName ?? "匿名", JsonSerializer.Serialize(parameters));

        var executedContext = await next();

        stopwatch.Stop();

        if (executedContext.Exception == null)
        {
            _logger.LogInformation(
                "<<< 请求完成 - Controller: {Controller}, Action: {Action}, 耗时: {Elapsed}ms, 状态码: {StatusCode}",
                controllerName, actionName, stopwatch.ElapsedMilliseconds, httpContext.Response.StatusCode);

            if (stopwatch.ElapsedMilliseconds > 3000)
            {
                await SaveSlowQueryLogAsync(httpContext, controllerName, actionName, stopwatch.ElapsedMilliseconds, parameters, userId, userName);
            }
        }
        else
        {
            _logger.LogWarning(
                "<<< 请求异常 - Controller: {Controller}, Action: {Action}, 耗时: {Elapsed}ms, 异常: {Exception}",
                controllerName, actionName, stopwatch.ElapsedMilliseconds, executedContext.Exception.Message);
        }
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        await next();
    }

    private async Task SaveSlowQueryLogAsync(
        HttpContext httpContext,
        string? controllerName,
        string? actionName,
        long elapsedMs,
        Dictionary<string, object?> parameters,
        string? userId,
        string? userName)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var systemLog = new SystemLog
            {
                Id = Guid.NewGuid(),
                Level = "Warning",
                Category = "SlowQuery",
                Message = $"慢查询警告: {controllerName}.{actionName} 耗时 {elapsedMs}ms",
                RequestPath = httpContext.Request.Path,
                RequestMethod = httpContext.Request.Method,
                UserId = userId,
                UserName = userName,
                IpAddress = httpContext.Connection?.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow,
                ExtraData = JsonSerializer.Serialize(new
                {
                    Controller = controllerName,
                    Action = actionName,
                    ElapsedMilliseconds = elapsedMs,
                    Parameters = parameters,
                    QueryString = httpContext.Request.QueryString.ToString()
                })
            };

            dbContext.SystemLogs.Add(systemLog);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存慢查询日志失败");
        }
    }
}
