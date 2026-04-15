namespace ParrotsAPI2.Dtos.VoyageDtos
{
    public class AddPlaceDto
    {
        public string Name { get; set; } = string.Empty;
        public string Brief { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool PublicOnMap { get; set; } = true;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int PlaceType { get; set; } = 1;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
