namespace ParrotsAPI2.Dtos.MessageDtos
{
    public class AddMessageDto
    {
        public string Text { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
    }
}
