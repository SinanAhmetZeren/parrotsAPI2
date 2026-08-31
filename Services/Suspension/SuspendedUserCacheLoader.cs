namespace ParrotsAPI2.Services.Suspension
{
    public class SuspendedUserCacheLoader : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SuspendedUserCache _cache;

        public SuspendedUserCacheLoader(IServiceScopeFactory scopeFactory, SuspendedUserCache cache)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();

            var suspendedIds = await context.Users
                .Where(u => u.LockoutEnabled && u.LockoutEnd > DateTimeOffset.UtcNow)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            _cache.Load(suspendedIds);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
