namespace ParrotsAPI2.Dtos.User
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public string? ProfileImageThumbnailUrl { get; set; }
        public string? Title { get; set; }
        public string? Bio { get; set; }
    }
}
