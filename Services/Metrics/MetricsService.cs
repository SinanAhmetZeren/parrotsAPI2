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

        // Helper: compute start of week (Sunday)
        private static DateTime GetWeekStart(DateTime date)
        {
            int diff = (int)date.DayOfWeek;
            return date.Date.AddDays(-diff);
        }

        // Weekly Purchases
        public async Task<ServiceResponse<List<WeeklyPurchaseDto>>> GetWeeklyPurchases()
        {
            var response = new ServiceResponse<List<WeeklyPurchaseDto>>();

            var data = (await _context.CoinPurchases
                    .AsNoTracking()
                    .ToListAsync())
                .GroupBy(p => GetWeekStart(p.CreatedAt))
                .Select(g => new WeeklyPurchaseDto
                {
                    WeekStart = g.Key,
                    PurchaseCount = g.Count(),
                    TotalAmount = g.Sum(p => p.CoinsAmount)
                })
                .OrderBy(x => x.WeekStart)
                .ToList();

            response.Data = data;
            return response;
        }

        // Weekly Transactions
        public async Task<ServiceResponse<List<WeeklyTransactionsDto>>> GetWeeklyTransactions()
        {
            var response = new ServiceResponse<List<WeeklyTransactionsDto>>();

            var data = (await _context.CoinTransactions
                    .AsNoTracking()
                    .ToListAsync())
                .GroupBy(t => new
                {
                    t.Type,
                    WeekStart = GetWeekStart(t.CreatedAt)
                })
                .Select(g => new WeeklyTransactionsDto
                {
                    WeekStart = g.Key.WeekStart,
                    Type = g.Key.Type,
                    TransactionCount = g.Count(),
                    TotalAmount = g.Sum(t => t.Amount)
                })
                .OrderBy(x => x.WeekStart)
                .ThenBy(x => x.Type)
                .ToList();

            response.Data = data;
            return response;
        }

        // Weekly Voyages
        public async Task<ServiceResponse<List<WeeklyVoyagesDto>>> GetWeeklyVoyagesCreated()
        {
            var response = new ServiceResponse<List<WeeklyVoyagesDto>>();

            var data = (await _context.Voyages
                    .AsNoTracking()
                    .ToListAsync())
                .GroupBy(v => GetWeekStart(v.CreatedAt))
                .Select(g => new WeeklyVoyagesDto
                {
                    WeekStart = g.Key,
                    VoyageCount = g.Count()
                })
                .OrderBy(x => x.WeekStart)
                .ToList();

            response.Data = data;
            return response;
        }

        // Weekly Vehicles
        public async Task<ServiceResponse<List<WeeklyVehiclesDto>>> GetWeeklyVehiclesCreated()
        {
            var response = new ServiceResponse<List<WeeklyVehiclesDto>>();

            var data = (await _context.Vehicles
                    .AsNoTracking()
                    .ToListAsync())
                .GroupBy(v => GetWeekStart(v.CreatedAt))
                .Select(g => new WeeklyVehiclesDto
                {
                    WeekStart = g.Key,
                    VehicleCount = g.Count()
                })
                .OrderBy(x => x.WeekStart)
                .ToList();

            response.Data = data;
            return response;
        }

        // Weekly Users
        public async Task<ServiceResponse<List<WeeklyUsersDto>>> GetWeeklyUsersCreated()
        {
            var response = new ServiceResponse<List<WeeklyUsersDto>>();

            var data = (await _context.Users
                    .AsNoTracking()
                    .ToListAsync())
                .GroupBy(u => GetWeekStart(u.CreatedAt))
                .Select(g => new WeeklyUsersDto
                {
                    WeekStart = g.Key,
                    UserCount = g.Count()
                })
                .OrderBy(x => x.WeekStart)
                .ToList();

            response.Data = data;
            return response;
        }

        // Weekly Bids
        public async Task<ServiceResponse<List<WeeklyBidsDto>>> GetWeeklyBids()
        {
            var response = new ServiceResponse<List<WeeklyBidsDto>>();

            var createdResults = (await _context.Bids
                    .AsNoTracking()
                    .ToListAsync())
                .GroupBy(b => GetWeekStart(b.DateTime))
                .Select(g => new { Week = g.Key, Count = g.Count() })
                .ToList();

            var acceptedResults = (await _context.Bids
                    .AsNoTracking()
                    .Where(b => b.Accepted && b.AcceptedAt.HasValue)
                    .ToListAsync())
                .GroupBy(b => GetWeekStart(b.AcceptedAt!.Value))
                .Select(g => new { Week = g.Key, Count = g.Count() })
                .ToList();

            var allWeeks = createdResults.Select(x => x.Week)
                .Union(acceptedResults.Select(x => x.Week))
                .OrderBy(w => w)
                .ToList();

            response.Data = allWeeks.Select(w => new WeeklyBidsDto
            {
                WeekStart = w,
                BidCount = createdResults.FirstOrDefault(c => c.Week == w)?.Count ?? 0,
                AcceptBidCount = acceptedResults.FirstOrDefault(a => a.Week == w)?.Count ?? 0
            }).ToList();

            return response;
        }

        // Weekly Messages
        public async Task<ServiceResponse<List<WeeklyMessagesDto>>> GetWeeklyMessages()
        {
            var response = new ServiceResponse<List<WeeklyMessagesDto>>();

            var data = (await _context.Messages
                    .AsNoTracking()
                    .ToListAsync())
                .GroupBy(m => GetWeekStart(m.DateTime))
                .Select(g => new WeeklyMessagesDto
                {
                    WeekStart = g.Key,
                    MessageCount = g.Count()
                })
                .OrderBy(x => x.WeekStart)
                .ToList();

            response.Data = data;
            return response;
        }
    }
}