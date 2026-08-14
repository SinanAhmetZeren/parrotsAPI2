public class ParrotCrackerSummaryDto
{
    public int Balance { get; set; }
    public List<CrackerPurchaseDto> Purchases { get; set; } = new();
    public List<CrackerTransactionDto> Transactions { get; set; } = new();
}
