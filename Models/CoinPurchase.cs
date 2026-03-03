using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParrotsAPI2.Models
{
    public class CoinPurchase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }


        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal UsdAmount { get; set; }  // e.g., 10.00

        [Required]
        public int CoinsAmount { get; set; }  // e.g., 100000

        [Required]
        [MaxLength(50)]
        public string? Status { get; set; } // "pending", "completed", "failed"

        [MaxLength(100)]
        public string? PaymentProviderId { get; set; } // Stripe / PayPal ID

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}