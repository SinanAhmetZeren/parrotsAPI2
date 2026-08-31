using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ParrotsAPI2.Services.Suspension;
using System.Security.Claims;

namespace ParrotsAPI2.Filters
{
    public class CheckUserSuspendedFilter : IAsyncActionFilter
    {
        private readonly SuspendedUserCache _cache;

        public CheckUserSuspendedFilter(SuspendedUserCache cache)
        {
            _cache = cache;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null && _cache.IsSuspended(userId))
            {
                context.Result = new ObjectResult(new { message = "Your account has been suspended." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            await next();
        }
    }
}
