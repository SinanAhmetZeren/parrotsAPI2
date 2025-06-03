
using Microsoft.AspNetCore.Identity;
using ParrotsAPI2.Models;

namespace ParrotsAPI2.Models
{
    public class AppUser : IdentityUser
    {
        public string? Title { get; set; }
        public string? Bio { get; set; }
        public string? DisplayEmail { get; set; }
        public string? Instagram { get; set; }
        public string? Facebook { get; set; }
        public override string? PhoneNumber { get; set; }
        public string? Youtube { get; set; }
        public string? Tiktok { get; set; }
        public string? Twitter { get; set; }
        public string? Linkedin { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? BackgroundImageUrl { get; set; }
        public bool UnseenMessages { get; set; } = false;
        public bool EmailVisible { get; set; } = true;
        public string? ConnectionId { get; set; }
        public List<Vehicle>? Vehicles { get; set; }
        public List<Voyage>? Voyages { get; set; }
        public List<Bid>? Bids { get; set; }
        public List<Message>? SentMessages { get; set; }
        public List<Message>? ReceivedMessages { get; set; }
        public string? ConfirmationCode { get; set; }
        public bool Confirmed { get; set; } = false;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }  
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    }
}