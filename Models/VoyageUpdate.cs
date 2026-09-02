using System.ComponentModel.DataAnnotations;

namespace ParrotsAPI2.Models
{
    public class VoyageUpdate
    {
        public int Id { get; set; }
        public int VoyageId { get; set; }
        public Voyage? Voyage { get; set; }
        [MaxLength(500)]
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
