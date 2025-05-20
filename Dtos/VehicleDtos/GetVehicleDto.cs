using ParrotsAPI2.Dtos.VehicleImageDtos;

namespace ParrotsAPI2.Dtos.VehicleDtos
{
    public class GetVehicleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public VehicleType Type { get; set; }
        public int Capacity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public UserDto? User { get; set; }
        public List<VehicleImageDto>? VehicleImages { get; set; }
        public List<VoyageDto>? Voyages { get; set; }
    }
}
