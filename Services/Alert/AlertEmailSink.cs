using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using Serilog.Core;
using Serilog.Events;

namespace ParrotsAPI2.Services.Alert;

public class AlertEmailSink : ILogEventSink
{
    private readonly string _smtpUser;
    private readonly string _smtpPass;
    private readonly string _adminEmail;

    // Cooldown: max one alert per unique message prefix per 5 minutes
    private readonly ConcurrentDictionary<string, DateTime> _lastSent = new();
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(5);

    // Framework wrapper messages — not actionable on their own
    private static readonly string[] FrameworkNoisePrefixes =
    [
        "An exception occurred while iterating over the results of a query",
    ];

    // WRN spike detection
    private readonly Queue<DateTime> _recentWarnings = new();
    private const int WrnSpikeThreshold = 10;
    private static readonly TimeSpan WrnSpikeWindow = TimeSpan.FromMinutes(5);
    private DateTime _lastWrnSpikeSent = DateTime.MinValue;

    public AlertEmailSink(string smtpUser, string smtpPass, string adminEmail)
    {
        _smtpUser = smtpUser;
        _smtpPass = smtpPass;
        _adminEmail = adminEmail;
    }

    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage();

        // WRN spike detection
        if (logEvent.Level == LogEventLevel.Warning)
        {
            var now = DateTime.UtcNow;
            lock (_recentWarnings)
            {
                _recentWarnings.Enqueue(now);
                while (_recentWarnings.Count > 0 && now - _recentWarnings.Peek() > WrnSpikeWindow)
                    _recentWarnings.Dequeue();

                if (_recentWarnings.Count >= WrnSpikeThreshold && now - _lastWrnSpikeSent > Cooldown)
                {
                    _lastWrnSpikeSent = now;
                    _ = Task.Run(() => SendAsync("WRN SPIKE", $"{_recentWarnings.Count} warnings in the last 5 minutes — possible attack or overload.", ""));
                }
            }
            return;
        }

        if (logEvent.Level < LogEventLevel.Error) return;

        // Skip pure framework wrapper noise
        if (FrameworkNoisePrefixes.Any(p => message.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return;

        // Cooldown: deduplicate by first 80 chars of message
        var key = message.Length > 80 ? message[..80] : message;
        var nowErr = DateTime.UtcNow;
        if (_lastSent.TryGetValue(key, out var last) && nowErr - last < Cooldown)
            return;
        _lastSent[key] = nowErr;

        var level = logEvent.Level.ToString().ToUpper();
        var exception = logEvent.Exception != null
            ? $"\n\n{logEvent.Exception.GetType().Name}: {logEvent.Exception.Message}"
            : string.Empty;

        _ = Task.Run(() => SendAsync(level, message, exception));
    }

    private async Task SendAsync(string level, string message, string exception)
    {
        try
        {
            var smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_smtpUser, "Parrots API"),
                Subject = $"[{level}] {message[..Math.Min(message.Length, 80)]}",
                Body = $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\nLevel: {level}\n\nMessage:\n{message}{exception}",
                IsBodyHtml = false
            };
            mail.To.Add(_adminEmail);

            await smtp.SendMailAsync(mail);
        }
        catch
        {
            // Swallow — never let alert failures affect the app
        }
    }
}
