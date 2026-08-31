using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Data;
using ParrotsAPI2.Services.EmailSender;

namespace ParrotsAPI2.Services.ReportDigest
{
    public class ReportDigestService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReportDigestService> _logger;
        private readonly IConfiguration _config;

        public ReportDigestService(IServiceScopeFactory scopeFactory, ILogger<ReportDigestService> logger, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = TimeUntilNextRun();
                _logger.LogInformation("Report digest next run in {Minutes} minutes", (int)delay.TotalMinutes);
                await Task.Delay(delay, stoppingToken);

                await SendDigestAsync();
            }
        }

        private async Task SendDigestAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                var since = DateTime.UtcNow.AddHours(-24);

                var reports = await context.UserReports
                    .Where(r => r.CreatedAt >= since)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                if (!reports.Any())
                {
                    _logger.LogInformation("Report digest: no new reports in last 24h, skipping email");
                    return;
                }

                var reporterIds = reports.Select(r => r.ReporterId).Distinct().ToList();
                var reportedUserIds = reports.Where(r => r.ReportedUserId != null).Select(r => r.ReportedUserId!).Distinct().ToList();
                var allUserIds = reporterIds.Union(reportedUserIds).ToList();

                var users = await context.Users
                    .Where(u => allUserIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.UserName })
                    .ToDictionaryAsync(u => u.Id);

                var voyageIds = reports.Where(r => r.ReportedVoyageId.HasValue).Select(r => r.ReportedVoyageId!.Value).Distinct().ToList();
                var voyageNames = voyageIds.Any()
                    ? await context.Voyages.Where(v => voyageIds.Contains(v.Id)).Select(v => new { v.Id, v.Name }).ToDictionaryAsync(v => v.Id, v => (string?)v.Name)
                    : new Dictionary<int, string?>();

                var items = reports.Select(r => new ReportDigestItem
                {
                    Id = r.Id,
                    ReporterUserId = r.ReporterId,
                    ReporterUsername = users.GetValueOrDefault(r.ReporterId)?.UserName ?? r.ReporterId,
                    ReportedUserId = r.ReportedUserId,
                    ReportedUsername = r.ReportedUserId != null ? users.GetValueOrDefault(r.ReportedUserId)?.UserName : null,
                    ReportedVoyageId = r.ReportedVoyageId,
                    VoyageName = r.ReportedVoyageId.HasValue ? voyageNames.GetValueOrDefault(r.ReportedVoyageId.Value) : null,
                    Reason = r.Reason,
                    CreatedAt = r.CreatedAt,
                }).ToList();

                var adminEmail = _config["Email:AdminDigestRecipient"] ?? _config["Email:From"]!;
                await emailSender.SendReportDigestEmail(adminEmail, items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running report digest");
            }
        }

        private static TimeSpan TimeUntilNextRun()
        {
            var now = DateTime.UtcNow;
            var next = now.Date.AddHours(8); // 08:00 UTC daily
            if (next <= now) next = next.AddDays(1);
            return next - now;
        }
    }
}
