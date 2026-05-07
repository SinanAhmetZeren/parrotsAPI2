namespace ParrotsAPI2.Services.Cleanup
{
    public class GroupCleanupService : IHostedService, IDisposable
    {
        private Timer? _timer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GroupCleanupService> _logger;

        private static readonly TimeSpan Threshold = TimeSpan.FromHours(24);
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        public GroupCleanupService(IServiceScopeFactory scopeFactory, ILogger<GroupCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Group Cleanup Service started.");
            _timer = new Timer(DoCleanup, null, Interval, Interval);
            return Task.CompletedTask;
        }

        private async void DoCleanup(object? state)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();

                var cutoff = DateTime.UtcNow - Threshold;

                // Find groups older than 24h with only 1 member and no messages
                var abandonedGroupIds = await context.GroupConversations
                    .Where(g => g.CreatedAt < cutoff)
                    .Where(g => !context.GroupMessages.Any(m => m.GroupConversationId == g.Id))
                    .Where(g => context.GroupMembers.Count(m => m.GroupConversationId == g.Id) <= 1)
                    .Select(g => g.Id)
                    .ToListAsync();

                if (abandonedGroupIds.Any())
                {
                    var groups = await context.GroupConversations
                        .Where(g => abandonedGroupIds.Contains(g.Id))
                        .ToListAsync();

                    context.GroupConversations.RemoveRange(groups);
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Group Cleanup: deleted {Count} abandoned groups.", groups.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during group cleanup.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose() => _timer?.Dispose();
    }
}
