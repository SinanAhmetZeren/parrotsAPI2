

using ParrotsAPI2.Models;

namespace ParrotsAPI2.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Bio { get; set; }
        public string Email { get; set; }
        public string Instagram { get; set; }
        public string Facebook { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfileImageUrl { get; set; }
        public bool UnseenMessages { get; set; }
        public List<Vehicle>? Vehicles { get; set; }
        public List<Voyage>? Voyages { get; set; }
        public List<Bid>? Bids { get; set; }
        public List<Message> SentMessages { get; set; }
        public List<Message> ReceivedMessages { get; set; }


    }
}