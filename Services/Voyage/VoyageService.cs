using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ParrotsAPI2.Services.Voyage
{
    public class VoyageService : IVoyageService
    {
        public Task<ServiceResponse<List<GetVoyageDto>>> AddVoyage(AddVoyageDto newVoyage)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResponse<GetVoyageDto>> AddVoyageImage(int vehicleId, IFormFile imageFile)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResponse<List<GetVoyageDto>>> DeleteVoyage(int id)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResponse<List<GetVoyageDto>>> GetAllVoyages()
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResponse<GetVoyageDto>> GetVoyageById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResponse<List<GetVoyageDto>>> GetVoyagesByUserId(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResponse<List<GetVoyageDto>>> GetVoyagesByVehicleId(int vehicleId)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResponse<GetVoyageDto>> PatchVoyage(int vehicleId, JsonPatchDocument<UpdateVoyageDto> patchDoc, ModelStateDictionary modelState)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResponse<GetVoyageDto>> UpdateVoyage(UpdateVoyageDto updatedVoyage)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResponse<GetVoyageDto>> UpdateVoyageProfileImage(int vehicleId, IFormFile imageFile)
        {
            throw new NotImplementedException();
        }
    }
}
