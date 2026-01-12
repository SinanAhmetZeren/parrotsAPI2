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

    public async Task SendConfirmationEmail(
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
                $"Welcome to Parrots {username}!\n" +
                $"Your confirmation code is: {confirmationCode}";

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
