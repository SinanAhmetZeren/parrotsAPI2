namespace ParrotsAPI2.Dtos.VoyageDtos
{
    public class VoyageUpdateDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
