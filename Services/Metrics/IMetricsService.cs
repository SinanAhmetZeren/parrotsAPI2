using Microsoft.AspNetCore.Mvc.ModelBinding;
using ParrotsAPI2.Dtos.AiDtos;
using ParrotsAPI2.Dtos.FavoriteDtos;
using ParrotsAPI2.Dtos.MessageDtos;

namespace ParrotsAPI2.Services.Message
{
    public interface IMetricsService
    {
        Task<ServiceResponse<List<WeeklyPurchaseDto>>> GetWeeklyPurchases();
        Task<ServiceResponse<List<WeeklyTransactionsDto>>> GetWeeklyTransactions();
        Task<ServiceResponse<List<WeeklyVoyagesDto>>> GetWeeklyVoyagesCreated();
        Task<ServiceResponse<List<WeeklyVehiclesDto>>> GetWeeklyVehiclesCreated();
        Task<ServiceResponse<List<WeeklyUsersDto>>> GetWeeklyUsersCreated();
        Task<ServiceResponse<List<WeeklyBidsDto>>> GetWeeklyBids();
        Task<ServiceResponse<List<WeeklyMessagesDto>>> GetWeeklyMessages();
        Task<ServiceResponse<AiQueryPageDto>> GetAiQueries(
            int page, int pageSize,
            string? userId, string? vehicleType, string? duration,
            string? vibe, string? spotType,
            DateTime? from, DateTime? to, bool? isSuccess, string? model);
        Task<ServiceResponse<List<AiQueryDayDto>>> GetAiQueryStats(DateTime? from, DateTime? to);
    }
}
