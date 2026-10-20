using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Models;
using System.Security.Claims;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RatingController : ControllerBase
    {
        private readonly DataContext _context;

        public RatingController(DataContext context)
        {
            _context = context;
        }

        [HttpPost("{subjectPublicId}")]
        public async Task<IActionResult> SubmitRating(string subjectPublicId, [FromBody] SubmitRatingDto dto)
        {
            if (dto.Stars < 1 || dto.Stars > 5)
                return BadRequest("Stars must be between 1 and 5.");

            var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (reviewerId == null) return Unauthorized();

            var subject = await _context.Users.FirstOrDefaultAsync(u => u.PublicId == subjectPublicId);
            if (subject == null) return NotFound("User not found.");

            if (subject.Id == reviewerId)
                return BadRequest("You cannot rate yourself.");

            var alreadyRated = await _context.UserRatings
                .AnyAsync(r => r.ReviewerId == reviewerId && r.SubjectId == subject.Id);
            if (alreadyRated)
                return BadRequest("You have already rated this user.");

            var hasCompletedVoyage = await _context.Bids
                .AnyAsync(b => b.UserId == reviewerId && b.Accepted &&
                    _context.Voyages.Any(v => v.Id == b.VoyageId && v.UserId == subject.Id && v.EndDate < DateTime.UtcNow));
            if (!hasCompletedVoyage)
                return BadRequest("You are not eligible to rate this user.");

            _context.UserRatings.Add(new UserRating
            {
                ReviewerId = reviewerId,
                SubjectId = subject.Id,
                Stars = dto.Stars,
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("{subjectPublicId}")]
        public async Task<IActionResult> GetRatings(string subjectPublicId)
        {
            var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var subject = await _context.Users.FirstOrDefaultAsync(u => u.PublicId == subjectPublicId);
            if (subject == null) return NotFound("User not found.");

            var ratings = await _context.UserRatings
                .Where(r => r.SubjectId == subject.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var isSubject = requesterId == subject.Id;

            var result = ratings.Select(r => new
            {
                r.Stars,
                r.CreatedAt,
                ReviewerName = isSubject
                    ? _context.Users.Where(u => u.Id == r.ReviewerId).Select(u => u.UserName).FirstOrDefault()
                    : null,
            });

            return Ok(new
            {
                averageRating = ratings.Count > 0 ? Math.Round(ratings.Average(r => r.Stars), 1) : (double?)null,
                ratingCount = ratings.Count,
                ratings = result,
            });
        }
    }

    public class SubmitRatingDto
    {
        public int Stars { get; set; }
    }
}
