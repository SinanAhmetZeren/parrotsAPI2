namespace ParrotsAPI2.Services.EmailSender;

public interface IEmailSender
{
    Task SendConfirmationEmail(
        string recipientEmail,
        string confirmationCode,
        string username);

    Task SendReportDigestEmail(
        string recipientEmail,
        List<ReportDigestItem> reports);
}

public class ReportDigestItem
{
    public int Id { get; set; }
    public string ReporterUserId { get; set; } = string.Empty;
    public string ReporterUsername { get; set; } = string.Empty;
    public string? ReportedUserId { get; set; }
    public string? ReportedUsername { get; set; }
    public int? ReportedVoyageId { get; set; }
    public string? VoyageName { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
