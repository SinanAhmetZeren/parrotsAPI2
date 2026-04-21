using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace parrotsAPI2.Tests;

public class DeviceRateLimitMiddlewareTests
{
    private static HttpContext BuildContext(IServiceProvider services, string? deviceId = null, string method = "GET", string? remoteIp = "1.2.3.4")
    {
        var context = new DefaultHttpContext();
        context.RequestServices = services;
        context.Request.Method = method;
        if (deviceId != null)
            context.Request.Headers["X-Device-Id"] = deviceId;
        if (remoteIp != null)
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remoteIp);
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static IServiceProvider BuildServices(string environmentName)
    {
        var services = new ServiceCollection();
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns(environmentName);
        services.AddSingleton(envMock.Object);
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static DeviceRateLimitMiddleware BuildMiddleware(RequestDelegate next) =>
        new DeviceRateLimitMiddleware(next, Mock.Of<ILogger<DeviceRateLimitMiddleware>>());

    // --- OPTIONS passthrough ---

    [Fact]
    public async Task Options_Request_PassesThrough()
    {
        var passed = false;
        var middleware = BuildMiddleware(_ => { passed = true; return Task.CompletedTask; });
        var services = BuildServices("Production");
        var context = BuildContext(services, method: "OPTIONS");

        await middleware.InvokeAsync(context);

        Assert.True(passed);
        Assert.NotEqual(429, context.Response.StatusCode);
    }

    // --- Testing env bypass ---

    [Fact]
    public async Task Testing_Environment_BypassesRateLimit()
    {
        var passed = false;
        var middleware = BuildMiddleware(_ => { passed = true; return Task.CompletedTask; });
        var services = BuildServices("Testing");
        var context = BuildContext(services, deviceId: Guid.NewGuid().ToString());

        await middleware.InvokeAsync(context);

        Assert.True(passed);
    }

    // --- Normal request passes ---

    [Fact]
    public async Task SingleRequest_BelowLimit_PassesThrough()
    {
        var passed = false;
        var middleware = BuildMiddleware(_ => { passed = true; return Task.CompletedTask; });
        var services = BuildServices("Production");
        var deviceId = Guid.NewGuid().ToString();
        var context = BuildContext(services, deviceId: deviceId);

        await middleware.InvokeAsync(context);

        Assert.True(passed);
        Assert.NotEqual(429, context.Response.StatusCode);
    }

    // --- Exceeds device limit → 429 ---

    [Fact]
    public async Task ExceedingDeviceLimit_Returns429()
    {
        var middleware = BuildMiddleware(_ => Task.CompletedTask);
        var services = BuildServices("Production");
        var deviceId = Guid.NewGuid().ToString();

        // Send 101 requests (limit is 100)
        HttpContext? lastContext = null;
        for (int i = 0; i <= 100; i++)
        {
            lastContext = BuildContext(services, deviceId: deviceId);
            await middleware.InvokeAsync(lastContext);
        }

        Assert.Equal(429, lastContext!.Response.StatusCode);
    }

    // --- IP fallback when no device header ---

    [Fact]
    public async Task NoDeviceHeader_UsesIpAsFallback_PassesThrough()
    {
        var passed = false;
        var middleware = BuildMiddleware(_ => { passed = true; return Task.CompletedTask; });
        var services = BuildServices("Production");

        // No deviceId header — falls back to IP
        var uniqueIp = $"10.{Random.Shared.Next(1, 254)}.{Random.Shared.Next(1, 254)}.{Random.Shared.Next(1, 254)}";
        var context = BuildContext(services, deviceId: null, remoteIp: uniqueIp);

        await middleware.InvokeAsync(context);

        Assert.True(passed);
    }
}
