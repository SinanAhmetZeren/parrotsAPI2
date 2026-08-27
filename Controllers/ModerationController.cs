using Microsoft.AspNetCore.Authorization;
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

        public ModerationController(IModerationService moderationService, DataContext context)
        {
            _moderationService = moderationService;
            _context = context;
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
    }

    public class ReportRequestDto
    {
        public string Reason { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}
