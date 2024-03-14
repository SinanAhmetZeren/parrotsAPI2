using Microsoft.AspNetCore.Mvc.ModelBinding;
using ParrotsAPI2.Dtos.VehicleDtos;

namespace ParrotsAPI2.Services.Vehicle
{
    public interface IVehicleService
    {
        Task<ServiceResponse<List<GetVehicleDto>>> GetAllVehicles();
        Task<ServiceResponse<GetVehicleDto>> GetVehicleById(int id);
        Task<ServiceResponse<List<GetVehicleDto>>> GetVehiclesByUserId(string userId);
        Task<ServiceResponse<GetVehicleDto>> AddVehicle(AddVehicleDto newVehicle);
        Task<ServiceResponse<GetVehicleDto>> UpdateVehicle(UpdateVehicleDto updatedVehicle);
        Task<ServiceResponse<string>> DeleteVehicle(int id);
        Task<ServiceResponse<GetVehicleDto>> PatchVehicle(int vehicleId, JsonPatchDocument<UpdateVehicleDto> patchDoc, ModelStateDictionary modelState);
        Task<ServiceResponse<GetVehicleDto>> UpdateVehicleProfileImage(int vehicleId, IFormFile imageFile);
        Task<ServiceResponse<string>> AddVehicleImage(int vehicleId, IFormFile imageFile);
        Task<ServiceResponse<string>> DeleteVehicleImage(int vehicleImageId);


    }
}
