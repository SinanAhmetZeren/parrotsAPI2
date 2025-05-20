namespace ParrotsAPI2.Dtos.MessageDtos
{
    public class GetMessageDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public bool Rendered { get; set; }
        public bool ReadByReceiver { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderProfileUrl { get; set; } = string.Empty;
        public string SenderUsername { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string ReceiverProfileUrl { get; set; } = string.Empty;
        public string ReceiverUsername { get; set; } = string.Empty;


    }
}
