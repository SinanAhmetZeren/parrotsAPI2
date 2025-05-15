using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Dtos.BidDtos;
using ParrotsAPI2.Dtos.FavoriteDtos;
using ParrotsAPI2.Dtos.MessageDtos;
using ParrotsAPI2.Dtos.VoyageImageDtos;
using ParrotsAPI2.Dtos.WaypointDtos;
using ParrotsAPI2.Models;

namespace ParrotsAPI2.Services.Message
{
    public class FavoriteService : IFavoriteService
    {

        private readonly IMapper _mapper;
        private readonly DataContext _context;

        public FavoriteService(IMapper mapper, DataContext context)
        {
            _context = context;
            _mapper = mapper;
        }
 

        public async Task<ServiceResponse<GetFavoriteDto>> AddFavorite(AddFavoriteDto newFavorite)
        {
            var serviceResponse = new ServiceResponse<GetFavoriteDto>();

            if (newFavorite == null)
                return new ServiceResponse<GetFavoriteDto> { Success = false, Message = "Input data is null" };

            if (string.IsNullOrEmpty(newFavorite.UserId))
                return new ServiceResponse<GetFavoriteDto> { Success = false, Message = "UserId cannot be null or empty" };

            if (string.IsNullOrEmpty(newFavorite.Type))
                return new ServiceResponse<GetFavoriteDto> { Success = false, Message = "Type cannot be null or empty" };

            if (newFavorite.ItemId <= 0)
                return new ServiceResponse<GetFavoriteDto> { Success = false, Message = "ItemId should be greater than 0" };

            try
            {
                var existing = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.UserId == newFavorite.UserId && f.ItemId == newFavorite.ItemId && f.Type == newFavorite.Type);

                if (existing != null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Favorite already exists";
                    return serviceResponse;
                }

                var favorite = new Favorite
                {
                    UserId = newFavorite.UserId,
                    Type = newFavorite.Type,
                    ItemId = newFavorite.ItemId
                };

                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();

                serviceResponse.Data = _mapper.Map<GetFavoriteDto>(favorite);
                serviceResponse.Success = true;
                serviceResponse.Message = "Favorite added successfully";
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error adding favorite: {ex.Message}";
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<string>> DeleteFavoriteVoyage(string userId, int voyageId)
        {
            var serviceResponse = new ServiceResponse<string>();

            if (string.IsNullOrEmpty(userId) || voyageId <= 0)
            {
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Invalid userId or voyageId"
                };
            }

            try
            {
                var favorites = await _context.Favorites
                    .Where(f => f.UserId == userId && f.ItemId == voyageId && f.Type == "voyage")
                    .ToListAsync();

                if (!favorites.Any())
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Favorites not found";
                    return serviceResponse;
                }

                _context.Favorites.RemoveRange(favorites);
                await _context.SaveChangesAsync();

                serviceResponse.Success = true;
                serviceResponse.Data = "Favorites deleted successfully";
                serviceResponse.Message = "Favorites deleted successfully";
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error deleting favorite: {ex.Message}";
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<string>> DeleteFavoriteVehicle(string userId, int vehicleId)
        {
            var serviceResponse = new ServiceResponse<string>();

            if (string.IsNullOrEmpty(userId) || vehicleId <= 0)
            {
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Invalid userId or vehicleId"
                };
            }

            try
            {
                var favorites = await _context.Favorites
                    .Where(f => f.UserId == userId && f.ItemId == vehicleId && f.Type == "vehicle")
                    .ToListAsync();

                if (!favorites.Any())
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Favorites not found";
                    return serviceResponse;
                }

                _context.Favorites.RemoveRange(favorites);
                await _context.SaveChangesAsync();

                serviceResponse.Success = true;
                serviceResponse.Data = "Favorites deleted successfully";
                serviceResponse.Message = "Favorites deleted successfully";
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error deleting favorite: {ex.Message}";
            }

            return serviceResponse;
        }
 

        public async Task<ServiceResponse<List<GetFavoriteDto>>> GetFavoritesByUserId(string userId)
        {
            var serviceResponse = new ServiceResponse<List<GetFavoriteDto>>();
            try
            {
                var favorites = await _context.Favorites
                    .Where(f => f.UserId == userId)
                    .ToListAsync();

                if (!favorites.Any())
                {
                    serviceResponse.Data = new List<GetFavoriteDto>();
                    serviceResponse.Success = true;
                    serviceResponse.Message = "No favorites found for the given user ID";
                    return serviceResponse;
                }

                var favoriteDtos = _mapper.Map<List<GetFavoriteDto>>(favorites);
                serviceResponse.Data = favoriteDtos;
                serviceResponse.Success = true;
                serviceResponse.Message = "Favorites retrieved successfully";
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error retrieving favorites: {ex.Message}";
            }
            return serviceResponse;
        }


        public async Task<ServiceResponse<List<GetVoyageDto>>> GetFavoriteVoyagesByUserId(string userId)
        {
            var serviceResponse = new ServiceResponse<List<GetVoyageDto>>();

            // Get favorite voyage IDs
            var favoriteVoyages = await _context.Favorites
                .Where(f => f.Type == "voyage" && f.UserId == userId)
                .ToListAsync();

            var voyageIds = favoriteVoyages.Select(fv => fv.ItemId).ToList();

            if (!voyageIds.Any())
            {
                serviceResponse.Success = true;
                serviceResponse.Message = "No favorite voyages found for the given user ID";
                serviceResponse.Data = new List<GetVoyageDto>();
                return serviceResponse;
            }

            // Load voyages with related entities
            var voyages = await _context.Voyages
                .Include(v => v.User)
                .Include(v => v.VoyageImages)
                .Include(v => v.Vehicle)
                .Where(v => voyageIds.Contains(v.Id))
                .ToListAsync();

            if (!voyages.Any())
            {
                serviceResponse.Success = true;
                serviceResponse.Message = "No voyages found for the given user ID";
                serviceResponse.Data = new List<GetVoyageDto>();
                return serviceResponse;
            }

            // Batch load related Bids and Waypoints
            var bids = await _context.Bids
                .Where(b => voyageIds.Contains(b.VoyageId))
                .ToListAsync();

            var waypoints = await _context.Waypoints
                .Where(w => voyageIds.Contains(w.VoyageId))
                .ToListAsync();

            // Map voyages to DTOs
            var voyageDtos = voyages.Select(voyage =>
            {
                var userDto = _mapper.Map<UserDto>(voyage.User);
                var voyageImageDtos = _mapper.Map<List<VoyageImageDto>>(voyage.VoyageImages);
                var vehicleDto = _mapper.Map<VehicleDto>(voyage.Vehicle);
                var bidDtos = _mapper.Map<List<VoyageBidDto>>(bids.Where(b => b.VoyageId == voyage.Id).ToList());
                var waypointDtos = _mapper.Map<List<GetWaypointDto>>(waypoints.Where(w => w.VoyageId == voyage.Id).ToList());
                var voyageDto = _mapper.Map<GetVoyageDto>(voyage);

                voyageDto.User = userDto;
                voyageDto.VoyageImages = voyageImageDtos;
                voyageDto.Vehicle = vehicleDto;
                voyageDto.Bids = bidDtos;
                voyageDto.Waypoints = waypointDtos;

                return voyageDto;
            }).ToList();

            serviceResponse.Success = true;
            serviceResponse.Data = voyageDtos;
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetVehicleDto>>> GetFavoriteVehiclesByUserId(string userId)
        {
            var serviceResponse = new ServiceResponse<List<GetVehicleDto>>();

            var favoritesVehicles = await _context.Favorites
                .Where(fv => fv.Type == "vehicle" && fv.UserId == userId)
                .ToListAsync();

            var vehicleIds = favoritesVehicles.Select(fv => fv.ItemId).ToList();

            if (!vehicleIds.Any())
            {
                serviceResponse.Success = true;
                serviceResponse.Message = "No favorite vehicles found for the given user ID";
                serviceResponse.Data = new List<GetVehicleDto>();
                return serviceResponse;
            }

            var vehicles = await _context.Vehicles
                .Where(v => vehicleIds.Contains(v.Id))
                .ToListAsync();

            if (!vehicles.Any())
            {
                serviceResponse.Success = true;
                serviceResponse.Message = "No vehicles found for the given user ID";
                serviceResponse.Data = new List<GetVehicleDto>();
                return serviceResponse;
            }

            serviceResponse.Success = true;
            serviceResponse.Data = _mapper.Map<List<GetVehicleDto>>(vehicles);
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<int>>> GetFavoriteVehicleIdsByUserId(string userId)
        {
            var serviceResponse = new ServiceResponse<List<int>>();

            var favoritesVehicles = await _context.Favorites
                .Where(fv => fv.Type == "vehicle" && fv.UserId == userId)
                .ToListAsync();

            var vehicleIds = favoritesVehicles.Select(fv => fv.ItemId).ToList();

            if (!vehicleIds.Any())
            {
                serviceResponse.Success = true;
                serviceResponse.Message = "No favorite vehicles found for the given user ID";
                serviceResponse.Data = new List<int>();
                return serviceResponse;
            }

            serviceResponse.Success = true;
            serviceResponse.Data = vehicleIds;
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<int>>> GetFavoriteVoyageIdsByUserId(string userId)
        {
            var serviceResponse = new ServiceResponse<List<int>>();

            var favoritesVoyages = await _context.Favorites
                .Where(fv => fv.Type == "voyage" && fv.UserId == userId)
                .ToListAsync();

            var voyageIds = favoritesVoyages.Select(fv => fv.ItemId).ToList();

            if (!voyageIds.Any())
            {
                serviceResponse.Success = true;
                serviceResponse.Message = "No favorite voyages found for the given user ID";
                serviceResponse.Data = new List<int>();
                return serviceResponse;
            }

            serviceResponse.Success = true;
            serviceResponse.Data = voyageIds;
            return serviceResponse;
        }

            }
}