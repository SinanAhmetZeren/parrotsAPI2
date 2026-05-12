using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParrotsAPI2.Data;

namespace ParrotsAPI2.Services;

public class HubDiagnosticsService : BackgroundService
{
    private readonly ILogger<HubDiagnosticsService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public HubDiagnosticsService(ILogger<HubDiagnosticsService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken);

            var connections = ChatHub.GetUserConnections();
            var unreadCache = ChatHub.GetUnreadCache();
            var userInfoCache = ChatHub.GetUserInfoCache();

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== _userConnections ===");
            foreach (var kvp in connections)
            {
                var user = await db.Users.FindAsync([kvp.Key], stoppingToken);
                sb.AppendLine($"  {user?.UserName ?? "unknown"} ({kvp.Key}): [{string.Join(", ", kvp.Value)}]");
            }

            sb.AppendLine("=== _unreadCache ===");
            foreach (var kvp in unreadCache)
            {
                var user = await db.Users.FindAsync([kvp.Key], stoppingToken);
                sb.AppendLine($"  {user?.UserName ?? "unknown"} ({kvp.Key}): {kvp.Value}");
            }

            sb.AppendLine("=== _userInfoCache ===");
            foreach (var (userId, userName) in userInfoCache)
            {
                sb.AppendLine($"  {userName} ({userId})");
            }

            _logger.LogInformation(sb.ToString());
        }
    }
}
