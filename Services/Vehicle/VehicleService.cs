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
        private readonly ILogger<VehicleService> _logger;
        private readonly IBlobService _blobService; // 🟢 CHANGED

        // 🟢 CHANGED - Added BlobService to constructor
        public VehicleService(IMapper mapper, DataContext context, ILogger<VehicleService> logger, IBlobService blobService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _blobService = blobService;
        }

        // 🔹 Helper method for uploading images
        private async Task<string> UploadImageToBlobAsync(IFormFile file)
        {
            const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

            if (file == null || file.Length == 0)
                throw new ArgumentException("No image provided");

            if (file.Length > MaxFileSize)
                throw new ArgumentException("Image size exceeds 5MB limit");

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            return await _blobService.UploadAsync(file.OpenReadStream(), fileName);
        }

        public async Task<ServiceResponse<GetVehicleDto>> AddVehicle(AddVehicleDto newVehicle)
        {
            var serviceResponse = new ServiceResponse<GetVehicleDto>();
            if (newVehicle == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Vehicle data is missing.";
                return serviceResponse;
            }

            string profileImageUrl = string.Empty;

            if (newVehicle.ImageFile != null && newVehicle.ImageFile.Length > 0)
            {
                try
                {
                    // Use helper method to handle file upload and validation
                    profileImageUrl = await UploadImageToBlobAsync(newVehicle.ImageFile);
                }
                catch (Exception ex)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = $"Image upload failed: {ex.Message}";
                    return serviceResponse;
                }
            }

            var vehicle = _mapper.Map<Models.Vehicle>(newVehicle);
            vehicle.ProfileImageUrl = profileImageUrl;
            vehicle.CreatedAt = DateTime.UtcNow;

            var currentUser = await _context.Users.FirstOrDefaultAsync(c => c.Id == newVehicle.UserId);
            if (currentUser == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "User not found.";
                return serviceResponse;
            }

            vehicle.User = currentUser;

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            serviceResponse.Data = _mapper.Map<GetVehicleDto>(vehicle);
            return serviceResponse;
        }

        public async Task<ServiceResponse<string>> AddVehicleImage(int vehicleId, IFormFile imageFile, string userId)
        {
            var serviceResponse = new ServiceResponse<string>();

            if (imageFile == null || imageFile.Length == 0)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Image file is missing or empty";
                return serviceResponse;
            }

            var existingVehicle = await _context.Vehicles
                .Include(v => v.VehicleImages)
                .FirstOrDefaultAsync(v => v.Id == vehicleId);

            if (existingVehicle == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Vehicle not found";
                return serviceResponse;
            }

            try
            {
                // Upload using helper
                var fileName = await UploadImageToBlobAsync(imageFile);

                var newVehicleImage = new VehicleImage
                {
                    VehicleImagePath = fileName,
                    VehicleId = vehicleId,
                    UserId = userId
                };

                existingVehicle.VehicleImages ??= new List<VehicleImage>();
                existingVehicle.VehicleImages.Add(newVehicleImage);

                await _context.SaveChangesAsync();

                serviceResponse.Data = newVehicleImage.Id.ToString();
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error saving vehicle image: {ex.Message}";
                if (ex.InnerException != null)
                    serviceResponse.Message += $" Inner Exception: {ex.InnerException.Message}";
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<string>> DeleteVehicle(int id)
        {
            var serviceResponse = new ServiceResponse<string>();

            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Vehicle with ID `{id}` not found";
                return serviceResponse;
            }

            // Get voyages related to the vehicle
            var voyages = await _context.Voyages
                .Where(v => v.VehicleId == id)
                .ToListAsync();

            // Extract voyage IDs
            var voyageIds = voyages.Select(v => v.Id).ToList();

            // Soft delete voyages
            foreach (var voyage in voyages)
            {
                voyage.IsDeleted = true;
            }

            // Delete favorites related to voyages (Type = "voyage" and itemId in voyageIds)
            var voyageFavorites = await _context.Favorites
                .Where(f => f.Type == "voyage" && voyageIds.Contains(f.ItemId))
                .ToListAsync();

            _context.Favorites.RemoveRange(voyageFavorites);

            // Delete favorites related to vehicle (Type = "vehicle" and itemId == vehicle.Id)
            var vehicleFavorites = await _context.Favorites
                .Where(f => f.Type == "vehicle" && f.ItemId == id)
                .ToListAsync();

            _context.Favorites.RemoveRange(vehicleFavorites);

            // Soft delete vehicle
            vehicle.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();
                serviceResponse.Data = "Vehicle and related voyages soft-deleted, favorites removed.";
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }

            return serviceResponse;
        }

        //only deletes if the vehicle has no images
        public async Task<ServiceResponse<string>> CheckAndDeleteVehicle(int id)
        {
            var serviceResponse = new ServiceResponse<string>();

            // Find the vehicle by ID
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Vehicle with ID `{id}` not found";
                return serviceResponse;
            }

            // Check if any vehicle images exist
            var vehicleImagesExist = await _context.VehicleImages
                .AnyAsync(vi => vi.VehicleId == id);

            if (vehicleImagesExist)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Vehicle not deleted because it has associated images.";
                return serviceResponse;
            }

            // Get voyage IDs related to the vehicle
            var voyageIds = await _context.Voyages
                .Where(v => v.VehicleId == id)
                .Select(v => v.Id)
                .ToListAsync();

            // Soft delete voyages
            var voyages = await _context.Voyages
                .Where(v => v.VehicleId == id)
                .ToListAsync();

            foreach (var voyage in voyages)
            {
                voyage.IsDeleted = true;
            }

            // Soft delete vehicle
            vehicle.IsDeleted = true;

            // Hard delete favorites for voyages
            var voyageFavorites = await _context.Favorites
                .Where(f => f.Type == "voyage" && voyageIds.Contains(f.ItemId))
                .ToListAsync();
            _context.Favorites.RemoveRange(voyageFavorites);

            // Hard delete favorites for vehicle
            var vehicleFavorites = await _context.Favorites
                .Where(f => f.Type == "vehicle" && f.ItemId == id)
                .ToListAsync();
            _context.Favorites.RemoveRange(vehicleFavorites);

            try
            {
                await _context.SaveChangesAsync();
                serviceResponse.Data = "Vehicle and its voyages soft deleted, related favorites hard deleted.";
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
                .FirstOrDefaultAsync(c => c.Id == id && c.Confirmed == true && c.IsDeleted == false);

            if (vehicle == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Vehicle not found";
                return serviceResponse;
            }

            if (vehicle.Type == VehicleType.Walk || vehicle.Type == VehicleType.Run)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Run or walk cant be fetched.";
                return serviceResponse;
            }

            var userDto = _mapper.Map<UserDto>(vehicle?.User);
            var vehicleImageDtos = _mapper.Map<List<VehicleImageDto>>(vehicle?.VehicleImages);
            var voyageDtos = _mapper.Map<List<VoyageDto>>(vehicle?.Voyages);

            var vehicleDto = _mapper.Map<GetVehicleDto>(vehicle);

            vehicleDto.User = userDto;
            vehicleDto.VehicleImages = vehicleImageDtos;
            vehicleDto.Voyages = voyageDtos;
            serviceResponse.Data = vehicleDto;

            // serviceResponse.Data = _mapper.Map<GetVehicleDto>(vehicle);
            return serviceResponse;

        }

        public async Task<ServiceResponse<GetVehicleDto>> GetUnconfirmedVehicleById(int id)
        {
            var serviceResponse = new ServiceResponse<GetVehicleDto>();

            var vehicle = await _context.Vehicles
                .Include(v => v.User)
                .Include(v => v.VehicleImages)
                .Include(v => v.Voyages)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted == false);

            if (vehicle == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Vehicle not found";
                return serviceResponse;
            }

            if (vehicle.Type == VehicleType.Walk || vehicle.Type == VehicleType.Run)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Run or walk cant be fetched.";
                return serviceResponse;
            }

            var userDto = _mapper.Map<UserDto>(vehicle?.User);
            var vehicleImageDtos = _mapper.Map<List<VehicleImageDto>>(vehicle?.VehicleImages);
            var voyageDtos = _mapper.Map<List<VoyageDto>>(vehicle?.Voyages);

            var vehicleDto = _mapper.Map<GetVehicleDto>(vehicle);

            vehicleDto.User = userDto;
            vehicleDto.VehicleImages = vehicleImageDtos;
            vehicleDto.Voyages = voyageDtos;
            serviceResponse.Data = vehicleDto;

            // serviceResponse.Data = _mapper.Map<GetVehicleDto>(vehicle);
            return serviceResponse;

        }

        public async Task<ServiceResponse<List<VehicleImageDto>>> GetVehicleImagesByVehicleId(int vehicleId)
        {
            var serviceResponse = new ServiceResponse<List<VehicleImageDto>>();

            try
            {
                var vehicleImages = await _context.VehicleImages
                    .AsNoTracking()
                    .Where(vi => vi.VehicleId == vehicleId)
                    .ToListAsync();

                if (vehicleImages == null || !vehicleImages.Any())
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Vehicle images not found for the given vehicleId.";
                    return serviceResponse;
                }

                var vehicleImageDtos = vehicleImages.Select(vi => new VehicleImageDto
                {
                    Id = vi.Id,
                    VehicleImagePath = vi.VehicleImagePath,
                    VehicleId = vi.VehicleId
                }).ToList();

                serviceResponse.Data = vehicleImageDtos;
                serviceResponse.Success = true;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"An error occurred while retrieving vehicle images: {ex.Message}";
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<VehicleImageDto>> GetVehicleImageById(int vehicleImageId)
        {
            var serviceResponse = new ServiceResponse<VehicleImageDto>();

            try
            {
                var vehicleImage = await _context.VehicleImages
                    .AsNoTracking()
                    .FirstOrDefaultAsync(vi => vi.Id == vehicleImageId);

                if (vehicleImage == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Vehicle image not found for the given image ID.";
                    return serviceResponse;
                }

                var vehicleImageDto = new VehicleImageDto
                {
                    Id = vehicleImage.Id,
                    VehicleImagePath = vehicleImage.VehicleImagePath,
                    VehicleId = vehicleImage.VehicleId,
                    UserId = vehicleImage.UserId
                };

                serviceResponse.Data = vehicleImageDto;
                serviceResponse.Success = true;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"An error occurred while retrieving the vehicle image: {ex.Message}";
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetVehicleDto>>> GetVehiclesByUserId(string userId)
        {
            var serviceResponse = new ServiceResponse<List<GetVehicleDto>>();

            // if userId is 1, return error
            if (userId == "1")
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "User ID '1' is not allowed to fetch vehicles.";
                return serviceResponse;
            }

            try
            {
                var vehicles = await _context.Vehicles
                    .Where(v => v.UserId == userId && v.Confirmed && v.IsDeleted == false)
                    .ToListAsync();

                if (vehicles == null || !vehicles.Any())
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No confirmed vehicles found for the specified user.";
                    return serviceResponse;
                }

                serviceResponse.Data = _mapper.Map<List<GetVehicleDto>>(vehicles);
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"An error occurred while fetching vehicles: {ex.Message}";
            }

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
                    serviceResponse.Success = false;
                    serviceResponse.Message = $"Vehicle with ID `{vehicleId}` not found.";
                    return serviceResponse;
                }
                // var vehicleDto = _mapper.Map<UpdateVehicleDto>(vehicle);
                var vehicleDto = _mapper.Map<UpdateVehicleDto>(vehicle);
                // if patchdoc vheicle type is walk or run, return error
                if (vehicleDto.Type == VehicleType.Walk || vehicleDto.Type == VehicleType.Run)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Walk and Run vehicles cannot be patched.";
                    return serviceResponse;
                }
                patchDoc.ApplyTo(vehicleDto, modelState);
                if (!modelState.IsValid)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Invalid model state after applying patch.";
                    return serviceResponse;
                }
                _mapper.Map(vehicleDto, vehicle);
                await _context.SaveChangesAsync();
                serviceResponse.Data = _mapper.Map<GetVehicleDto>(vehicle);
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error updating vehicle: {ex.Message}";
                if (ex.InnerException != null)
                {
                    serviceResponse.Message += $" Inner Exception: {ex.InnerException.Message}";
                }
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
                serviceResponse.Message = "Vehicle not found";
                return serviceResponse;
            }

            if (imageFile == null || imageFile.Length == 0)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "No image provided";
                return serviceResponse;
            }

            try
            {
                // Use the helper method to upload the image
                var fileName = await UploadImageToBlobAsync(imageFile);

                // Update the vehicle profile image URL
                vehicle.ProfileImageUrl = fileName;
                await _context.SaveChangesAsync();

                serviceResponse.Data = _mapper.Map<GetVehicleDto>(vehicle);
                serviceResponse.Success = true;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error uploading image: {ex.Message}";
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

                // Delete image from Blob Storage if it exists
                if (!string.IsNullOrEmpty(vehicleImage.VehicleImagePath))
                {
                    try
                    {
                        await _blobService.DeleteAsync(vehicleImage.VehicleImagePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to delete image from blob: {ex.Message}");
                    }
                }

                // Remove the record from database
                _context.VehicleImages.Remove(vehicleImage);
                await _context.SaveChangesAsync();

                response.Success = true;
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

        public async Task<ServiceResponse<string>> ConfirmVehicle(int vehicleId)
        {

            var serviceResponse = new ServiceResponse<string>();

            // var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
            var vehicle = await _context.Vehicles.FindAsync(vehicleId); // FindAsync is more efficient for PK lookup

            if (vehicle == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "vehicle not found.";
                return serviceResponse;
            }

            vehicle.Confirmed = true;
            await _context.SaveChangesAsync();

            serviceResponse.Data = "vehicle confirmed";
            return serviceResponse;

        }


    }
}
