namespace ParrotsAPI2.Dtos.VoyageDtos
{
    public class AddVoyageDto
    {
        public string Name { get; set; } = string.Empty;
        public string Brief { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Vacancy { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime LastBidDate { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public bool FixedPrice { get; set; }
        public bool Auction { get; set; }
        public IFormFile? ImageFile { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int? VehicleId { get; set; }
    }
}
