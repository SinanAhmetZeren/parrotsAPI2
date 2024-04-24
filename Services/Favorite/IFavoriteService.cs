using Microsoft.AspNetCore.Mvc.ModelBinding;
using ParrotsAPI2.Dtos.FavoriteDtos;
using ParrotsAPI2.Dtos.MessageDtos;

namespace ParrotsAPI2.Services.Message
{
    public interface IFavoriteService
    {
        Task<ServiceResponse<List<GetFavoriteDto>>> GetFavoritesByUserId(string userId);
        Task<ServiceResponse<List<GetVoyageDto>>> GetFavoriteVoyagesByUserId(string userId);
        Task<ServiceResponse<List<GetVehicleDto>>> GetFavoriteVehiclesByUserId(string userId);
        Task<ServiceResponse<List<int>>> GetFavoriteVehicleIdsByUserId(string userId);
        Task<ServiceResponse<List<int>>> GetFavoriteVoyageIdsByUserId(string userId);
        Task<ServiceResponse<GetFavoriteDto>> AddFavorite(AddFavoriteDto newFavorite);
        Task<ServiceResponse<string>> DeleteFavoriteVoyage(string userId, int voyageId);
        Task<ServiceResponse<string>> DeleteFavoriteVehicle(string userId, int vehicleId);

    }
}
