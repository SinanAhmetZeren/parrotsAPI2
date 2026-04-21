using System.Collections.Concurrent;

public class DeviceRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DeviceRateLimitMiddleware> _logger;

    private static readonly ConcurrentDictionary<string, RateLimitEntry> _deviceStore = new();
    private static readonly ConcurrentDictionary<string, RateLimitEntry> _ipStore = new();

    private const int DEVICE_LIMIT = 100; // requests per minute per device
    private const int IP_LIMIT = 300;     // requests per minute per IP (covers shared NAT)
    private static readonly TimeSpan WINDOW = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan IDLE_EXPIRY = TimeSpan.FromMinutes(30);

    private static readonly Timer _cleanupTimer = new Timer(
        _ => Cleanup(),
        null,
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(10));

    public DeviceRateLimitMiddleware(RequestDelegate next, ILogger<DeviceRateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Ignore CORS preflight
        if (context.Request.Method == HttpMethods.Options)
        {
            await _next(context);
            return;
        }

        // Skip rate limiting in test environment
        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (env.EnvironmentName == "Testing")
        {
            await _next(context);
            return;
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var deviceId = context.Request.Headers["X-Device-Id"].FirstOrDefault() ?? ip;

        _logger.LogDebug("Request {Method} {Path} | DeviceId={DeviceId} | IP={IP}",
            context.Request.Method, context.Request.Path, deviceId, ip);

        var now = DateTime.UtcNow;

        var deviceEntry = Track(_deviceStore, deviceId, now);
        var ipEntry = Track(_ipStore, ip, now);

        if (deviceEntry.Count > DEVICE_LIMIT || ipEntry.Count > IP_LIMIT)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                "{\"error\":\"Too many requests\"}"
            );
            return;
        }

        await _next(context);
    }

    private static RateLimitEntry Track(ConcurrentDictionary<string, RateLimitEntry> store, string key, DateTime now)
    {
        return store.AddOrUpdate(
            key,
            _ => new RateLimitEntry { Count = 1, WindowStart = now, LastSeen = now },
            (_, existing) =>
            {
                existing.LastSeen = now;
                if (now - existing.WindowStart >= WINDOW)
                {
                    existing.Count = 1;
                    existing.WindowStart = now;
                    return existing;
                }
                existing.Count++;
                return existing;
            });
    }

    // Evict entries that haven't made a request in 30 minutes
    private static void Cleanup()
    {
        var cutoff = DateTime.UtcNow - IDLE_EXPIRY;
        foreach (var store in new[] { _deviceStore, _ipStore })
        {
            foreach (var key in store.Keys)
            {
                if (store.TryGetValue(key, out var entry) && entry.LastSeen < cutoff)
                    store.TryRemove(key, out _);
            }
        }
    }

    private class RateLimitEntry
    {
        public int Count;
        public DateTime WindowStart;
        public DateTime LastSeen;
    }
}
