namespace ParrotsAPI2.Dtos.MessageDtos
{
    public class GetMessageDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime DateTime { get; set; }
        public bool Rendered { get; set; }
        public bool ReadByReceiver { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
    }
}
