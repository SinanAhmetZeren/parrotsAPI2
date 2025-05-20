namespace ParrotsAPI2.Dtos.VoyageImageDtos
{
    public class VoyageImageDto
    {
        public int Id { get; set; }
        public string VoyageImagePath { get; set; } = string.Empty;
        public int VoyageId { get; set; }
    }
}
