using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParrotsAPI2.Models
{
    [Table("CoinPurchases")]
    public class CrackerPurchase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }


        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal EurAmount { get; set; }

        [Required]
        public int CrackersAmount { get; set; }

        [Required]
        [MaxLength(50)]
        public string? Status { get; set; }

        [MaxLength(100)]
        public string? PaymentProviderId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
