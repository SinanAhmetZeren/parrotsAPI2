﻿namespace ParrotsAPI2.Dtos.User
{
    public class AddUserDto
    {
        public string UserName { get; set; }
        public string Title { get; set; }
        public string Bio { get; set; }
        public string Email { get; set; }
        public string Instagram { get; set; }
        public string? Tiktok { get; set; }
        public string? Twitter { get; set; }
        public string? Linkedin { get; set; }
        public string Facebook { get; set; }
        public string PhoneNumber { get; set; }
        public string Youtube { get; set; }
        public string ProfileImageUrl { get; set; }
        public string BackgroundImageUrl { get; set; }
        
        public IFormFile ImageFile { get; set; }
        public bool UnseenMessages { get; set; }
        public List<Vehicle>? Vehicles { get; set; }
        public List<Voyage>? Voyages { get; set; }
        public List<Bid>? Bids { get; set; }
        public List<Message>? SentMessages { get; set; }
        public List<Message>? ReceivedMessages { get; set; }
    }
}
