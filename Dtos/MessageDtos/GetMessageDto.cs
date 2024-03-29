﻿namespace ParrotsAPI2.Dtos.MessageDtos
{
    public class GetMessageDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime DateTime { get; set; }
        public bool Rendered { get; set; }
        public bool ReadByReceiver { get; set; }
        public string SenderId { get; set; }
        public string SenderProfileUrl { get; set; }
        public string SenderUsername { get; set; }
        public string ReceiverId { get; set; }
        public string ReceiverProfileUrl { get; set; }
        public string ReceiverUsername { get; set; }


    }
}
