public class CoinPurchaseDto
{
    public int Id { get; set; }
    public decimal UsdAmount { get; set; }
    public int CoinsAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentProviderId { get; set; }
    public DateTime CreatedAt { get; set; }
}