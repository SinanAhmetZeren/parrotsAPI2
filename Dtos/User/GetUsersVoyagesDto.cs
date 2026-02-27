namespace ParrotsAPI2.Dtos.User
{
    public class GetUsersVoyagesDto
    {
        public int Id { get; set; }
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
        public bool PublicOnMap { get; set; }
        public string ProfileImage { get; set; } = string.Empty;
        public int? VehicleId { get; set; }
        public string? VehicleImage { get; set; }
        public string? VehicleName { get; set; }
        public VehicleType VehicleType { get; set; }

    }
}
