using ParrotsAPI2.Dtos.VehicleImageDtos;

namespace ParrotsAPI2.Dtos.VehicleDtos
{
    public class GetVehicleDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Type { get; set; }
        public int Capacity { get; set; }
        public string Description { get; set; }
        public string UserId { get; set; }
        public UserDto User { get; set; }
        public List<VehicleImageDto>? VehicleImages { get; set; }
        public List<VoyageDto>? Voyages { get; set; }
    }
}
