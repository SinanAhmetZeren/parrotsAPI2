

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ParrotsAPI2.Helpers
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RateLimitAttribute : ActionFilterAttribute
    {
        private readonly int _limit;
        private readonly TimeSpan _window;
        private readonly bool _perUser;

        /// <summary>
        /// Sliding window rate limiting attribute
        /// </summary>
        /// <param name="limit">Maximum allowed requests in the window</param>
        /// <param name="windowSeconds">Time window in seconds</param>
        /// <param name="perUser">Whether to limit per user/email in addition to IP</param>
        public RateLimitAttribute(int limit = 5, int windowSeconds = 60, bool perUser = false)
        {
            _limit = limit;
            _window = TimeSpan.FromSeconds(windowSeconds);
            _perUser = perUser;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var memoryCache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RateLimitAttribute>>();

            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var deviceId = context.HttpContext.Request.Headers["X-Device-Id"].FirstOrDefault() ?? "unknown";

            string userIdentifier = null;

            if (_perUser)
            {
                foreach (var arg in context.ActionArguments.Values)
                {
                    var type = arg?.GetType();
                    if (type == null) continue;

                    var emailProp = type.GetProperty("Email");
                    if (emailProp != null)
                    {
                        var value = emailProp.GetValue(arg) as string;
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            userIdentifier = value.ToLower().Trim();
                            break;
                        }
                    }
                }
            }

            // ✅ Key now includes IP + DeviceId (+ user if enabled)
            string key = _perUser && userIdentifier != null
                ? $"{context.ActionDescriptor.DisplayName}-{ip}-{deviceId}-{userIdentifier}"
                : $"{context.ActionDescriptor.DisplayName}-{ip}-{deviceId}";

            var now = DateTime.UtcNow;

            if (!memoryCache.TryGetValue(key, out List<DateTime> timestamps))
            {
                memoryCache.Set(key, new List<DateTime> { now }, _window);
                return;
            }

            timestamps = timestamps.Where(t => t > now - _window).ToList();

            if (timestamps.Count >= _limit)
            {
                logger.LogWarning(
                    "Rate limit exceeded | IP={IP} | Device={DeviceId} | User={User} | Endpoint={Endpoint}",
                    ip,
                    deviceId,
                    userIdentifier ?? "N/A",
                    context.ActionDescriptor.DisplayName
                );

                context.Result = new ContentResult
                {
                    StatusCode = 429,
                    Content = "Too many requests. Try again later."
                };
                return;
            }

            timestamps.Add(now);
            memoryCache.Set(key, timestamps, _window);
        }


    }
}
