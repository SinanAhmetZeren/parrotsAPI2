using Microsoft.EntityFrameworkCore;

namespace ParrotsAPI2.Services.Moderation
{
    public class ModerationService : IModerationService
    {
        private readonly DataContext _context;

        public ModerationService(DataContext context)
        {
            _context = context;
        }

        public async Task<ServiceResponse<string>> BlockUser(string blockerId, string blockedId)
        {
            var response = new ServiceResponse<string>();
            if (blockerId == blockedId)
            {
                response.Success = false;
                response.Message = "Cannot block yourself.";
                return response;
            }

            _context.BlockedUsers.Add(new Models.BlockedUser
            {
                BlockerId = blockerId,
                BlockedId = blockedId,
                Action = "blocked",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            response.Data = blockedId;
            return response;
        }

        public async Task<ServiceResponse<string>> UnblockUser(string blockerId, string blockedId)
        {
            var response = new ServiceResponse<string>();

            var latest = await _context.BlockedUsers
                .Where(b => b.BlockerId == blockerId && b.BlockedId == blockedId)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();

            if (latest == null || latest.Action == "unblocked")
            {
                response.Success = false;
                response.Message = "User is not blocked.";
                return response;
            }

            _context.BlockedUsers.Add(new Models.BlockedUser
            {
                BlockerId = blockerId,
                BlockedId = blockedId,
                Action = "unblocked",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            response.Data = blockedId;
            return response;
        }

        public async Task<bool> IsBlocked(string blockerId, string blockedId)
        {
            var latest = await _context.BlockedUsers
                .Where(b => b.BlockerId == blockerId && b.BlockedId == blockedId)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();

            return latest?.Action == "blocked";
        }

        public async Task<ServiceResponse<string>> ReportUser(string reporterId, string reportedUserId, string reason, string? details)
        {
            var response = new ServiceResponse<string>();
            _context.UserReports.Add(new Models.UserReport
            {
                ReporterId = reporterId,
                ReportedUserId = reportedUserId,
                Reason = reason,
                Details = details,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            response.Data = reportedUserId;
            return response;
        }

        public async Task<ServiceResponse<string>> ReportVoyage(string reporterId, int reportedVoyageId, string reason, string? details)
        {
            var response = new ServiceResponse<string>();
            _context.UserReports.Add(new Models.UserReport
            {
                ReporterId = reporterId,
                ReportedVoyageId = reportedVoyageId,
                Reason = reason,
                Details = details,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            response.Data = reportedVoyageId.ToString();
            return response;
        }
    }
}
