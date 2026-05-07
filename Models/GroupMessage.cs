namespace ParrotsAPI2.Models
{
    public class GroupMessage
    {
        public int Id { get; set; }
        public int GroupConversationId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }

        public GroupConversation GroupConversation { get; set; } = null!;
        public AppUser Sender { get; set; } = null!;
    }
}
