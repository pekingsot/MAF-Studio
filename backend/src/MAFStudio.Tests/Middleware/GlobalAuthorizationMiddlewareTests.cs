using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using MAFStudio.Api.Middleware;
using System.Security.Claims;
using Xunit;

namespace MAFStudio.Tests.Middleware;

/// <summary>
/// 全局授权中间件测试
/// </summary>
public class GlobalAuthorizationMiddlewareTests
{
    private readonly Mock<ILogger<GlobalAuthorizationMiddleware>> _mockLogger;

    public GlobalAuthorizationMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<GlobalAuthorizationMiddleware>>();
    }

    [Fact]
    public async Task InvokeAsync_ShouldAllowAnonymousPath()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/auth/login";
        context.Request.Method = "POST";

        var wasNextCalled = false;
        Task Next(HttpContext ctx)
        {
            wasNextCalled = true;
            return Task.CompletedTask;
        }

        var middleware = new GlobalAuthorizationMiddleware(Next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(wasNextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn401WhenNotAuthenticated()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/agents";
        context.Request.Method = "GET";
        context.User = new ClaimsPrincipal(new ClaimsIdentity());

        var wasNextCalled = false;
        Task Next(HttpContext ctx)
        {
            wasNextCalled = true;
            return Task.CompletedTask;
        }

        var middleware = new GlobalAuthorizationMiddleware(Next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(wasNextCalled);
        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAllowAuthenticatedUserWithoutPermissionCheck()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/auth/me";
        context.Request.Method = "GET";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1000000000000001"),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim("roles", "USER"),
            new Claim("permissions", "agent:read")
        };
        var identity = new ClaimsIdentity(claims, "test");
        context.User = new ClaimsPrincipal(identity);

        var wasNextCalled = false;
        Task Next(HttpContext ctx)
        {
            wasNextCalled = true;
            return Task.CompletedTask;
        }

        var middleware = new GlobalAuthorizationMiddleware(Next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(wasNextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn403WhenPermissionDenied()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/agents";
        context.Request.Method = "DELETE";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1000000000000001"),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim("roles", "USER"),
            new Claim("permissions", "agent:read,agent:create")
        };
        var identity = new ClaimsIdentity(claims, "test");
        context.User = new ClaimsPrincipal(identity);

        var wasNextCalled = false;
        Task Next(HttpContext ctx)
        {
            wasNextCalled = true;
            return Task.CompletedTask;
        }

        var middleware = new GlobalAuthorizationMiddleware(Next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(wasNextCalled);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAllowAccessWhenPermissionGranted()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/agents";
        context.Request.Method = "GET";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1000000000000001"),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim("roles", "USER"),
            new Claim("permissions", "agent:read,agent:create")
        };
        var identity = new ClaimsIdentity(claims, "test");
        context.User = new ClaimsPrincipal(identity);

        var wasNextCalled = false;
        Task Next(HttpContext ctx)
        {
            wasNextCalled = true;
            return Task.CompletedTask;
        }

        var middleware = new GlobalAuthorizationMiddleware(Next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(wasNextCalled);
    }
}
