/*

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

namespace ParrotsAPI2.Helpers
{
    public class EmailSender
    {


        public async Task SendConfirmationEmail(string recipientEmail, string confirmationCode, string username)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("parrotsapp@gmail.com", "asfy dhdu mmjq kwss"),
                    EnableSsl = true,
                };
                string body = $"Welcome to Parrots {username}! \nYour confirmation code is: {confirmationCode}";
                var message = new MailMessage
                {
                    From = new MailAddress("parrotsapp@gmail.com"),
                    Subject = "Parrots Confirmation Code",
                    Body = body,
                    IsBodyHtml = false,
                };
                message.To.Add(recipientEmail);
                await smtpClient.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }
        }

    }
}



*/