namespace ParrotsAPI2.Services.Moderation
{
    public interface IModerationService
    {
        Task<ServiceResponse<string>> BlockUser(string blockerId, string blockedId);
        Task<ServiceResponse<string>> UnblockUser(string blockerId, string blockedId);
        Task<bool> IsBlocked(string blockerId, string blockedId);
        Task<ServiceResponse<string>> ReportUser(string reporterId, string reportedUserId, string reason, string? details);
        Task<ServiceResponse<string>> ReportVoyage(string reporterId, int reportedVoyageId, string reason, string? details);
    }
}
