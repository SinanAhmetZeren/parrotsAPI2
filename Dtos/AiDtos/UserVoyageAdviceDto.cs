namespace ParrotsAPI2.Dtos.AiDtos
{
    public class UserVoyageAdviceDto
    {
        public string Name { get; set; } = string.Empty;
        public string Brief { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Vacancy { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public bool IsAuction { get; set; }
        public bool IsFixedPrice { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string LastBidDate { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public int VehicleCapacity { get; set; }
        public List<VoyageAdviceWaypointDto> Waypoints { get; set; } = new();
        public List<string> Categories { get; set; } = new();
    }

    public class VoyageAdviceWaypointDto
    {
        public int Order { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
