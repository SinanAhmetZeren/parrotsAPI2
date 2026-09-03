using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Helpers;
using ParrotsAPI2.Services.Moderation;
using ParrotsAPI2.Services.Suspension;
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
        private readonly SuspendedUserCache _suspendedUserCache;

        public ModerationController(IModerationService moderationService, DataContext context, UserManager<AppUser> userManager, SuspendedUserCache suspendedUserCache)
        {
            _moderationService = moderationService;
            _context = context;
            _userManager = userManager;
            _suspendedUserCache = suspendedUserCache;
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
        public async Task<IActionResult> GetReports([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            if (!await IsAdmin()) return Forbid();

            var query = _context.UserReports.AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);
            if (from.HasValue)
                query = query.Where(r => r.CreatedAt >= from.Value.ToUniversalTime());
            if (to.HasValue)
                query = query.Where(r => r.CreatedAt <= to.Value.ToUniversalTime());

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
                .Select(u => new { u.Id, u.UserName, u.PublicId, u.LockoutEnabled, u.LockoutEnd })
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
                IsUserSuspended = r.ReportedUserId != null && users.TryGetValue(r.ReportedUserId, out var ru) && ru.LockoutEnabled && ru.LockoutEnd > DateTimeOffset.UtcNow,
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
            _suspendedUserCache.Add(userId);

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
            _suspendedUserCache.Remove(userId);

            return Ok(new { success = true });
        }

        [HttpGet("admin/deleted-accounts")]
        public async Task<IActionResult> GetDeletedAccounts()
        {
            if (!await IsAdmin()) return Forbid();

            var deletedUserIds = await _context.UserSuspensions
                .Where(s => s.Action == "deleted")
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync();

            var unsuspendedUserIds = await _context.UserSuspensions
                .Where(s => s.Action == "unsuspended" && deletedUserIds.Contains(s.UserId))
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync();

            var activelyDeletedIds = deletedUserIds.Except(unsuspendedUserIds).ToList();

            var suspensions = await _context.UserSuspensions
                .Where(s => s.Action == "deleted" && activelyDeletedIds.Contains(s.UserId))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var latestPerUser = suspensions
                .GroupBy(s => s.UserId)
                .Select(g => g.First())
                .ToList();

            var users = await _context.Users
                .Where(u => activelyDeletedIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName, u.PublicId })
                .ToDictionaryAsync(u => u.Id);

            var result = latestPerUser.Select(s => new
            {
                s.UserId,
                Username = users.GetValueOrDefault(s.UserId)?.UserName,
                PublicId = users.GetValueOrDefault(s.UserId)?.PublicId,
                s.CreatedAt,
            });

            return Ok(result);
        }

        [HttpGet("admin/direct-messages")]
        public async Task<IActionResult> GetDirectMessages(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? userId1,
            [FromQuery] string? userId2,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (!await IsAdmin()) return Forbid();

            var query = _context.Messages.AsQueryable();

            if (from.HasValue)
                query = query.Where(m => m.DateTime >= from.Value.ToUniversalTime());
            if (to.HasValue)
                query = query.Where(m => m.DateTime <= to.Value.ToUniversalTime());
            if (!string.IsNullOrEmpty(userId1) && !string.IsNullOrEmpty(userId2))
                query = query.Where(m =>
                    (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                    (m.SenderId == userId2 && m.ReceiverId == userId1));
            else if (!string.IsNullOrEmpty(userId1))
                query = query.Where(m => m.SenderId == userId1 || m.ReceiverId == userId1);
            else if (!string.IsNullOrEmpty(userId2))
                query = query.Where(m => m.SenderId == userId2 || m.ReceiverId == userId2);

            var total = await query.CountAsync();

            var messages = await query
                .OrderByDescending(m => m.DateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userIds = messages.SelectMany(m => new[] { m.SenderId, m.ReceiverId }).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName, u.EncryptionKey })
                .ToDictionaryAsync(u => u.Id);

            var result = messages.Select(m =>
            {
                string text;
                try
                {
                    var senderKey = users.GetValueOrDefault(m.SenderId)?.EncryptionKey;
                    var keyBytes = EncryptionHelper.KeyFromBase64(senderKey!);
                    text = EncryptionHelper.DecryptString(m.TextSenderEncrypted, keyBytes);
                }
                catch
                {
                    text = "[decryption failed]";
                }

                return new
                {
                    m.Id,
                    m.DateTime,
                    m.SenderId,
                    SenderUsername = users.GetValueOrDefault(m.SenderId)?.UserName,
                    m.ReceiverId,
                    ReceiverUsername = users.GetValueOrDefault(m.ReceiverId)?.UserName,
                    m.IsBlocked,
                    Text = text,
                };
            });

            return Ok(new { totalCount = total, items = result });
        }

        [HttpGet("admin/group-messages")]
        public async Task<IActionResult> GetAdminGroupMessages(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? groupId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (!await IsAdmin()) return Forbid();

            var query = _context.GroupMessages
                .Include(m => m.GroupConversation)
                .Include(m => m.Sender)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(m => m.DateTime >= from.Value.ToUniversalTime());
            if (to.HasValue)
                query = query.Where(m => m.DateTime <= to.Value.ToUniversalTime());
            if (groupId.HasValue)
                query = query.Where(m => m.GroupConversationId == groupId.Value);

            var total = await query.CountAsync();

            var messages = await query
                .OrderByDescending(m => m.DateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = messages.Select(m =>
            {
                string text;
                try
                {
                    var keyBytes = EncryptionHelper.KeyFromBase64(m.GroupConversation.EncryptionKey);
                    text = EncryptionHelper.DecryptString(m.Text, keyBytes);
                }
                catch
                {
                    text = "[decryption failed]";
                }

                return new
                {
                    m.Id,
                    m.DateTime,
                    m.GroupConversationId,
                    GroupName = m.GroupConversation.Name,
                    m.SenderId,
                    SenderUsername = m.Sender.UserName,
                    Text = text,
                };
            });

            return Ok(new { totalCount = total, items = result });
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
