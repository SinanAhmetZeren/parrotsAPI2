namespace ParrotsAPI2.Models
{
    public class UserBookmark
    {
        public int Id { get; set; }
        public string BookmarkerId { get; set; } = string.Empty;
        public string BookmarkedUserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
