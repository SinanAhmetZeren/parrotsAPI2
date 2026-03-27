using System.Collections.Concurrent;

public class DeviceRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DeviceRateLimitMiddleware> _logger;

    private static readonly ConcurrentDictionary<string, RateLimitEntry> _store = new();

    private const int LIMIT = 100; // requests
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

        var deviceId =
            context.Request.Headers["X-Device-Id"].FirstOrDefault()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogDebug("Request {Method} {Path} | DeviceId={DeviceId} | IP={IP}",
            context.Request.Method, context.Request.Path, deviceId, ip);

        var now = DateTime.UtcNow;

        var entry = _store.AddOrUpdate(
            deviceId,
            _ => new RateLimitEntry
            {
                Count = 1,
                WindowStart = now,
                LastSeen = now
            },
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

        if (entry.Count > LIMIT)
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

    // Evict entries that haven't made a request in 30 minutes
    private static void Cleanup()
    {
        var cutoff = DateTime.UtcNow - IDLE_EXPIRY;
        foreach (var key in _store.Keys)
        {
            if (_store.TryGetValue(key, out var entry) && entry.LastSeen < cutoff)
                _store.TryRemove(key, out _);
        }
    }

    private class RateLimitEntry
    {
        public int Count;
        public DateTime WindowStart;
        public DateTime LastSeen;
    }
}
