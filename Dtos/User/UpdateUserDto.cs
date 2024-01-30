namespace ParrotsAPI2.Dtos.User
{
    public class UpdateUserDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Bio { get; set; }
        public string Email { get; set; }
        public string Instagram { get; set; }
        public string Facebook { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfileImageUrl { get; set; }
        public IFormFile ImageFile { get; set; }

    }
}
