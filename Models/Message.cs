﻿using ParrotsAPI2.Models;
using System.Reflection;

namespace ParrotsAPI2.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime DateTime { get; set; }
        public bool Rendered { get; set; }
        public bool ReadByReceiver { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }


    }
}
