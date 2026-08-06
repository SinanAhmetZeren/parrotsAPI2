namespace ParrotsAPI2.Dtos.AiDtos
{
    public class AiQueryDto
    {
        public string VehicleType { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string Vibe { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string RadiusKm { get; set; } = string.Empty;
    }
}
