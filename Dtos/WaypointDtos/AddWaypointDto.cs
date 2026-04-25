using System.ComponentModel.DataAnnotations;

namespace ParrotsAPI2.Dtos.WaypointDtos
{
    public class AddWaypointDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        [MaxLength(25)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(300)]
        public string Description { get; set; } = string.Empty;
        public IFormFile? ImageFile { get; set; }
        public int Order { get; set; }
        public int VoyageId { get; set; }
    }
}
