using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using MAFStudio.Api.Middleware;
using System.Security.Claims;
using Xunit;

namespace MAFStudio.Tests.Middleware;

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
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/auth/login";
        context.Request.Method = "POST";

        var wasNextCalled = false;
        Task Next(HttpContext ctx) { wasNextCalled = true; return Task.CompletedTask; }

        var middleware = new GlobalAuthorizationMiddleware(Next, _mockLogger.Object);
        await middleware.InvokeAsync(context);

        Assert.True(wasNextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAllowAnonymousRegister()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/auth/register";
        context.Request.Method = "POST";

        var wasNextCalled = false;
        Task Next(HttpContext ctx) { wasNextCalled = true; return Task.CompletedTask; }

        var middleware = new GlobalAuthorizationMiddleware(Next, _mockLogger.Object);
        await middleware.InvokeAsync(context);

        Assert.True(wasNextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAllowAnonymousSwagger()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/swagger/index.html";
        context.Request.Method = "GET";

        var wasNextCalled = false;
        Task Next(HttpContext ctx) { wasNextCalled = true; return Task.CompletedTask; }

        var middleware = new GlobalAuthorizationMiddleware(Next, _mockLogger.Object);
        await middleware.InvokeAsync(context);

        Assert.True(wasNextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn401WhenNotAuthenticated()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/agents";
        context.Request.Method = "GET";
        context.User = new ClaimsPrincipal(new ClaimsIdentity());

        var wasNextCalled = false;
        Task Next(HttpContext ctx) { wasNextCalled = true; return Task.CompletedTask; }

        var middleware = new GlobalAuthorizationMiddleware(Next, _mockLogger.Object);
        await middleware.InvokeAsync(context);

        Assert.False(wasNextCalled);
        Assert.Equal(401, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAllowAuthenticatedUserToAnyPath()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/agents";
        context.Request.Method = "DELETE";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1000000000000001"),
            new Claim(ClaimTypes.Name, "testuser"),
        };
        var identity = new ClaimsIdentity(claims, "test");
        context.User = new ClaimsPrincipal(identity);

        var wasNextCalled = false;
        Task Next(HttpContext ctx) { wasNextCalled = true; return Task.CompletedTask; }

        var middleware = new GlobalAuthorizationMiddleware(Next, _mockLogger.Object);
        await middleware.InvokeAsync(context);

        Assert.True(wasNextCalled);
    }
}
