using System.ComponentModel.DataAnnotations;
using ParrotsAPI2.Models;

namespace ParrotsAPI2.Models
{
    public class Waypoint
    {
        public int Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        [MaxLength(25)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(300)]
        public string Description { get; set; } = string.Empty;
        public string ProfileImage { get; set; } = string.Empty;
        public int Order { get; set; }
        public int VoyageId { get; set; }
        public Voyage? Voyage { get; set; }
        public string UserId { get; set; } = string.Empty;

    }
}
