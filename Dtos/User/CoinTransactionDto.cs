namespace ParrotsAPI2.Dtos.User
{
    public class CoinTransactionDto
    {
        public int Id { get; set; }
        public decimal UsdAmount { get; set; }
        public int CoinsAmount { get; set; }
        public string? Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}