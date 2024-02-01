using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ParrotsAPI2.Services.Voyage
{
    public interface IVoyageService
    {
        Task<ServiceResponse<List<GetVoyageDto>>> GetAllVoyages();
        Task<ServiceResponse<GetVoyageDto>> GetVoyageById(int id);
        Task<ServiceResponse<List<GetVoyageDto>>> GetVoyagesByUserId(string userId);
        Task<ServiceResponse<List<GetVoyageDto>>> GetVoyagesByVehicleId(int vehicleId);
        Task<ServiceResponse<GetVoyageDto>> AddVoyage(AddVoyageDto newVoyage);
        Task<ServiceResponse<GetVoyageDto>> UpdateVoyage(UpdateVoyageDto updatedVoyage);
        Task<ServiceResponse<List<GetVoyageDto>>> DeleteVoyage(int id);
        Task<ServiceResponse<GetVoyageDto>> PatchVoyage(int voyageId, JsonPatchDocument<UpdateVoyageDto> patchDoc, ModelStateDictionary modelState);
        Task<ServiceResponse<GetVoyageDto>> UpdateVoyageProfileImage(int voyageId, IFormFile imageFile);
        Task<ServiceResponse<GetVoyageDto>> AddVoyageImage(int voyageId, IFormFile imageFile);
        Task<ServiceResponse<List<GetVoyageDto>>> GetVoyagesByCoordinates(double lat1, double lat2, double lon1, double lon2);
        Task<ServiceResponse<List<int>>> GetVoyageIdsByCoordinates(double lat1, double lat2, double lon1, double lon2);

    }
}
