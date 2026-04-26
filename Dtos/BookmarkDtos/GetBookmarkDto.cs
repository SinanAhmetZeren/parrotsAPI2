namespace ParrotsAPI2.Dtos.BookmarkDtos
{
    public class GetBookmarkDto
    {
        public int Id { get; set; }
        public string BookmarkedUserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string ProfileImageThumbnailUrl { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
