namespace ParrotsAPI2.Dtos.VehicleDtos
{
    public class AddVehicleDto
    {
        public string Name { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Type { get; set; }
        public int Capacity { get; set; }
        public string Description { get; set; }
        public int UserId { get; set; }
        public IFormFile ImageFile { get; set; }

        //public Models.User User { get; set; }
        public List<VehicleImage>? VehicleImages { get; set; }
        public List<Voyage>? Voyages { get; set; }
    }
}
