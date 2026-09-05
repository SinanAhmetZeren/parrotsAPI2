namespace ParrotsAPI2.Models
{
    public class BlockedEmail
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty; // "self-suspended" or "suspended-by-admin"
        public string BlockedBy { get; set; } = string.Empty; // adminId or "self"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
