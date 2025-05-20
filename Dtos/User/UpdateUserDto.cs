namespace ParrotsAPI2.Dtos.User
{
    public class UpdateUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Tiktok { get; set; } = string.Empty;
        public string Twitter { get; set; } = string.Empty;
        public string Linkedin { get; set; } = string.Empty;
        public string Instagram { get; set; } = string.Empty;
        public string Facebook { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Youtube { get; set; } = string.Empty;
        public bool EmailVisible { get; set; }
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string BackgroundImageUrl { get; set; } = string.Empty;
        public IFormFile? ImageFile { get; set; }

    }
}
