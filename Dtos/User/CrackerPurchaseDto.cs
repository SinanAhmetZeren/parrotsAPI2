public class CrackerPurchaseDto
{
    public int Id { get; set; }
    public decimal EurAmount { get; set; }
    public int CrackersAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentProviderId { get; set; }
    public DateTime CreatedAt { get; set; }
}
