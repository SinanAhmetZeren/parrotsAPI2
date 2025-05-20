namespace ParrotsAPI2.Dtos.RegisterLoginDtos
{
    public class UpdatePasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmationCode { get; set; } = string.Empty;
        
    }
}
