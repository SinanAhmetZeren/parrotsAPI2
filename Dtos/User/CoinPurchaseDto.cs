namespace ParrotsAPI2.Dtos.User
{
    public class CoinPurchaseDto
    {
        public int Id { get; set; }
        public decimal EurAmount { get; set; }
        public int CoinsAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PaymentProviderId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}