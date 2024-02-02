using ParrotsAPI2.Dtos.BidDtos;
using ParrotsAPI2.Dtos.VoyageImageDtos;
using ParrotsAPI2.Dtos.WaypointDtos;

namespace ParrotsAPI2.Dtos.VoyageDtos
{
    public class GetVoyageDto
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
        public VehicleType VehicleType { get; set; }

        public List<WaypointDto> Waypoints { get; set; }
        public List<VoyageImageDto>? VoyageImages { get; set; }
        public string UserId { get; set; }
        public UserDto? User { get; set; }
        public int? VehicleId { get; set; }
        public VehicleDto? Vehicle { get; set; }
    }
}
