
using Microsoft.AspNetCore.Identity;
using ParrotsAPI2.Models;

namespace ParrotsAPI2.Models
{
    public class AppUser : IdentityUser
    {   
        //public string UserName { get; set; }
        public string? Title { get; set; } 
        public string? Bio { get; set; }
        public string Email { get; set; }
        //public string Password { get; set; }
        public string? Instagram { get; set; }
        public string? Facebook { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Youtube { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? BackgroundImageUrl { get; set; }

        public bool UnseenMessages { get; set; } = false;
        public string? ConnectionId { get; set; }
        public List<Vehicle>? Vehicles { get; set; }
        public List<Voyage>? Voyages { get; set; }
        public List<Bid>? Bids { get; set; }
        public List<Message>? SentMessages { get; set; }
        public List<Message>? ReceivedMessages { get; set; }

    }
}