public class ParrotCoinSummaryDto
{
    public int Balance { get; set; }
    public List<CoinPurchaseDto> Purchases { get; set; } = new();
}

