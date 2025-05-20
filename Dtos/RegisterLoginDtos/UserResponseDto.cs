namespace ParrotsAPI2.Dtos.RegisterLoginDtos
{
    public class UserResponseDto
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;

    }
}
