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
            {
                return new ServiceResponse<GetFavoriteDto>
                {
                    Success = false,
                    Message = "Input data is null",
                    Data = null
                };
            }
            if (string.IsNullOrEmpty(newFavorite.UserId))
            {
                return new ServiceResponse<GetFavoriteDto>
                {
                    Success = false,
                    Message = "UserId cannot be null or empty",
                    Data = null
                };
            }
            if (string.IsNullOrEmpty(newFavorite.Type))
            {
                return new ServiceResponse<GetFavoriteDto>
                {
                    Success = false,
                    Message = "Type cannot be null or empty",
                    Data = null
                };
            }
            if (newFavorite.ItemId <= 0)
            {
                return new ServiceResponse<GetFavoriteDto>
                {
                    Success = false,
                    Message = "ItemId should be greater than 0",
                    Data = null
                };
            }
            try
            {
                var favorite = new Favorite
                {
                    UserId = newFavorite.UserId,
                    Type = newFavorite.Type,
                    ItemId = newFavorite.ItemId
                };

                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();
                var messageDto = _mapper.Map<GetFavoriteDto>(favorite);
                serviceResponse.Data = messageDto;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error adding message: {ex.Message}";
            }
            return serviceResponse;
        }

        public async Task<ServiceResponse<string>> DeleteFavoriteVoyage(string userId, int voyageId)
        {
            var serviceResponse = new ServiceResponse<string>();
            try
            {
                var favorites = await _context.Favorites.Where(
                    f => f.UserId == userId &&
                    f.ItemId == voyageId &&
                    f.Type == "voyage").ToListAsync();


                if (favorites == null || favorites.Count == 0)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Favorites not found";
                    return serviceResponse;
                }
                _context.Favorites.RemoveRange(favorites);
                await _context.SaveChangesAsync();
                serviceResponse.Data = "Favorites deleted successfully";
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
            try
            {

                var favorites = await _context.Favorites.Where(
                    f => f.UserId == userId &&
                    f.ItemId == vehicleId &&
                    f.Type == "vehicle").ToListAsync();

                if (favorites == null || favorites.Count == 0)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Favorites not found";
                    return serviceResponse;
                }

                _context.Favorites.RemoveRange(favorites);
                await _context.SaveChangesAsync();
                serviceResponse.Data = "Favorites deleted successfully";

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
                if (favorites == null || favorites.Count == 0)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No favorites found for the given sender ID";
                    return serviceResponse;
                }
                var favoriteDtos = _mapper.Map<List<GetFavoriteDto>>(favorites);
                serviceResponse.Data = favoriteDtos;
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

            var favorites_Voyages = await _context.Favorites
                .Where(fv => fv.Type == "voyage" && fv.UserId == userId)
                .ToListAsync();

            var voyageIds = favorites_Voyages.Select(fv => fv.ItemId).ToList();

            var voyages = await _context.Voyages
                .Include(v => v.User)
                .Include(v => v.VoyageImages)
                .Include(v => v.Vehicle)
                .Where(v => voyageIds.Contains(v.Id))
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
                var bidDtos = _mapper.Map<List<VoyageBidDto>>(_context.Bids.Where(bid => bid.VoyageId == voyage.Id).ToList());
                var voyageDto = _mapper.Map<GetVoyageDto>(voyage);
                var waypointDtos = _mapper.Map<List<GetWaypointDto>>(_context.Waypoints.Where(w => w.VoyageId == voyage.Id).ToList());
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

        public async Task<ServiceResponse<List<GetVehicleDto>>> GetFavoriteVehiclesByUserId(string userId)
        {
            var serviceResponse = new ServiceResponse<List<GetVehicleDto>>();

            var favorites_Vehicles = await _context.Favorites
                .Where(fv => fv.Type == "vehicle" && fv.UserId == userId)
                .ToListAsync();

            var vehicleIds = favorites_Vehicles.Select(fv => fv.ItemId).ToList();

            var vehicles = await _context.Vehicles
                .Where(v => vehicleIds.Contains(v.Id))
                .ToListAsync();

            if (vehicles == null || vehicles.Count == 0)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "No vehicles found for the given user ID";
                return serviceResponse;
            }

            serviceResponse.Data = _mapper.Map<List<GetVehicleDto>>(vehicles);
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<int>>> GetFavoriteVehicleIdsByUserId(string userId)
        {
            var serviceResponse = new ServiceResponse<List<int>>();

            var favorites_Vehicles = await _context.Favorites
                .Where(fv => fv.Type == "vehicle" && fv.UserId == userId)
                .ToListAsync();
            var vehicleIds = favorites_Vehicles.Select(fv => fv.ItemId).ToList();
            if (vehicleIds == null || vehicleIds.Count == 0)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "No vehicles found for the given user ID";
                return serviceResponse;
            }

            serviceResponse.Data = vehicleIds;
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<int>>> GetFavoriteVoyageIdsByUserId(string userId)
        {
            var serviceResponse = new ServiceResponse<List<int>>();

            var favorites_Voyages = await _context.Favorites
                .Where(fv => fv.Type == "voyage" && fv.UserId == userId)
                .ToListAsync();
            var voyageIds = favorites_Voyages.Select(fv => fv.ItemId).ToList();
            if (voyageIds == null || voyageIds.Count == 0)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "No vehicles found for the given user ID";
                return serviceResponse;
            }

            serviceResponse.Data = voyageIds;
            return serviceResponse;
        }

    }
}