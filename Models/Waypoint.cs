using ParrotsAPI2.Models;

namespace ParrotsAPI2.Models
{
    public class Waypoint
    {
        public int Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string? Image { get; set; }
        public int Order { get; set; }
        public int VoyageId { get; set; }
        public Voyage Voyage { get; set; }
    }
}
