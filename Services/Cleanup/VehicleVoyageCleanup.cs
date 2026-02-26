using System.Text;

namespace ParrotsAPI2.Services.Cleanup
{
    public class VehicleVoyageCleanupService : IHostedService, IDisposable
    {
        private Timer? _timer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VehicleVoyageCleanupService> _logger;

        private const double ThresholdTimeInMinutes = 48 * 60;
        private const double TimerIntervalInMinutes = 60 * 24 * 7;


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
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var thresholdTime = DateTime.UtcNow.AddMinutes(-TimerIntervalInMinutes);
            var outputText = new StringBuilder();
            outputText.AppendLine($"Checking for vehicles older than {thresholdTime} and not confirmed.");
            var vehiclesToDelete = await context.Vehicles
                .Where(v => !v.Confirmed && v.CreatedAt <= thresholdTime)
                .ToListAsync();

            if (vehiclesToDelete.Any())
            {
                outputText.AppendLine($"Found {vehiclesToDelete.Count} unconfirmed vehicles older than {thresholdTime} hours. Deleting...");
                foreach (var vehicle in vehiclesToDelete)
                {
                    outputText.AppendLine($"Deleting Vehicle ID: {vehicle.Id}, Created At: {vehicle.CreatedAt}");
                }
                context.Vehicles.RemoveRange(vehiclesToDelete);
                await context.SaveChangesAsync();
                outputText.AppendLine("Vehicle cleanup completed successfully.");
            }
            else
            {
                outputText.AppendLine("No vehicles found to delete.");
            }
            outputText.AppendLine("-------");
            var voyagesToDelete = await context.Voyages
                .Where(v => !v.Confirmed && v.CreatedAt <= thresholdTime)
                .ToListAsync();

            if (voyagesToDelete.Any())
            {
                outputText.AppendLine($"Found {voyagesToDelete.Count} unconfirmed voyages older than {thresholdTime} hours. Deleting...");
                foreach (var voyage in voyagesToDelete)
                {
                    outputText.AppendLine($"Deleting Voyage ID: {voyage.Id}, Created At: {voyage.CreatedAt}");
                }
                context.Voyages.RemoveRange(voyagesToDelete);
                await context.SaveChangesAsync();
                outputText.AppendLine("Voyage cleanup completed successfully.");
            }
            else
            {
                outputText.AppendLine("No voyages found to delete.");
            }
            outputText.AppendLine("-------");
            _logger.LogInformation(outputText.ToString());
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
