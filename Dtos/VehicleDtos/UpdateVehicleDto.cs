namespace ParrotsAPI2.Dtos.VehicleDtos
{
    public class UpdateVehicleDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ProfileImageUrl { get; set; }
        public VehicleType Type { get; set; }
        public int Capacity { get; set; }
        public string Description { get; set; }


    }
}
