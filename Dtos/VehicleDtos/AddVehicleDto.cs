namespace ParrotsAPI2.Dtos.VehicleDtos
{
    public class AddVehicleDto
    {
        public string Name { get; set; }
        public VehicleType Type { get; set; }
        public int Capacity { get; set; }
        public string Description { get; set; }
        public string UserId { get; set; }
        public IFormFile ImageFile { get; set; }

    }
}
