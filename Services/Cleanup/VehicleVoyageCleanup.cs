using System.Text;

namespace ParrotsAPI2.Services.Cleanup
{
    public class VehicleVoyageCleanupService : IHostedService, IDisposable
    {
        private Timer? _timer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VehicleVoyageCleanupService> _logger;

        private const double ThresholdTimeInMinutes = 60 * 24 * 2; // 48 hours in minutes
        private const double TimerIntervalInMinutes = 60 * 24 * 7; // 7 days in minutes


        public VehicleVoyageCleanupService(IServiceScopeFactory scopeFactory, ILogger<VehicleVoyageCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Vehicle Voyage Cleanup Service started.");
            _timer = new Timer(DoVehicleVoyageCleanup, null, TimeSpan.Zero, TimeSpan.FromMinutes(TimerIntervalInMinutes));
            return Task.CompletedTask;
        }


        private async void DoVehicleVoyageCleanup(object? state)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                var thresholdTime = DateTime.UtcNow.AddMinutes(-ThresholdTimeInMinutes);

                var outputText = new StringBuilder();
                outputText.AppendLine($"[Weekly Cleanup] Threshold Date: {thresholdTime}");

                // Handle Vehicles
                var vehiclesToDelete = await context.Vehicles
                    .Where(v => !v.Confirmed && v.CreatedAt <= thresholdTime)
                    .ToListAsync();

                if (vehiclesToDelete.Any())
                {
                    outputText.AppendLine($"Deleting {vehiclesToDelete.Count} unconfirmed vehicles.");
                    context.Vehicles.RemoveRange(vehiclesToDelete);
                }

                // Handle Voyages
                var voyagesToDelete = await context.Voyages
                    .Where(v => !v.Confirmed && v.CreatedAt <= thresholdTime)
                    .ToListAsync();

                if (voyagesToDelete.Any())
                {
                    outputText.AppendLine($"Deleting {voyagesToDelete.Count} unconfirmed voyages.");
                    context.Voyages.RemoveRange(voyagesToDelete);
                }

                // Save all changes in one single DB transaction to save costs
                if (vehiclesToDelete.Any() || voyagesToDelete.Any())
                {
                    await context.SaveChangesAsync();
                    outputText.AppendLine("Database updated successfully.");
                }

                _logger.LogInformation(outputText.ToString());
            }
            catch (Exception ex)
            {
                // Vital: Prevents the background service from crashing the whole API
                _logger.LogError(ex, "Error occurred during weekly VehicleVoyageCleanup.");
            }
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Vehicle Voyage Cleanup Service stopped.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
