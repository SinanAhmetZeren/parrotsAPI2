


using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Data;
using ParrotsAPI2.Dtos;
using ParrotsAPI2.Models;

namespace ParrotsAPI2.Services.Message
{
    public class MetricsService : IMetricsService
    {
        private readonly DataContext _context;

        public MetricsService(DataContext context)
        {
            _context = context;
        }

        public async Task<ServiceResponse<List<WeeklyPurchaseDto>>> GetWeeklyPurchases()
        {
            var response = new ServiceResponse<List<WeeklyPurchaseDto>>();

            var data = await _context.CoinPurchases
                .GroupBy(p => new
                {
                    p.CreatedAt.Year,
                    Week = EF.Functions.DateDiffWeek(DateTime.MinValue, p.CreatedAt)
                })
                .Select(g => new WeeklyPurchaseDto
                {
                    WeekStart = g.Min(p => p.CreatedAt.Date),
                    PurchaseCount = g.Count(),
                    TotalAmount = g.Sum(p => p.CoinsAmount)
                })
                .OrderBy(x => x.WeekStart)
                .ToListAsync();

            response.Data = data;
            return response;
        }



        public async Task<ServiceResponse<List<WeeklyTransactionsDto>>> GetWeeklyTransactions()
        {
            var response = new ServiceResponse<List<WeeklyTransactionsDto>>();

            // Group by week and transaction type
            var data = await _context.CoinTransactions
                .GroupBy(t => new
                {
                    t.Type, // e.g., "purchase", "bid", "refund", etc.
                    Week = EF.Functions.DateDiffWeek(DateTime.MinValue, t.CreatedAt)
                })
                .Select(g => new WeeklyTransactionsDto
                {
                    WeekStart = g.Min(t => t.CreatedAt.Date),
                    Type = g.Key.Type,
                    TransactionCount = g.Count(),
                    TotalAmount = g.Sum(t => t.Amount) // if applicable
                })
                .OrderBy(x => x.WeekStart)
                .ThenBy(x => x.Type)
                .ToListAsync();

            response.Data = data;
            return response;
        }
    }
}