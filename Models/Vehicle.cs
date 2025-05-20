using ParrotsAPI2.Models;
using static System.Net.Mime.MediaTypeNames;

namespace ParrotsAPI2.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public VehicleType Type { get; set; }
        public int Capacity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public AppUser? User { get; set; }
        public List<VehicleImage>? VehicleImages { get; set; }
        public List<Voyage>? Voyages { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Confirmed { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

    }
}

