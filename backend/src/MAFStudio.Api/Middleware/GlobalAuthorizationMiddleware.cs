using System.Security.Claims;

namespace MAFStudio.Api.Middleware;

public class GlobalAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalAuthorizationMiddleware> _logger;

    private static readonly HashSet<string> AllowAnonymousPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register"
    };

    private static readonly string[] AllowAnonymousPrefixes = ["/swagger", "/health"];

    public GlobalAuthorizationMiddleware(RequestDelegate next, ILogger<GlobalAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        if (IsAllowAnonymous(path))
        {
            await _next(context);
            return;
        }

        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("未登录访问: {Path}", path);
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = "未登录，请先登录" });
            return;
        }

        await _next(context);
    }

    private static bool IsAllowAnonymous(string path)
    {
        if (AllowAnonymousPaths.Contains(path))
            return true;

        foreach (var prefix in AllowAnonymousPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
