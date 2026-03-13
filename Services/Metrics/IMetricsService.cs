using Microsoft.AspNetCore.Mvc.ModelBinding;
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
    }
}
