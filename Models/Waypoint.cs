using ParrotsAPI2.Models;

namespace ParrotsAPI2.Models
{
    public class Waypoint
    {
        public int Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProfileImage { get; set; } = string.Empty;
        public int Order { get; set; }
        public int VoyageId { get; set; }
        public Voyage? Voyage { get; set; }
    }
}
