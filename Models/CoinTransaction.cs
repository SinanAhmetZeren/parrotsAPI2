using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParrotsAPI2.Models
{
    public class CoinTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; }

        [Required]
        public int Amount { get; set; }  // Positive for purchase, negative for voyage_cost

        [Required]
        [MaxLength(50)]
        public string? Type { get; set; }  // "purchase" or "voyage_cost"

        public int? VoyageId { get; set; }
        public string? Description { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}