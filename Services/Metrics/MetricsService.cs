


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


        public async Task<ServiceResponse<List<WeeklyVoyagesDto>>> GetWeeklyVoyagesCreated()
        {
            var response = new ServiceResponse<List<WeeklyVoyagesDto>>();

            var data = await _context.Voyages
                .GroupBy(v => new
                {
                    Week = EF.Functions.DateDiffWeek(DateTime.MinValue, v.CreatedAt)
                })
                .Select(g => new WeeklyVoyagesDto
                {
                    WeekStart = g.Min(v => v.CreatedAt.Date),
                    VoyageCount = g.Count()
                })
                .OrderBy(x => x.WeekStart)
                .ToListAsync();

            response.Data = data;
            return response;
        }



        public async Task<ServiceResponse<List<WeeklyVehiclesDto>>> GetWeeklyVehiclesCreated()
        {
            var response = new ServiceResponse<List<WeeklyVehiclesDto>>();

            var data = await _context.Vehicles
                .GroupBy(v => new
                {
                    Week = EF.Functions.DateDiffWeek(DateTime.MinValue, v.CreatedAt)
                })
                .Select(g => new WeeklyVehiclesDto
                {
                    WeekStart = g.Min(v => v.CreatedAt.Date),
                    VehicleCount = g.Count()
                })
                .OrderBy(x => x.WeekStart)
                .ToListAsync();

            response.Data = data;
            return response;
        }

        public async Task<ServiceResponse<List<WeeklyUsersDto>>> GetWeeklyUsersCreated()
        {
            var response = new ServiceResponse<List<WeeklyUsersDto>>();

            var data = await _context.Users
                .GroupBy(u => new
                {
                    Week = EF.Functions.DateDiffWeek(DateTime.MinValue, u.CreatedAt)
                })
                .Select(g => new WeeklyUsersDto
                {
                    WeekStart = g.Min(v => v.CreatedAt.Date),
                    UserCount = g.Count()
                })
                .OrderBy(x => x.WeekStart)
                .ToListAsync();

            response.Data = data;
            return response;
        }

        public async Task<ServiceResponse<List<WeeklyBidsDto>>> GetWeeklyBids()
        {
            var response = new ServiceResponse<List<WeeklyBidsDto>>();

            // Single query to DB
            var data = await _context.Bids
                .Select(b => new
                {
                    CreatedWeek = EF.Functions.DateDiffWeek(DateTime.MinValue, b.DateTime),
                    AcceptedWeek = b.Accepted && b.AcceptedAt.HasValue
                        ? EF.Functions.DateDiffWeek(DateTime.MinValue, b.AcceptedAt.Value)
                        : (int?)null
                })
                .ToListAsync(); // Pull only weeks info

            // Group by all weeks (created or accepted)
            var weeklyGroups = data
                .SelectMany(d => d.AcceptedWeek.HasValue
                    ? new[] { (Week: d.CreatedWeek, Type: "Created"), (Week: d.AcceptedWeek.Value, Type: "Accepted") }
                    : new[] { (Week: d.CreatedWeek, Type: "Created") })
                .GroupBy(x => x.Week)
                .Select(g => new WeeklyBidsDto
                {
                    WeekStart = DateTime.MinValue.AddDays(g.Key * 7),
                    BidCount = g.Count(x => x.Type == "Created"),
                    AcceptBidCount = g.Count(x => x.Type == "Accepted")
                })
                .OrderBy(x => x.WeekStart)
                .ToList();

            response.Data = weeklyGroups;
            return response;
        }



        public async Task<ServiceResponse<List<WeeklyMessagesDto>>> GetWeeklyMessages()
        {
            var response = new ServiceResponse<List<WeeklyMessagesDto>>();

            var data = await _context.Messages
                .GroupBy(u => new
                {
                    Week = EF.Functions.DateDiffWeek(DateTime.MinValue, u.DateTime)
                })
                .Select(g => new WeeklyMessagesDto
                {
                    WeekStart = g.Min(v => v.DateTime.Date),
                    MessageCount = g.Count()
                })
                .OrderBy(x => x.WeekStart)
                .ToListAsync();

            response.Data = data;
            return response;
        }

    }
}