using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ParrotsAPI2.Helpers;

namespace parrotsAPI2.Tests;

public class RateLimitAttributeTests
{
    private static ActionExecutingContext BuildContext(
        IServiceProvider services,
        string? deviceId = null,
        Dictionary<string, object?>? actionArgs = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services;
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("1.2.3.4");
        if (deviceId != null)
            httpContext.Request.Headers["X-Device-Id"] = deviceId;

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor { DisplayName = $"TestAction_{Guid.NewGuid()}" }
        );

        var filters = new List<IFilterMetadata>();
        return new ActionExecutingContext(
            actionContext,
            filters,
            actionArgs ?? new Dictionary<string, object?>(),
            controller: new object()
        );
    }

    private static IServiceProvider BuildServices(string environmentName)
    {
        var services = new ServiceCollection();
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns(environmentName);
        services.AddSingleton(envMock.Object);
        services.AddMemoryCache();
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    // --- Testing env bypass ---

    [Fact]
    public void Testing_Environment_DoesNotBlock()
    {
        var attr = new RateLimitAttribute(limit: 1, windowSeconds: 60);
        var services = BuildServices("Testing");
        var deviceId = Guid.NewGuid().ToString();

        // Even if we call it many times, Testing env should never set Result
        for (int i = 0; i < 5; i++)
        {
            var ctx = BuildContext(services, deviceId);
            attr.OnActionExecuting(ctx);
            Assert.Null(ctx.Result);
        }
    }

    // --- First request allowed ---

    [Fact]
    public void FirstRequest_IsAllowed()
    {
        var attr = new RateLimitAttribute(limit: 3, windowSeconds: 60);
        var services = BuildServices("Production");
        var ctx = BuildContext(services, Guid.NewGuid().ToString());

        attr.OnActionExecuting(ctx);

        Assert.Null(ctx.Result);
    }

    // --- Exceeds limit → 429 ---

    [Fact]
    public void ExceedingLimit_Returns429()
    {
        var attr = new RateLimitAttribute(limit: 3, windowSeconds: 60);
        var services = BuildServices("Production");
        var deviceId = Guid.NewGuid().ToString();

        // Each context must share the same action display name to hit the same cache key
        var actionName = $"TestAction_{Guid.NewGuid()}";
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services;
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("5.5.5.5");
        httpContext.Request.Headers["X-Device-Id"] = deviceId;

        ActionExecutingContext MakeCtx() => new ActionExecutingContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor { DisplayName = actionName }),
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: new object()
        );

        // First 3 allowed
        for (int i = 0; i < 3; i++)
        {
            var ctx = MakeCtx();
            attr.OnActionExecuting(ctx);
            Assert.Null(ctx.Result);
        }

        // 4th should be blocked
        var blockedCtx = MakeCtx();
        attr.OnActionExecuting(blockedCtx);

        var result = blockedCtx.Result as ContentResult;
        Assert.NotNull(result);
        Assert.Equal(429, result!.StatusCode);
    }

    // --- perUser: key includes email ---

    [Fact]
    public void PerUser_DifferentEmails_TrackedSeparately()
    {
        var attr = new RateLimitAttribute(limit: 2, windowSeconds: 60, perUser: true);
        var services = BuildServices("Production");

        var actionName = $"TestAction_{Guid.NewGuid()}";
        var ip = System.Net.IPAddress.Parse("6.6.6.6");

        ActionExecutingContext MakeCtxWithEmail(string email)
        {
            var hc = new DefaultHttpContext();
            hc.RequestServices = services;
            hc.Connection.RemoteIpAddress = ip;
            hc.Request.Headers["X-Device-Id"] = "shared-device";
            return new ActionExecutingContext(
                new ActionContext(hc, new RouteData(), new ActionDescriptor { DisplayName = actionName }),
                new List<IFilterMetadata>(),
                new Dictionary<string, object?> { ["dto"] = new EmailDto { Email = email } },
                controller: new object()
            );
        }

        // 2 requests for user A — both allowed
        for (int i = 0; i < 2; i++)
        {
            var ctx = MakeCtxWithEmail("a@test.com");
            attr.OnActionExecuting(ctx);
            Assert.Null(ctx.Result);
        }

        // 3rd for user A — blocked
        var blocked = MakeCtxWithEmail("a@test.com");
        attr.OnActionExecuting(blocked);
        Assert.NotNull(blocked.Result);

        // But user B's first request is still allowed
        var userB = MakeCtxWithEmail("b@test.com");
        attr.OnActionExecuting(userB);
        Assert.Null(userB.Result);
    }

    private class EmailDto
    {
        public string Email { get; set; } = string.Empty;
    }
}
