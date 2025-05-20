using ParrotsAPI2.Models;
using static System.Net.Mime.MediaTypeNames;

namespace ParrotsAPI2.Models
{
    public class Voyage
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Brief { get; set; }
        public string Description { get; set; }
        public int Vacancy { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime LastBidDate { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public bool FixedPrice { get; set; }
        public bool Auction { get; set; }
        public string ProfileImage { get; set; }
        public List<Waypoint> Waypoints { get; set; }
        public List<VoyageImage>? VoyageImages { get; set; }
        public string UserId { get; set; }
        public AppUser? User { get; set; }
        public int? VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }
        public string? VehicleImage { get; set; }         
        public string? VehicleName { get; set; }
        public VehicleType VehicleType { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Confirmed { get; set; }
        public bool IsDeleted { get; set; }

    }
    

}
