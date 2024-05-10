namespace ParrotsAPI2.Dtos.RegisterLoginDtos
{
    public class UpdatePasswordDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmationCode { get; set; }
        
    }
}
