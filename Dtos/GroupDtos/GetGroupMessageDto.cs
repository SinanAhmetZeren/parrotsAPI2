namespace ParrotsAPI2.Dtos.GroupDtos
{
    public class GetGroupMessageDto
    {
        public int Id { get; set; }
        public int GroupConversationId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderUsername { get; set; } = string.Empty;
        public string SenderProfileUrl { get; set; } = string.Empty;
        public string SenderProfileThumbnailUrl { get; set; } = string.Empty;
        public string SenderPublicId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
    }
}
