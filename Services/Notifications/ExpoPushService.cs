using System.Net.Http.Json;

namespace ParrotsAPI2.Services.Notifications;

public class ExpoPushService
{
    private readonly HttpClient _http;
    private const string ExpoEndpoint = "https://exp.host/--/api/v2/push/send";

    public ExpoPushService(HttpClient http)
    {
        _http = http;
    }

    public async Task SendBadgeNotificationAsync(string expoPushToken)
    {
        if (string.IsNullOrEmpty(expoPushToken)) return;

        var payload = new
        {
            to = expoPushToken,
            badge = 1,
            sound = (string?)null,
            body = (string?)null,
            data = new { silent = true }
        };

        try
        {
            await _http.PostAsJsonAsync(ExpoEndpoint, payload);
        }
        catch
        {
            // Fire and forget — never crash the hub
        }
    }
}
