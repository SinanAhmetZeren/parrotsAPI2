using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ParrotsAPI2.Services.Voyage
{
    public interface IVoyageService
    {
        Task<ServiceResponse<List<GetVoyageDto>>> GetAllVoyages();
        Task<ServiceResponse<GetVoyageDto>> GetVoyageById(int id);
        Task<ServiceResponse<List<GetVoyageDto>>> GetVoyagesByUserId(int userId);
        Task<ServiceResponse<List<GetVoyageDto>>> GetVoyagesByVehicleId(int vehicleId);
        Task<ServiceResponse<List<GetVoyageDto>>> AddVoyage(AddVoyageDto newVoyage);
        Task<ServiceResponse<GetVoyageDto>> UpdateVoyage(UpdateVoyageDto updatedVoyage);
        Task<ServiceResponse<List<GetVoyageDto>>> DeleteVoyage(int id);
        Task<ServiceResponse<GetVoyageDto>> PatchVoyage(int vehicleId, JsonPatchDocument<UpdateVoyageDto> patchDoc, ModelStateDictionary modelState);
        Task<ServiceResponse<GetVoyageDto>> UpdateVoyageProfileImage(int vehicleId, IFormFile imageFile);
        Task<ServiceResponse<GetVoyageDto>> AddVoyageImage(int vehicleId, IFormFile imageFile);

    }
}
