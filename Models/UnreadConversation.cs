namespace ParrotsAPI2.Models;

public class UnreadConversation
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ConversationKey { get; set; } = string.Empty; // DM: "userId1_userId2", Group: "group_{groupId}"
    public int Count { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
