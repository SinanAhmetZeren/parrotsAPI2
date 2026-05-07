namespace ParrotsAPI2.Models
{
    public class GroupMember
    {
        public int Id { get; set; }
        public int GroupConversationId { get; set; }
        public string UserId { get; set; } = string.Empty;

        public GroupConversation GroupConversation { get; set; } = null!;
        public AppUser User { get; set; } = null!;
    }
}
