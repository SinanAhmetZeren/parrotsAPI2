namespace ParrotsAPI2.Services.EmailSender;

public interface IEmailSender
{
    Task SendConfirmationEmail(
        string recipientEmail,
        string confirmationCode,
        string username);
}
