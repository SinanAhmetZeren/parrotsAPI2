using System.ComponentModel.DataAnnotations;

namespace ParrotsAPI2.Dtos.VehicleDtos
{
    public class AddVehicleDto
    {

        [MaxLength(20)]
        public string Name { get; set; } = string.Empty;
        public VehicleType Type { get; set; }
        public int Capacity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public IFormFile? ImageFile { get; set; }

    }
}
