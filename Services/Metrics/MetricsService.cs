using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Data;
using ParrotsAPI2.Dtos;
using ParrotsAPI2.Dtos.AiDtos;
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

            var data = (await _context.CrackerPurchases
                    .AsNoTracking()
                    .ToListAsync())
                .GroupBy(p => GetWeekStart(p.CreatedAt))
                .Select(g => new WeeklyPurchaseDto
                {
                    WeekStart = g.Key,
                    PurchaseCount = g.Count(),
                    TotalAmount = g.Sum(p => p.CrackersAmount)
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

            var data = (await _context.CrackerTransactions
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

        public async Task<ServiceResponse<List<AiQueryDayDto>>> GetAiQueryStats(DateTime? from, DateTime? to)
        {
            var response = new ServiceResponse<List<AiQueryDayDto>>();

            var query = _context.AskParrotsQueries.AsNoTracking().AsQueryable();

            if (from.HasValue)
                query = query.Where(q => q.CreatedAt >= DateTime.SpecifyKind(from.Value, DateTimeKind.Utc));
            if (to.HasValue)
                query = query.Where(q => q.CreatedAt <= DateTime.SpecifyKind(to.Value, DateTimeKind.Utc));

            var rows = await query
                .Select(q => new { q.CreatedAt, q.DurationMs, q.InputTokens, q.OutputTokens, q.Duration, q.PlannedSpotCount })
                .ToListAsync();

            var grouped = rows
                .GroupBy(q => q.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var durationBreakdown = g
                        .Where(q => !string.IsNullOrEmpty(q.Duration))
                        .GroupBy(q => q.Duration)
                        .Select(dg => new AiDailyDurationBreakdown
                        {
                            Duration = dg.Key,
                            AvgPlacesSuggested = dg.Average(q => q.PlannedSpotCount),
                        })
                        .ToList();

                    return new AiQueryDayDto
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        QueryCount = g.Count(),
                        AvgDurationMs = g.Average(q => q.DurationMs),
                        AvgInputTokens = g.Average(q => q.InputTokens),
                        AvgOutputTokens = g.Average(q => q.OutputTokens),
                        AvgTotalTokens = g.Average(q => q.InputTokens + q.OutputTokens),
                        DurationBreakdown = durationBreakdown,
                    };
                })
                .ToList();

            response.Data = grouped;
            return response;
        }

        public async Task<ServiceResponse<AskParrotsQueryPageDto>> GetAskParrotsQueries(
            int page, int pageSize,
            string? userId, string? vehicleType, string? duration,
            string? vibe, string? spotType,
            DateTime? from, DateTime? to, bool? isSuccess, string? model)
        {
            var response = new ServiceResponse<AskParrotsQueryPageDto>();

            var query = _context.AskParrotsQueries.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(q => q.UserId == userId);
            if (!string.IsNullOrWhiteSpace(vehicleType))
                query = query.Where(q => q.VehicleType == vehicleType);
            if (!string.IsNullOrWhiteSpace(duration))
                query = query.Where(q => q.Duration == duration);
            if (!string.IsNullOrWhiteSpace(vibe))
                query = query.Where(q => q.Vibe == vibe);
            if (!string.IsNullOrWhiteSpace(spotType))
                query = query.Where(q => q.SpotType == spotType);
            if (from.HasValue)
                query = query.Where(q => q.CreatedAt >= DateTime.SpecifyKind(from.Value, DateTimeKind.Utc));
            if (to.HasValue)
                query = query.Where(q => q.CreatedAt <= DateTime.SpecifyKind(to.Value, DateTimeKind.Utc));
            if (isSuccess.HasValue)
                query = query.Where(q => q.IsSuccess == isSuccess.Value);
            if (!string.IsNullOrWhiteSpace(model))
                query = query.Where(q => q.ModelUsed == model);

            var totalCount = await query.CountAsync();

            var rows = await query
                .OrderByDescending(q => q.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(q => new
                {
                    q.Id, q.UserId, q.UserQuery, q.Latitude, q.Longitude,
                    q.VehicleType, q.Duration, q.Vibe, q.SpotType,
                    q.ModelRequested, q.ModelUsed, q.IsSuccess, q.ErrorMessage,
                    q.DurationMs, q.InputTokens, q.OutputTokens, q.CachedInputTokens,
                    q.PlannedSpotCount, q.PlannedSpotsJson, q.PlacesApiAuditJson,
                    q.DraftNarrative, q.FinalSanitizedNarrative, q.CreatedAt
                })
                .ToListAsync();

            var items = rows.Select(q =>
            {
                int placesVerified = 0;
                if (!string.IsNullOrEmpty(q.PlacesApiAuditJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(q.PlacesApiAuditJson);
                        placesVerified = doc.RootElement.EnumerateArray()
                            .Count(e => e.TryGetProperty("placeId", out var pid) && pid.ValueKind != JsonValueKind.Null);
                    }
                    catch { }
                }
                return new AskParrotsQueryRecordDto
                {
                    Id = q.Id,
                    UserId = q.UserId,
                    UserQuery = q.UserQuery,
                    Latitude = q.Latitude,
                    Longitude = q.Longitude,
                    VehicleType = q.VehicleType,
                    Duration = q.Duration,
                    Vibe = q.Vibe,
                    SpotType = q.SpotType,
                    ModelRequested = q.ModelRequested,
                    ModelUsed = q.ModelUsed,
                    IsSuccess = q.IsSuccess,
                    ErrorMessage = q.ErrorMessage,
                    DurationMs = q.DurationMs,
                    InputTokens = q.InputTokens,
                    OutputTokens = q.OutputTokens,
                    CachedInputTokens = q.CachedInputTokens,
                    PlannedSpotCount = q.PlannedSpotCount,
                    PlacesVerified = placesVerified,
                    PlannedSpotsJson = q.PlannedSpotsJson,
                    PlacesApiAuditJson = q.PlacesApiAuditJson,
                    DraftNarrative = q.DraftNarrative,
                    FinalSanitizedNarrative = q.FinalSanitizedNarrative,
                    CreatedAt = q.CreatedAt
                };
            }).ToList();

            response.Data = new AskParrotsQueryPageDto
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
            return response;
        }
    }
}