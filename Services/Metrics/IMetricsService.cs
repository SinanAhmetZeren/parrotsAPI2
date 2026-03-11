using Microsoft.AspNetCore.Mvc.ModelBinding;
using ParrotsAPI2.Dtos.FavoriteDtos;
using ParrotsAPI2.Dtos.MessageDtos;

namespace ParrotsAPI2.Services.Message
{
    public interface IMetricsService
    {
        Task<ServiceResponse<List<WeeklyPurchaseDto>>> GetWeeklyPurchases();
        Task<ServiceResponse<List<WeeklyTransactionsDto>>> GetWeeklyTransactions();
    }
}
