using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Services.Moderation;
using System.Security.Claims;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ModerationController : ControllerBase
    {
        private readonly IModerationService _moderationService;
        private readonly DataContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ModerationController(IModerationService moderationService, DataContext context, UserManager<AppUser> userManager)
        {
            _moderationService = moderationService;
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("block/{publicId}")]
        public async Task<ActionResult<ServiceResponse<string>>> BlockUser(string publicId)
        {
            var blockerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (blockerId == null) return Unauthorized();

            var target = await _context.Users.FirstOrDefaultAsync(u => u.PublicId == publicId);
            if (target == null) return NotFound();

            return Ok(await _moderationService.BlockUser(blockerId, target.Id));
        }

        [HttpPost("unblock/{publicId}")]
        public async Task<ActionResult<ServiceResponse<string>>> UnblockUser(string publicId)
        {
            var blockerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (blockerId == null) return Unauthorized();

            var target = await _context.Users.FirstOrDefaultAsync(u => u.PublicId == publicId);
            if (target == null) return NotFound();

            return Ok(await _moderationService.UnblockUser(blockerId, target.Id));
        }

        [HttpPost("report/user/{publicId}")]
        public async Task<ActionResult<ServiceResponse<string>>> ReportUser(string publicId, [FromBody] ReportRequestDto dto)
        {
            var reporterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (reporterId == null) return Unauthorized();

            var target = await _context.Users.FirstOrDefaultAsync(u => u.PublicId == publicId);
            if (target == null) return NotFound();

            return Ok(await _moderationService.ReportUser(reporterId, target.Id, dto.Reason, dto.Details));
        }

        [HttpPost("report/voyage/{reportedVoyageId}")]
        public async Task<ActionResult<ServiceResponse<string>>> ReportVoyage(int reportedVoyageId, [FromBody] ReportRequestDto dto)
        {
            var reporterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (reporterId == null) return Unauthorized();
            return Ok(await _moderationService.ReportVoyage(reporterId, reportedVoyageId, dto.Reason, dto.Details));
        }

        [HttpGet("isBlocked/{publicId}")]
        public async Task<ActionResult<ServiceResponse<bool>>> IsBlocked(string publicId)
        {
            var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (requesterId == null) return Unauthorized();

            var target = await _context.Users.FirstOrDefaultAsync(u => u.PublicId == publicId);
            if (target == null) return NotFound();

            var blocked = await _moderationService.IsBlocked(requesterId, target.Id);
            return Ok(new ServiceResponse<bool> { Data = blocked });
        }

        // ── ADMIN ENDPOINTS ──

        [HttpGet("admin/reports")]
        public async Task<IActionResult> GetReports([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (!await IsAdmin()) return Forbid();

            var query = _context.UserReports.AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            var total = await query.CountAsync();

            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var reporterIds = reports.Where(r => r.ReporterId != null).Select(r => r.ReporterId).Distinct().ToList();
            var reportedIds = reports.Where(r => r.ReportedUserId != null).Select(r => r.ReportedUserId!).Distinct().ToList();
            var allUserIds = reporterIds.Union(reportedIds).ToList();
            var users = await _context.Users
                .Where(u => allUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName, u.PublicId })
                .ToDictionaryAsync(u => u.Id);

            var voyageIds = reports.Where(r => r.ReportedVoyageId.HasValue).Select(r => r.ReportedVoyageId!.Value).Distinct().ToList();
            var voyageNames = voyageIds.Any()
                ? await _context.Voyages.Where(v => voyageIds.Contains(v.Id)).Select(v => new { v.Id, v.Name }).ToDictionaryAsync(v => v.Id, v => (string?)v.Name)
                : new Dictionary<int, string?>();

            var result = reports.Select(r => new
            {
                r.Id,
                r.Reason,
                r.Details,

                r.Status,
                r.CreatedAt,
                ReporterUsername = users.GetValueOrDefault(r.ReporterId)?.UserName,
                ReporterPublicId = users.GetValueOrDefault(r.ReporterId)?.PublicId,
                ReportedUsername = r.ReportedUserId != null ? users.GetValueOrDefault(r.ReportedUserId)?.UserName : null,
                ReportedPublicId = r.ReportedUserId != null ? users.GetValueOrDefault(r.ReportedUserId)?.PublicId : null,
                ReportedUserId = r.ReportedUserId,
                ReportedVoyageId = r.ReportedVoyageId,
                VoyageName = r.ReportedVoyageId.HasValue ? voyageNames.GetValueOrDefault(r.ReportedVoyageId.Value) : null,
            });

            return Ok(new { totalCount = total, items = result });
        }

        [HttpPost("admin/reports/{id}/review")]
        public async Task<IActionResult> MarkReviewed(int id)
        {
            if (!await IsAdmin()) return Forbid();

            var report = await _context.UserReports.FindAsync(id);
            if (report == null) return NotFound();

            report.Status = "reviewed";
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost("admin/suspend/{userId}")]
        public async Task<IActionResult> SuspendUser(string userId, [FromBody] SuspendRequestDto dto)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!await IsAdmin() || adminId == null) return Forbid();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            await _userManager.UpdateAsync(user);

            _context.UserSuspensions.Add(new UserSuspension
            {
                UserId = userId,
                AdminId = adminId,
                Action = "suspended",
                Reason = dto.Reason,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("admin/unsuspend/{userId}")]
        public async Task<IActionResult> UnsuspendUser(string userId)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!await IsAdmin() || adminId == null) return Forbid();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.LockoutEnd = null;
            await _userManager.UpdateAsync(user);

            _context.UserSuspensions.Add(new UserSuspension
            {
                UserId = userId,
                AdminId = adminId,
                Action = "unsuspended",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        private async Task<bool> IsAdmin()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return false;
            var user = await _context.Users.FindAsync(userId);
            return user?.IsAdmin == true;
        }
    }

    public class ReportRequestDto
    {
        public string Reason { get; set; } = string.Empty;
        public string? Details { get; set; }
    }

    public class SuspendRequestDto
    {
        public string? Reason { get; set; }
    }
}
