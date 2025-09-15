using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Controllers;
using ParrotsAPI2.Dtos.BidDtos;
using ParrotsAPI2.Dtos.VehicleImageDtos;
using ParrotsAPI2.Dtos.VoyageImageDtos;
using ParrotsAPI2.Dtos.WaypointDtos;
using ParrotsAPI2.Models;
using System.Globalization;

namespace ParrotsAPI2.Services.Voyage
{
    public class VoyageService2 : IVoyageService
    {

        private readonly IMapper _mapper;
        private readonly DataContext _context;

        public VoyageService2(IMapper mapper, DataContext context)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ServiceResponse<GetVoyageDto>> AddVoyage(AddVoyageDto newVoyage)
        {
            var serviceResponse = new ServiceResponse<GetVoyageDto>();
            string voyageProfileImage = "";

            if (newVoyage.ImageFile is not null && newVoyage.ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(newVoyage.ImageFile.FileName);
                var filePath = Path.Combine("Uploads/VoyageImages/", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newVoyage.ImageFile.CopyToAsync(stream);
                }
                voyageProfileImage = fileName;
            }

            // var user = await _context.Users.FirstOrDefaultAsync(c => c.Id == newVoyage.UserId);
            var user = await _context.Users.FindAsync(newVoyage.UserId);
            if (user == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "User not found.";
                return serviceResponse;
            }

            // var vehicle = await _context.Vehicles.FirstOrDefaultAsync(c => c.Id == newVoyage.VehicleId);
            var vehicle = await _context.Vehicles.FindAsync(newVoyage.VehicleId);
            if (vehicle == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Vehicle not found.";
                return serviceResponse;
            }

            var voyage = _mapper.Map<Models.Voyage>(newVoyage);

            voyage.User = user;
            voyage.Vehicle = vehicle;
            voyage.VehicleImage = vehicle?.ProfileImageUrl;
            voyage.ProfileImage = voyageProfileImage;
            voyage.VehicleType = vehicle?.Type ?? default;
            voyage.VehicleName = vehicle?.Name;
            voyage.CreatedAt = DateTime.UtcNow;
            // voyage.Confirmed = false;
            // voyage.IsDeleted = false;

            _context.Voyages.Add(voyage);
            await _context.SaveChangesAsync();

            serviceResponse.Data = _mapper.Map<GetVoyageDto>(voyage);
            return serviceResponse;
        }

        public async Task<ServiceResponse<string>> AddVoyageImage(int voyageId, IFormFile imageFile, string userId)
        {
            // userId is read from the token.
            // if voyage owner Id is not equal to userId, controller returns forbidden
            var serviceResponse = new ServiceResponse<string>();

            try
            {
                if (imageFile == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No image file provided.";
                    return serviceResponse;
                }

                const long maxFileSize = 5 * 1024 * 1024; // 5MB in bytes
                if (imageFile.Length > maxFileSize)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Image file size exceeds 5 MB limit.";
                    return serviceResponse;
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Unsupported image file type. Allowed types: jpg, jpeg, png, gif.";
                    return serviceResponse;
                }

                var existingVoyage = await _context.Voyages
                    .Include(v => v.VoyageImages)
                    .FirstOrDefaultAsync(v => v.Id == voyageId);

                if (existingVoyage == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Voyage not found";
                    return serviceResponse;
                }

                var fileName = Guid.NewGuid().ToString() + fileExtension;
                var filePath = Path.Combine("Uploads/VoyageImages/", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                var newVoyageImage = new VoyageImage
                {
                    VoyageImagePath = fileName,
                    UserId = userId
                };

                existingVoyage.VoyageImages ??= new List<VoyageImage>();
                existingVoyage.VoyageImages.Add(newVoyageImage);

                await _context.SaveChangesAsync();

                var newImageId = newVoyageImage.Id.ToString();
                serviceResponse.Data = newImageId;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error adding voyage image: {ex.Message}";
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetVoyageDto>>> DeleteVoyage(int id)
        {
            var serviceResponse = new ServiceResponse<List<GetVoyageDto>>();
            try
            {
                var voyage = await _context.Voyages.FindAsync(id);
                if (voyage == null)
                {
                    throw new Exception($"Voyage with ID `{id}` not found");
                }

                // Soft delete voyage
                voyage.IsDeleted = true;

                // Hard delete related favorites
                var relatedFavorites = await _context.Favorites
                    .Where(f => f.Type == "voyage" && f.ItemId == id)
                    .ToListAsync();

                if (relatedFavorites.Count > 0)
                {
                    _context.Favorites.RemoveRange(relatedFavorites);
                }

                await _context.SaveChangesAsync();

                // Return non-deleted voyages
                var voyages = await _context.Voyages
                    .Where(v => !v.IsDeleted)
                    .ToListAsync();

                serviceResponse.Data = voyages
                    .Select(c => _mapper.Map<GetVoyageDto>(c))
                    .ToList();
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error soft deleting voyage: {ex.Message}";
                if (ex.InnerException != null)
                {
                    serviceResponse.Message += $" Inner Exception: {ex.InnerException.Message}";
                }
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetVoyageDto>>> CheckAndDeleteVoyage(int id)
        {
            var serviceResponse = new ServiceResponse<List<GetVoyageDto>>();
            try
            {
                var voyage = await _context.Voyages.FindAsync(id);
                if (voyage == null)
                {
                    throw new Exception($"Voyage with ID `{id}` not found");
                }

                // Fetch waypoints and voyage images
                var waypoints = await _context.Waypoints
                    .Where(w => w.VoyageId == id)
                    .ToListAsync();

                var voyageImages = await _context.VoyageImages
                    .Where(vi => vi.VoyageId == id)
                    .ToListAsync();

                // Only proceed if there are NO waypoints OR NO voyage images
                if (!waypoints.Any() || !voyageImages.Any())
                {
                    // Soft delete voyage
                    voyage.IsDeleted = true;

                    // Hard delete related favorites
                    var relatedFavorites = await _context.Favorites
                        .Where(f => f.Type == "voyage" && f.ItemId == id)
                        .ToListAsync();

                    if (relatedFavorites.Any())
                    {
                        _context.Favorites.RemoveRange(relatedFavorites);
                    }

                    await _context.SaveChangesAsync();
                }

                // Return non-deleted voyages
                var voyages = await _context.Voyages
                    .Where(v => !v.IsDeleted)
                    .ToListAsync();

                serviceResponse.Data = voyages
                    .Select(c => _mapper.Map<GetVoyageDto>(c))
                    .ToList();
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error soft deleting voyage: {ex.Message}";
                if (ex.InnerException != null)
                {
                    serviceResponse.Message += $" Inner Exception: {ex.InnerException.Message}";
                }
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetVoyageDto>>> GetAllVoyages()
        {
            var serviceResponse = new ServiceResponse<List<GetVoyageDto>>();
            var dbVoyages = await _context.Voyages.ToListAsync();
            serviceResponse.Data = dbVoyages.Select(c => _mapper.Map<GetVoyageDto>(c)).ToList();
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVoyageDto>> GetVoyageById(int id)
        {
            var serviceResponse = new ServiceResponse<GetVoyageDto>();
            var voyage = await _context.Voyages
                .Include(v => v.User)
                .Include(v => v.VoyageImages)
                .Include(v => v.Vehicle)
                .FirstOrDefaultAsync(c => c.Id == id && c.Confirmed == true && c.IsDeleted == false);

            if (voyage == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Voyage not found";
                return serviceResponse;
            }

            // Map related entities to their DTOs
            var userDto = _mapper.Map<UserDto>(voyage.User);
            var voyageImageDtos = _mapper.Map<List<VoyageImageDto>>(voyage.VoyageImages);
            var vehicleDto = _mapper.Map<VehicleDto>(voyage.Vehicle);

            // Improved bid query with async and projection
            var bidDtos = await _context.Bids
                .Where(bid => bid.VoyageId == id)
                .Select(bid => new VoyageBidDto
                {
                    Accepted = bid.Accepted,
                    Id = bid.Id,
                    Message = bid.Message,
                    OfferPrice = bid.OfferPrice,
                    Currency = bid.Currency,
                    DateTime = bid.DateTime,
                    VoyageId = bid.VoyageId,
                    UserId = bid.UserId,
                    PersonCount = bid.PersonCount,
                    UserName = _context.Users
                        .Where(u => u.Id == bid.UserId)
                        .Select(u => u.UserName)
                        .FirstOrDefault(),
                    UserProfileImage = _context.Users
                        .Where(u => u.Id == bid.UserId)
                        .Select(u => u.ProfileImageUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // Map voyage entity to DTO
            var voyageDto = _mapper.Map<GetVoyageDto>(voyage);

            // Get waypoints asynchronously
            var waypointDtos = await _context.Waypoints
                .Where(w => w.VoyageId == id)
                .Select(w => _mapper.Map<GetWaypointDto>(w))
                .ToListAsync();

            // Assign related DTOs
            voyageDto.User = userDto;
            voyageDto.VoyageImages = voyageImageDtos;
            voyageDto.Vehicle = vehicleDto;
            voyageDto.Bids = bidDtos;
            voyageDto.Waypoints = waypointDtos;

            serviceResponse.Data = voyageDto;
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVoyageDto>> GetUnconfirmedVoyageById(int id)
        {
            var serviceResponse = new ServiceResponse<GetVoyageDto>();
            var voyage = await _context.Voyages
                .Include(v => v.User)
                .Include(v => v.VoyageImages)
                .Include(v => v.Vehicle)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted == false);

            if (voyage == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Voyage not found";
                return serviceResponse;
            }

            // Map related entities to their DTOs
            var userDto = _mapper.Map<UserDto>(voyage.User);
            var voyageImageDtos = _mapper.Map<List<VoyageImageDto>>(voyage.VoyageImages);
            var vehicleDto = _mapper.Map<VehicleDto>(voyage.Vehicle);

            // Improved bid query with async and projection
            var bidDtos = await _context.Bids
                .Where(bid => bid.VoyageId == id)
                .Select(bid => new VoyageBidDto
                {
                    Accepted = bid.Accepted,
                    Id = bid.Id,
                    Message = bid.Message,
                    OfferPrice = bid.OfferPrice,
                    Currency = bid.Currency,
                    DateTime = bid.DateTime,
                    VoyageId = bid.VoyageId,
                    UserId = bid.UserId,
                    PersonCount = bid.PersonCount,
                    UserName = _context.Users
                        .Where(u => u.Id == bid.UserId)
                        .Select(u => u.UserName)
                        .FirstOrDefault(),
                    UserProfileImage = _context.Users
                        .Where(u => u.Id == bid.UserId)
                        .Select(u => u.ProfileImageUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // Map voyage entity to DTO
            var voyageDto = _mapper.Map<GetVoyageDto>(voyage);

            // Get waypoints asynchronously
            var waypointDtos = await _context.Waypoints
                .Where(w => w.VoyageId == id)
                .Select(w => _mapper.Map<GetWaypointDto>(w))
                .ToListAsync();

            // Assign related DTOs
            voyageDto.User = userDto;
            voyageDto.VoyageImages = voyageImageDtos;
            voyageDto.Vehicle = vehicleDto;
            voyageDto.Bids = bidDtos;
            voyageDto.Waypoints = waypointDtos;

            serviceResponse.Data = voyageDto;
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetVoyageDto>>> GetVoyagesByUserId(string userId)  // updated for confirmed
        {
            var serviceResponse = new ServiceResponse<List<GetVoyageDto>>();
            var voyages = await _context.Voyages
                .Include(v => v.User)
                .Include(v => v.VoyageImages)
                .Include(v => v.Vehicle)
                //.Where(v => v.UserId == userId)
                .Where(v => v.UserId == userId && v.Confirmed == true && v.IsDeleted == false)
                .ToListAsync();

            if (voyages == null || voyages.Count == 0)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "No voyages found for the given user ID";
                return serviceResponse;
            }

            var voyageDtos = voyages.Select(voyage =>
            {
                var userDto = _mapper.Map<UserDto>(voyage?.User);
                var voyageImageDtos = _mapper.Map<List<VoyageImageDto>>(voyage?.VoyageImages);
                var vehicleDto = _mapper.Map<VehicleDto>(voyage?.Vehicle);
                var bidDtos = _mapper.Map<List<VoyageBidDto>>(_context.Bids
                        .Where(bid => voyage != null && bid.VoyageId == voyage.Id)
                        .ToList());
                var voyageDto = _mapper.Map<GetVoyageDto>(voyage);
                var waypointDtos = _mapper.Map<List<GetWaypointDto>>(_context.Waypoints
                        .Where(w => voyage != null && w.VoyageId == voyage.Id)
                        .ToList());
                voyageDto.User = userDto;
                voyageDto.VoyageImages = voyageImageDtos;
                voyageDto.Vehicle = vehicleDto;
                voyageDto.Bids = bidDtos;
                voyageDto.Waypoints = waypointDtos;
                return voyageDto;
            }).ToList();

            serviceResponse.Data = voyageDtos;
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetVoyageDto>>> GetVoyagesByVehicleId(int vehicleId)
        {
            var serviceResponse = new ServiceResponse<List<GetVoyageDto>>();

            var voyages = await _context.Voyages
                .Include(v => v.User)
                .Include(v => v.VoyageImages)
                .Include(v => v.Vehicle)
                //.Where(v => v.VehicleId == vehicleId)
                .Where(v => v.VehicleId == vehicleId && v.Confirmed == true && v.IsDeleted == false)
                .ToListAsync();

            if (voyages == null || voyages.Count == 0)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "No voyages found for the given vehicle ID";
                return serviceResponse;
            }

            var voyageDtos = voyages
                .Select(voyage =>
            {
                if (voyage == null)
                {
                    return new GetVoyageDto();
                }
                var userDto = _mapper.Map<UserDto>(voyage?.User);
                var voyageImageDtos = _mapper.Map<List<VoyageImageDto>>(voyage?.VoyageImages);
                var vehicleDto = _mapper.Map<VehicleDto>(voyage?.Vehicle);
                var bidDtos = _mapper.Map<List<VoyageBidDto>>(_context.Bids.Where(bid => voyage != null && bid.VoyageId == voyage.Id).ToList());
                var voyageDto = _mapper.Map<GetVoyageDto>(voyage);
                var waypointDtos = _mapper.Map<List<GetWaypointDto>>(_context.Waypoints.Where(w => voyage != null && w.VoyageId == voyage.Id).ToList());
                voyageDto.User = userDto;
                voyageDto.VoyageImages = voyageImageDtos;
                voyageDto.Vehicle = vehicleDto;
                voyageDto.Bids = bidDtos;
                voyageDto.Waypoints = waypointDtos;
                return voyageDto;
            }).ToList();

            serviceResponse.Data = voyageDtos;
            return serviceResponse;
        }
        public async Task<ServiceResponse<GetVoyageDto>> PatchVoyage(int voyageId, JsonPatchDocument<UpdateVoyageDto> patchDoc, ModelStateDictionary modelState)
        {
            var serviceResponse = new ServiceResponse<GetVoyageDto>();

            try
            {
                var voyage = await _context.Voyages.FindAsync(voyageId);
                if (voyage == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = $"Voyage with ID `{voyageId}` not found";
                    return serviceResponse;
                }

                if (patchDoc == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Patch document is null";
                    return serviceResponse;
                }

                var voyageDto = _mapper.Map<UpdateVoyageDto>(voyage);
                patchDoc.ApplyTo(voyageDto, modelState);

                if (!modelState.IsValid)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Invalid model state after patch operations";
                    return serviceResponse;
                }

                _mapper.Map(voyageDto, voyage);
                _context.Voyages.Update(voyage);
                await _context.SaveChangesAsync();

                serviceResponse.Data = _mapper.Map<GetVoyageDto>(voyage);
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error patching voyage: {ex.Message}";
                if (ex.InnerException != null)
                {
                    serviceResponse.Message += $" Inner Exception: {ex.InnerException.Message}";
                }
            }

            return serviceResponse;
        }


        public async Task<ServiceResponse<GetVoyageDto>> UpdateVoyage(UpdateVoyageDto updatedVoyage)
        {
            var serviceResponse = new ServiceResponse<GetVoyageDto>();
            try
            {

                if (updatedVoyage == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Updated voyage data is null";
                    return serviceResponse;
                }

                var voyage = await _context.Voyages.FindAsync(updatedVoyage.Id);
                if (voyage == null)
                {
                    throw new Exception($"Voyage with ID `{updatedVoyage.Id}` not found");
                }
                voyage.Name = updatedVoyage.Name;
                voyage.Brief = updatedVoyage.Brief;
                voyage.Description = updatedVoyage.Description;
                voyage.Vacancy = updatedVoyage.Vacancy;
                voyage.StartDate = updatedVoyage.StartDate;
                voyage.EndDate = updatedVoyage.EndDate;
                voyage.LastBidDate = updatedVoyage.LastBidDate;
                voyage.MinPrice = updatedVoyage.MinPrice;
                voyage.MaxPrice = updatedVoyage.MaxPrice;
                voyage.FixedPrice = updatedVoyage.FixedPrice;
                voyage.Auction = updatedVoyage.Auction;
                voyage.ProfileImage = updatedVoyage.ProfileImage;


                await _context.SaveChangesAsync();
                serviceResponse.Data = _mapper.Map<GetVoyageDto>(voyage);
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVoyageDto>> UpdateVoyageProfileImage(int voyageId, IFormFile imageFile)
        {
            var serviceResponse = new ServiceResponse<GetVoyageDto>();
            try
            {
                var voyage = await _context.Voyages.FindAsync(voyageId);
                if (voyage == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Voyage not found";
                    return serviceResponse;
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine("Uploads/VoyageImages/", fileName);

                    var directoryPath = Path.GetDirectoryName(filePath);
                    if (directoryPath != null && !Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    voyage.ProfileImage = "/Uploads/VoyageImages/" + fileName;
                    await _context.SaveChangesAsync();
                    var voyageDto = _mapper.Map<GetVoyageDto>(voyage);
                    serviceResponse.Success = true;
                    serviceResponse.Data = voyageDto;


                }
                else
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No image provided";
                }

                return serviceResponse;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
                return serviceResponse;
            }
        }

        public async Task<ServiceResponse<List<GetVoyageDto>>> GetVoyagesByCoordinates(double lat1, double lat2, double lon1, double lon2)
        {
            var serviceResponse = new ServiceResponse<List<GetVoyageDto>>();
            try
            {
                var voyages = await _context.Voyages
                    .Where(v =>
                        v.Confirmed &&
                        !v.IsDeleted &&
                        v.Waypoints != null &&
                        v.Waypoints.Any(w =>
                            w.Order == 1 &&
                            w.Latitude >= lat1 &&
                            w.Latitude <= lat2 &&
                            w.Longitude >= lon1 &&
                            w.Longitude <= lon2))
                    .Where(v => v.LastBidDate >= DateTime.Today)
                    .Include(v => v.User)
                    .Include(v => v.Vehicle)
                    .Include(v => v.Waypoints)
                    .ToListAsync();

                foreach (var voyage in voyages)
                {
                    voyage.Waypoints = voyage.Waypoints?.Where(w => w.Order == 1).ToList();
                    Console.WriteLine("Voyage Waypoint : " + voyage.Waypoints);
                }

                if (voyages == null || voyages.Count == 0)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No voyages found with waypoints matching the specified conditions.";
                    return serviceResponse;
                }
                var voyageDtos = _mapper.Map<List<GetVoyageDto>>(voyages);
                serviceResponse.Data = voyageDtos;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error retrieving voyages: {ex.Message}";
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<int>>> GetVoyageIdsByCoordinates(double lat1, double lat2, double lon1, double lon2)
        {
            var serviceResponse = new ServiceResponse<List<int>>();

            try
            {
                var voyageIds = await _context.Voyages
                    .Where(v =>
                        v.Confirmed == true &&
                        v.IsDeleted == false &&
                        v.Waypoints != null &&
                        v.Waypoints.Any(w =>
                            w.Order == 1 &&
                            w.Latitude >= lat1 &&
                            w.Latitude <= lat2 &&
                            w.Longitude >= lon1 &&
                            w.Longitude <= lon2))
                    .Select(v => v.Id)
                    .ToListAsync();


                if (voyageIds == null || voyageIds.Count == 0)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No voyages found with waypoints matching the specified conditions.";
                    return serviceResponse;
                }

                serviceResponse.Data = voyageIds;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error retrieving voyage IDs: {ex.Message}";
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<GetVoyageDto>> DeleteVoyageImage(int voyageImageId)
        {
            var serviceResponse = new ServiceResponse<GetVoyageDto>();
            try
            {
                var voyageImage = await _context.VoyageImages.FirstOrDefaultAsync(v => v.Id == voyageImageId);

                if (voyageImage == null)
                {
                    serviceResponse.Message = "Voyage image not found";
                    return serviceResponse;
                }
                _context.VoyageImages.Remove(voyageImage);
                await _context.SaveChangesAsync();
                var updatedVoyage = await _context.Voyages.FirstOrDefaultAsync(v => v.Id == voyageImage.VoyageId);

                // Consider including related data if GetVoyageDto expects them:
                // var updatedVoyage = await _context.Voyages
                //     .Include(v => v.User)
                //     .Include(v => v.VoyageImages)
                //     .Include(v => v.Vehicle)
                //     .FirstOrDefaultAsync(v => v.Id == voyageImage.VoyageId);


                serviceResponse.Data = _mapper.Map<GetVoyageDto>(updatedVoyage);
                serviceResponse.Success = true;
                serviceResponse.Message = "Voyage image deleted successfully";
            }
            catch (Exception ex)
            {
                serviceResponse.Message = $"Error deleting voyage image: {ex.Message}";
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetVoyageDto>>> GetFilteredVoyages(
            double? lat1, double? lat2, double? lon1, double? lon2,
            int? vacancy, VehicleType? vehicleType,
            DateTime? startDate, DateTime? endDate)
        {
            var serviceResponse = new ServiceResponse<List<GetVoyageDto>>();

            try
            {
                // ✅ Consolidate Confirmed + IsDeleted + LastBidDate filters at the start
                var query = _context.Voyages
                    .Include(v => v.User)
                    .Include(v => v.VoyageImages)
                    .Include(v => v.Vehicle)
                    .Where(v => v.Confirmed && !v.IsDeleted && v.LastBidDate >= DateTime.Today)
                    .AsQueryable();

                // ✅ Apply coordinate filtering only if all lat/lon bounds are provided
                if (lat1.HasValue && lon1.HasValue && lat2.HasValue && lon2.HasValue)
                {
                    query = query.Where(v =>
                        v.Waypoints != null && v.Waypoints.Any(wp =>
                            wp.Order == 1 &&
                            wp.Latitude >= lat1.Value && wp.Latitude <= lat2.Value &&
                            wp.Longitude >= lon1.Value && wp.Longitude <= lon2.Value
                        )
                    );
                }

                if (vacancy.HasValue)
                    query = query.Where(v => v.Vacancy >= vacancy.Value);

                if (startDate.HasValue)
                    query = query.Where(v => v.StartDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(v => v.EndDate <= endDate.Value);

                // ✅ Cleaner Enum check
                if (vehicleType.HasValue && Enum.IsDefined(vehicleType.Value))
                    query = query.Where(v => v.Vehicle != null && v.Vehicle.Type == vehicleType.Value);

                var queryResult = await query.ToListAsync();

                // ✅ Return clear message if no voyages match
                if (queryResult == null || queryResult.Count == 0)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No voyages found matching the filters.";
                    return serviceResponse;
                }

                var filteredVoyages = queryResult.Select(voyage =>
                {
                    // ✅ No need for `?.` because voyage is never null here
                    var userDto = _mapper.Map<UserDto>(voyage.User);
                    var voyageImageDtos = _mapper.Map<List<VoyageImageDto>>(voyage.VoyageImages ?? new List<VoyageImage>());
                    var vehicleDto = _mapper.Map<VehicleDto>(voyage.Vehicle);

                    // ✅ Ensure null-safe collections
                    var bidDtos = _mapper.Map<List<VoyageBidDto>>(
                        _context.Bids.Where(b => b.VoyageId == voyage.Id).ToList()
                    );
                    var waypointDtos = _mapper.Map<List<GetWaypointDto>>(
                        _context.Waypoints.Where(w => w.VoyageId == voyage.Id).ToList()
                    );

                    var voyageDto = _mapper.Map<GetVoyageDto>(voyage);
                    voyageDto.User = userDto;
                    voyageDto.VoyageImages = voyageImageDtos;
                    voyageDto.Vehicle = vehicleDto;
                    voyageDto.Bids = bidDtos;
                    voyageDto.Waypoints = waypointDtos;

                    return voyageDto;
                }).ToList();

                serviceResponse.Data = filteredVoyages;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error retrieving voyages: {ex.Message}";
            }

            return serviceResponse;
        }


        public async Task<ServiceResponse<string>> ConfirmVoyage(int voyageId)
        {
            var serviceResponse = new ServiceResponse<string>();
            var voyage = await _context.Voyages.FirstOrDefaultAsync(v => v.Id == voyageId);
            if (voyage == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Voyage not found.";
                return serviceResponse;
            }

            voyage.Confirmed = true;
            await _context.SaveChangesAsync();
            serviceResponse.Data = "Voyage confirmed";
            return serviceResponse;
        }


        public async Task<ServiceResponse<VoyageImageDto>> GetVoyageImageById(int voyageImageId)
        {
            var serviceResponse = new ServiceResponse<VoyageImageDto>();

            try
            {
                var voyageImage = await _context.VoyageImages
                    .AsNoTracking()
                    .FirstOrDefaultAsync(vi => vi.Id == voyageImageId);

                if (voyageImage == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Voyage image not found for the given image ID.";
                    return serviceResponse;
                }

                var voyageImageDto = new VoyageImageDto
                {
                    Id = voyageImage.Id,
                    VoyageImagePath = voyageImage.VoyageImagePath,
                    VoyageId = voyageImage.VoyageId,
                    UserId = voyageImage.UserId
                };

                serviceResponse.Data = voyageImageDto;
                serviceResponse.Success = true;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"An error occurred while retrieving the voyage image: {ex.Message}";
            }

            return serviceResponse;
        }

    }
}
