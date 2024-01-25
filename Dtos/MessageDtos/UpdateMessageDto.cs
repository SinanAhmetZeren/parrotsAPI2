namespace ParrotsAPI2.Dtos.MessageDtos
{
    public class UpdateMessageDto
    {
        public int Id { get; set; }
        public bool Rendered { get; set; }
        public bool ReadByReceiver { get; set; }
    }
}
