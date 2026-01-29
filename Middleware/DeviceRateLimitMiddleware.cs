using System.Collections.Concurrent;

public class DeviceRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DeviceRateLimitMiddleware> _logger;

    private static readonly ConcurrentDictionary<string, RateLimitEntry> _store = new();

    private const int LIMIT = 100; // requests
    private static readonly TimeSpan WINDOW = TimeSpan.FromMinutes(1);
    //private static readonly TimeSpan WINDOW = TimeSpan.FromSeconds(1);

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

        /*
                // _logger.LogDebug(
                _logger.LogWarning(
                     "Request {Method} {Path} | DeviceId={DeviceId} | IP={IP}",
                    "Request {Method}  | DeviceId={DeviceId} | IP={IP}",
                    context.Request.Method,
                     context.Request.Path,
                    deviceId,
                    ip
                );
                */
        Console.WriteLine(
    $" --->> {context.Request.Method} | DeviceId={deviceId} | IP={ip}"
);



        var now = DateTime.UtcNow;

        var entry = _store.AddOrUpdate(
            deviceId,
            _ => new RateLimitEntry
            {
                Count = 1,
                WindowStart = now
            },
            (_, existing) =>
            {
                if (now - existing.WindowStart >= WINDOW)
                {
                    return new RateLimitEntry
                    {
                        Count = 1,
                        WindowStart = now
                    };
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

    private class RateLimitEntry
    {
        public int Count;
        public DateTime WindowStart;
    }
}
