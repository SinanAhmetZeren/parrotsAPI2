namespace ParrotsAPI2.Models
{
    public class UserSuspension
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string AdminId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // "suspended-by-admin", "self-suspended", "unsuspended"
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
