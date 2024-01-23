using ParrotsAPI2.Models;
using static System.Net.Mime.MediaTypeNames;

namespace ParrotsAPI2.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ProfileImage { get; set; }
        public string Type { get; set; }
        public int Capacity { get; set; }
        public string Description { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public List<VehicleImage>? VehicleImages { get; set; }
        public List<Voyage> Voyages { get; set; }
    }
}

