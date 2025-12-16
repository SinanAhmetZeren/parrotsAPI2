namespace ParrotsAPI2.Dtos.WaypointDtos
{
    public class AddWaypointDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IFormFile? ImageFile { get; set; }
        public int Order { get; set; }
        public int VoyageId { get; set; }
    }
}
