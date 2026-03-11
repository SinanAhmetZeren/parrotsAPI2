public class WeeklyTransactionsDto
{
    public DateTime WeekStart { get; set; }
    public string Type { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
}