using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Dtos.AiDtos;
using ParrotsAPI2.Services.Ai;
using ParrotsAPI2.Services.User;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;
        private readonly IUserService _userService;
        private static readonly Dictionary<string, (int count, DateTime windowStart)> _rateLimitCache = new();
        private static readonly object _lock = new();
        private const int MaxRequestsPerHour = 20;

        public AiController(IAiService aiService, IUserService userService)
        {
            _aiService = aiService;
            _userService = userService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] AiQueryDto dto)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (!CheckRateLimit(userId))
                return StatusCode(429, new { message = "You've reached the limit of 5 requests per hour. Please try again later." });

            var deduct = await _userService.DeductCoinForAsk(userId);
            if (!deduct.Success)
                return StatusCode(402, new { message = deduct.Message });

            var result = await _aiService.AskAsync(dto);
            if (result == null)
                return StatusCode(500, new { message = "AI service unavailable. Please try again." });

            return Ok(new { response = result, remainingBalance = deduct.Data });
        }

        private static bool CheckRateLimit(string userId)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                if (_rateLimitCache.TryGetValue(userId, out var entry))
                {
                    if ((now - entry.windowStart).TotalHours >= 1)
                        _rateLimitCache[userId] = (1, now);
                    else if (entry.count >= MaxRequestsPerHour)
                        return false;
                    else
                        _rateLimitCache[userId] = (entry.count + 1, entry.windowStart);
                }
                else
                {
                    _rateLimitCache[userId] = (1, now);
                }
                return true;
            }
        }
    }
}
