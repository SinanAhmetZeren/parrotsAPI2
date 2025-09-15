using Microsoft.AspNetCore.Mvc.ModelBinding;
using ParrotsAPI2.Dtos.VoyageImageDtos;

namespace ParrotsAPI2.Services.Voyage
{
    public interface IVoyageService
    {
        Task<ServiceResponse<List<GetVoyageDto>>> GetAllVoyages();
        Task<ServiceResponse<GetVoyageDto>> GetVoyageById(int id);
        Task<ServiceResponse<GetVoyageDto>> GetUnconfirmedVoyageById(int id);
        Task<ServiceResponse<List<GetVoyageDto>>> GetVoyagesByUserId(string userId);
        Task<ServiceResponse<List<GetVoyageDto>>> GetVoyagesByVehicleId(int vehicleId);
        Task<ServiceResponse<GetVoyageDto>> AddVoyage(AddVoyageDto newVoyage);
        Task<ServiceResponse<GetVoyageDto>> UpdateVoyage(UpdateVoyageDto updatedVoyage);
        Task<ServiceResponse<string>> DeleteVoyage(int id);
        Task<ServiceResponse<List<GetVoyageDto>>> CheckAndDeleteVoyage(int id);
        Task<ServiceResponse<GetVoyageDto>> PatchVoyage(int voyageId, JsonPatchDocument<UpdateVoyageDto> patchDoc, ModelStateDictionary modelState);
        Task<ServiceResponse<GetVoyageDto>> UpdateVoyageProfileImage(int voyageId, IFormFile imageFile);
        Task<ServiceResponse<string>> AddVoyageImage(int voyageId, IFormFile imageFile, string userId);
        Task<ServiceResponse<string>> DeleteVoyageImage(int voyageImageId);
        Task<ServiceResponse<List<GetVoyageDto>>> GetVoyagesByCoordinates(double lat1, double lat2, double lon1, double lon2);
        Task<ServiceResponse<List<int>>> GetVoyageIdsByCoordinates(double lat1, double lat2, double lon1, double lon2);
        Task<ServiceResponse<List<GetVoyageDto>>> GetFilteredVoyages(double? lat1, double? lat2, double? lon1, double? lon2, int? vacancy, VehicleType? vehicleType, DateTime? startDate, DateTime? endDate);
        Task<ServiceResponse<string>> ConfirmVoyage(int voyageId);
        Task<ServiceResponse<VoyageImageDto>> GetVoyageImageById(int voyageImageId);

    }
}
