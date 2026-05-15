using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace ParrotsAPI2.Services.Notifications;

public class ExpoPushService
{
    private readonly HttpClient _http;
    private readonly ILogger<ExpoPushService> _logger;
    private const string ExpoEndpoint = "https://exp.host/--/api/v2/push/send";

    public ExpoPushService(HttpClient http, ILogger<ExpoPushService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task SendBadgeNotificationAsync(string expoPushToken)
    {
        if (string.IsNullOrEmpty(expoPushToken)) return;

        _logger.LogInformation("[PUSH] Sending badge to token: {Token}", expoPushToken);

        var payload = new
        {
            to = expoPushToken,
            badge = 1,
            sound = (string?)null,
            body = "",
            data = new { silent = true }
        };

        try
        {
            var response = await _http.PostAsJsonAsync(ExpoEndpoint, payload);
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("[PUSH] Expo response {Status}: {Body}", response.StatusCode, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PUSH] Failed to send push notification");
        }
    }
}
