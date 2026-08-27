namespace ParrotsAPI2.Models
{
    public class UserReport
    {
        public int Id { get; set; }
        public string ReporterId { get; set; } = string.Empty;
        public string? ReportedUserId { get; set; }
        public int? ReportedVoyageId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string Status { get; set; } = "pending"; // "pending" or "reviewed"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
