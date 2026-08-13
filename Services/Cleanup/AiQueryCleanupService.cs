namespace ParrotsAPI2.Services.Cleanup
{
    public class AiQueryCleanupService : IHostedService, IDisposable
    {
        private Timer? _timer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AiQueryCleanupService> _logger;

        private static readonly TimeSpan Retention = TimeSpan.FromDays(180);
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        public AiQueryCleanupService(IServiceScopeFactory scopeFactory, ILogger<AiQueryCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("AiQuery Cleanup Service started.");
            _timer = new Timer(DoCleanup, null, Interval, Interval);
            return Task.CompletedTask;
        }

        private async void DoCleanup(object? state)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();

                var cutoff = DateTime.UtcNow - Retention;
                var deleted = await context.AiQueries
                    .Where(q => q.CreatedAt < cutoff)
                    .ExecuteDeleteAsync();

                if (deleted > 0)
                    _logger.LogInformation("AiQuery Cleanup: deleted {Count} records older than 180 days.", deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AiQuery cleanup.");
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
