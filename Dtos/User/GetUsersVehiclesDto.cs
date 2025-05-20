using ParrotsAPI2.Dtos.VehicleImageDtos;

namespace ParrotsAPI2.Dtos.User
{
    public class GetUsersVehiclesDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Description { get; set; } = string.Empty;

        //public List<VehicleImageDto>? VehicleImages { get; set; }
    }
}
