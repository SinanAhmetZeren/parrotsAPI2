using ParrotsAPI2.Dtos.VehicleImageDtos;

namespace ParrotsAPI2.Dtos.VehicleDtos
{
    public class VehicleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public VehicleType Type { get; set; }
        public int Capacity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;

    }
}
