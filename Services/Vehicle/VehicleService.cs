using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Dtos.VehicleDtos;
using ParrotsAPI2.Dtos.VehicleImageDtos;
using ParrotsAPI2.Models;

namespace ParrotsAPI2.Services.Vehicle
{
    public class VehicleService : IVehicleService
    {

        private readonly IMapper _mapper;
        private readonly DataContext _context;
        public VehicleService(IMapper mapper, DataContext context)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<ServiceResponse<List<GetVehicleDto>>> AddVehicle(AddVehicleDto newVehicle)
        {
            var serviceResponse = new ServiceResponse<List<GetVehicleDto>>();
            string profileImageUrl = "";
            if (newVehicle.ImageFile != null && newVehicle.ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(newVehicle.ImageFile.FileName);
                var filePath = Path.Combine("Uploads/VehicleImages/", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newVehicle.ImageFile.CopyToAsync(stream);
                }
                profileImageUrl = "/Uploads/VehicleImages/" + fileName;
            }

            var vehicle = _mapper.Map<Models.Vehicle>(newVehicle);
            vehicle.ProfileImageUrl = profileImageUrl;
            var currentUser = await _context.Users.FirstOrDefaultAsync(c => c.Id == newVehicle.UserId);
            vehicle.User = currentUser;
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var vehicles = await _context.Vehicles.ToListAsync();
            serviceResponse.Data = vehicles.Select(c => _mapper.Map<GetVehicleDto>(c)).ToList();

            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVehicleDto>> AddVehicleImage(int vehicleId, IFormFile imageFile)
        {
            var serviceResponse = new ServiceResponse<GetVehicleDto>();
            var existingVehicle = await _context.Vehicles
                .Include(v => v.VehicleImages)
                .FirstOrDefaultAsync(v => v.Id == vehicleId);
            if (existingVehicle == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Vehicle not found";
                return serviceResponse;
            }
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine("Uploads/VehicleImages/", fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
            var newVehicleImage = new VehicleImage
            {
                VehicleImagePath = "/Uploads/VehicleImages/" + fileName
            };
            existingVehicle.VehicleImages ??= new List<VehicleImage>();
            existingVehicle.VehicleImages.Add(newVehicleImage);
            await _context.SaveChangesAsync();
            serviceResponse.Data = _mapper.Map<GetVehicleDto>(existingVehicle);
            return serviceResponse;
        }

        public async Task<ServiceResponse<string>> DeleteVehicle(int id)
        {

            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                var notFoundResponse = new ServiceResponse<string>
                {
                    Success = false,
                    Message = $"Vehicle with ID `{id}` not found"
                };
                return notFoundResponse;
            }

            // get voyageIds - by vehicleId
            var voyageIds = _context.Voyages
                .Where(v => v.VehicleId == id)
                .Select(v => v.Id)
                .ToList();

            // delete voyageImages - by voyageIds
            if (voyageIds.Count > 0)
            {
                var voyageImagesToDelete = _context.VoyageImages
                    .Where(vi => voyageIds.Contains(vi.VoyageId))
                    .ToList();
                _context.VoyageImages.RemoveRange(voyageImagesToDelete);
                await _context.SaveChangesAsync();
            }

            // delete bids - byVoyageIds
            if (voyageIds.Count > 0)
            {
                var bidsToDelete = _context.Bids
                    .Where(b => voyageIds.Contains(b.VoyageId))
                    .ToList();
                _context.Bids.RemoveRange(bidsToDelete);
                await _context.SaveChangesAsync();
            }

            // delete waypoints - by VoyageIds
            if (voyageIds.Count > 0)
            {
                var waypointsToDelete = _context.Waypoints
                    .Where(b => voyageIds.Contains(b.VoyageId))
                    .ToList();
                _context.Waypoints.RemoveRange(waypointsToDelete);
                await _context.SaveChangesAsync();
            }

            // delete voyages - by vehicleId
            var voyagesToDelete = _context.Voyages
                .Where(v => v.VehicleId == id)
                .ToList();
            if (voyagesToDelete.Count > 0)
            {
                _context.Voyages.RemoveRange(voyagesToDelete);
                await _context.SaveChangesAsync();
            }


            // delete vehicleImages - by vehicleId
            var vehicleImagesToDelete = _context.VehicleImages
                .Where(vi => vi.VehicleId == id)
                .ToList();
            if (vehicleImagesToDelete.Count > 0)
            {
                _context.VehicleImages.RemoveRange(vehicleImagesToDelete);
                await _context.SaveChangesAsync();
            }

            // delete vehicle
            var serviceResponse = new ServiceResponse<string>();
            try
            {
                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();
                serviceResponse.Data = "Vehicle successfully deleted";
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetVehicleDto>>> GetAllVehicles()
        {
            var serviceResponse = new ServiceResponse<List<GetVehicleDto>>();
            var dbVehicles = await _context.Vehicles.ToListAsync();
            serviceResponse.Data = dbVehicles.Select(c => _mapper.Map<GetVehicleDto>(c)).ToList();
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVehicleDto>> GetVehicleById(int id)
        {

            var serviceResponse = new ServiceResponse<GetVehicleDto>();

            var vehicle = await _context.Vehicles
                .Include(v => v.User)
                .Include(v => v.VehicleImages)
                .Include(v => v.Voyages)
                .FirstOrDefaultAsync(c => c.Id == id);

            var userDto = _mapper.Map<UserDto>(vehicle?.User);
            var vehicleImageDtos = _mapper.Map<List<VehicleImageDto>>(vehicle?.VehicleImages);
            var voyageDtos = _mapper.Map<List<VoyageDto>>(vehicle?.Voyages);

            var vehicleDto = _mapper.Map<GetVehicleDto>(vehicle);

            vehicleDto.User = userDto;
            vehicleDto.VehicleImages = vehicleImageDtos;
            vehicleDto.Voyages = voyageDtos;

            serviceResponse.Data = vehicleDto;

            if (vehicle == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Vehicle not found";
                return serviceResponse;
            }
            serviceResponse.Data = _mapper.Map<GetVehicleDto>(vehicle);
            return serviceResponse;

        }

        public async Task<ServiceResponse<List<GetVehicleDto>>> GetVehiclesByUserId(string userId)
        {
            var serviceResponse = new ServiceResponse<List<GetVehicleDto>>();
            var vehicles = await _context.Vehicles
                .Where(v => v.UserId == userId)
                .ToListAsync();
            serviceResponse.Data = _mapper.Map<List<GetVehicleDto>>(vehicles);
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVehicleDto>> PatchVehicle(int vehicleId, [FromBody] JsonPatchDocument<UpdateVehicleDto> patchDoc, ModelStateDictionary modelState)
        {
            var serviceResponse = new ServiceResponse<GetVehicleDto>();
            try
            {
                var vehicle = await _context.Vehicles.FindAsync(vehicleId);
                if (vehicle == null)
                {
                    throw new Exception($"vehicle with ID `{vehicleId}` not found");
                }
                var vehicleDto = _mapper.Map<UpdateVehicleDto>(vehicle);
                patchDoc.ApplyTo(vehicleDto, modelState);
                if (!modelState.IsValid)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Invalid model state after patch operations";
                    return serviceResponse;
                }
                _mapper.Map(vehicleDto, vehicle);
                _context.Vehicles.Attach(vehicle);
                _context.Entry(vehicle).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                serviceResponse.Data = _mapper.Map<GetVehicleDto>(vehicle);
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVehicleDto>> UpdateVehicle(UpdateVehicleDto updatedVehicle)
        {
            var serviceResponse = new ServiceResponse<GetVehicleDto>();
            try
            {
                var vehicle = await _context.Vehicles.FindAsync(updatedVehicle.Id);
                if (vehicle == null)
                {
                    throw new Exception($"Vehicle with ID `{updatedVehicle.Id}` not found");
                }
                vehicle.Name = updatedVehicle.Name;
                vehicle.Type = updatedVehicle.Type;
                vehicle.Capacity = updatedVehicle.Capacity;
                vehicle.Description = updatedVehicle.Description;
                vehicle.ProfileImageUrl = updatedVehicle.ProfileImageUrl;

        await _context.SaveChangesAsync();
                serviceResponse.Data = _mapper.Map<GetVehicleDto>(vehicle);
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVehicleDto>> UpdateVehicleProfileImage(int vehicleId, IFormFile imageFile)
        {
            var serviceResponse = new ServiceResponse<GetVehicleDto>();
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "vehicle not found";
                return serviceResponse;
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine("Uploads/VehicleImages/", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                vehicle.ProfileImageUrl = "/Uploads/VehicleImages/" + fileName;
                await _context.SaveChangesAsync();
                var vehicleDto = _mapper.Map<GetVehicleDto>(vehicle);
                serviceResponse.Success = true;
                serviceResponse.Data = vehicleDto;
            }
            else
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "No image provided";
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<string>> DeleteVehicleImage(int vehicleImageId)
        {
            var response = new ServiceResponse<string>();
            try
            {
                var vehicleImage = await _context.VehicleImages.FindAsync(vehicleImageId);
                if (vehicleImage == null)
                {
                    response.Success = false;
                    response.Message = "Vehicle image not found.";
                    return response;
                }
                _context.VehicleImages.Remove(vehicleImage);
                await _context.SaveChangesAsync();

                response.Data = $"Vehicle image with ID {vehicleImageId} has been deleted successfully.";
                response.Message = "Vehicle image deleted successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error deleting vehicle image: {ex.Message}";
            }
            return response;
        }

    }
}
