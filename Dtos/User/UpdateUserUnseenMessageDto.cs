namespace ParrotsAPI2.Dtos.User
{
    public class UpdateUserUnseenMessageDto
    {
        public string Id { get; set; } = string.Empty;
        public bool UnseenMessages { get; set; }

    }
}
