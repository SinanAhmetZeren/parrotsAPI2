using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

namespace ParrotsAPI2.Services.EmailSender;

public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;
    private readonly IConfiguration _config;

    public EmailSender(
        ILogger<EmailSender> logger,
        IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task SendReportDigestEmail(string recipientEmail, List<ReportDigestItem> reports)
    {
        try
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(
                    _config["Email:SmtpUser"],
                    _config["Email:SmtpPass"]
                ),
                EnableSsl = true
            };

            var rows = string.Join("\n\n", reports.Select(r =>
            {
                var targetLine = r.ReportedUserId != null
                    ? $"    Reported user   : {r.ReportedUsername} (ID: {r.ReportedUserId})"
                    : $"    Reported voyage : #{r.ReportedVoyageId}{(r.VoyageName != null ? $" — {r.VoyageName}" : "")}";
                return
                    $"  Report #{r.Id} — {r.CreatedAt:dd MMM yyyy HH:mm} UTC\n" +
                    $"    Reported by     : {r.ReporterUsername} (ID: {r.ReporterUserId})\n" +
                    targetLine + "\n" +
                    $"    Reason          : {r.Reason}";
            }));

            var body =
                $"Parrots Voyages — Daily Report Digest\n" +
                $"======================================\n\n" +
                $"{reports.Count} new report{(reports.Count != 1 ? "s" : "")} in the last 24 hours:\n\n" +
                rows +
                $"\n\nReview at https://parrotsvoyages.com/admin";

            var message = new MailMessage
            {
                From = new MailAddress(_config["Email:From"]),
                Subject = $"[Parrots] {reports.Count} new report{(reports.Count != 1 ? "s" : "")} — {DateTime.UtcNow:dd MMM yyyy}",
                Body = body,
                IsBodyHtml = false
            };

            message.To.Add(recipientEmail);
            await smtpClient.SendMailAsync(message);
            _logger.LogInformation("Report digest sent: {Count} reports", reports.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send report digest email");
        }
    }

    public async Task SendRegistrationEmail(
        string recipientEmail,
        string confirmationCode,
        string username)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            _logger.LogWarning(
                "Registration email skipped: empty recipient email. Username={Username}",
                username
            );
            return;
        }

        try
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(
                    _config["Email:SmtpUser"],
                    _config["Email:SmtpPass"]
                ),
                EnableSsl = true
            };

            string body =
                $"Welcome to Parrots, {username}!\n\n" +
                $"Your confirmation code is: {confirmationCode}\n\n" +
                $"Enter this code in the app to activate your account.";

            var message = new MailMessage
            {
                From = new MailAddress(_config["Email:From"]),
                Subject = "Welcome to Parrots — Confirm your account",
                Body = body,
                IsBodyHtml = false
            };

            message.To.Add(recipientEmail);
            await smtpClient.SendMailAsync(message);

            _logger.LogInformation(
                "Registration email sent successfully. Email={Email}, Username={Username}",
                recipientEmail,
                username
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send registration email. Email={Email}, Username={Username}",
                recipientEmail,
                username
            );
        }
    }

    public async Task SendConfirmationEmail( // HELPER FOR FORGOT PASSWORD
        string recipientEmail,
        string confirmationCode,
        string username)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            _logger.LogWarning(
                "Confirmation email skipped: empty recipient email. Username={Username}",
                username
            );
            return;
        }

        try
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(
                    _config["Email:SmtpUser"],
                    _config["Email:SmtpPass"]
                ),
                EnableSsl = true
            };

            string body =
                $"Hi {username}!\n" +
                $"Your reset password confirmation code is: {confirmationCode}";

            var message = new MailMessage
            {
                From = new MailAddress(_config["Email:From"]),
                Subject = "Parrots Confirmation Code",
                Body = body,
                IsBodyHtml = false
            };

            message.To.Add(recipientEmail);

            await smtpClient.SendMailAsync(message);

            _logger.LogInformation(
                "Confirmation email sent successfully. Email={Email}, Username={Username}",
                recipientEmail,
                username
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send confirmation email. Email={Email}, Username={Username}",
                recipientEmail,
                username
            );
        }
    }
}
