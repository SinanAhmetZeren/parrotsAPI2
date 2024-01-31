using ParrotsAPI2.Dtos.VehicleImageDtos;

namespace ParrotsAPI2.Dtos.User
{
    public class GetUsersVehiclesDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Type { get; set; }
        public int Capacity { get; set; }
        public string Description { get; set; }

        //public List<VehicleImageDto>? VehicleImages { get; set; }
    }
}
