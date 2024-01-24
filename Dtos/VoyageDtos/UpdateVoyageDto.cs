namespace ParrotsAPI2.Dtos.VoyageDtos
{
    public class UpdateVoyageDto
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
        public int UserId { get; set; }
        public UserDto? User { get; set; }
        public int? VehicleId { get; set; }
        public VehicleDto? Vehicle { get; set; }
        public List<Bid>? Bids { get; set; }
    }
}
