// DTO for request
public class UserDepositCoinsDto
{
    public string UserId { get; set; } = default!;
    public int Coins { get; set; }
    public decimal UsdAmount { get; set; }
    public string? PaymentProviderId { get; set; }

}