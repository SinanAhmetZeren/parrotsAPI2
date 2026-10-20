using Microsoft.EntityFrameworkCore;

namespace ParrotsAPI2.Models
{
    [Index(nameof(ReviewerId), nameof(SubjectId), IsUnique = true)]
    public class UserRating
    {
        public int Id { get; set; }
        public string ReviewerId { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
        public int Stars { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
