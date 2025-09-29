using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Dtos.BidDtos;
using ParrotsAPI2.Dtos.VoyageImageDtos;
using ParrotsAPI2.Dtos.WaypointDtos;
using ParrotsAPI2.Models;

namespace ParrotsAPI2.Services.Waypoint
{
    public class WaypointService : IWaypointService
    {

        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly ILogger<WaypointService> _logger;
        private readonly IBlobService _blobService; // 🟢 CHANGED

        // 🟢 CHANGED - Added BlobService to constructor
        public WaypointService(IMapper mapper, DataContext context, ILogger<WaypointService> logger, IBlobService blobService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _blobService = blobService;
        }

        // 🔹 Helper method for uploading images
        private async Task<string> UploadImageToBlobAsync(IFormFile file, string prefix)
        {
            const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

            if (file == null || file.Length == 0)
                throw new ArgumentException("No image provided");

            if (file.Length > MaxFileSize)
                throw new ArgumentException("Image size exceeds 5MB limit");

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

            var blobPath = string.IsNullOrEmpty(prefix)
                ? fileName
                : $"{prefix.TrimEnd('/')}/{fileName}";

            return await _blobService.UploadAsync(file.OpenReadStream(), blobPath);

        }

        public async Task<ServiceResponse<int>> AddWaypoint(AddWaypointDto newWaypoint, string userId)
        {
            var serviceResponse = new ServiceResponse<int>();
            try
            {
                if (newWaypoint == null || newWaypoint.ImageFile == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Waypoint or ImageFile is null.";
                    return serviceResponse;
                }
                // Add a folder-like prefix for organization
                var prefix = $"waypoint-images/{userId}";
                var uploadedFileName = await UploadImageToBlobAsync(newWaypoint.ImageFile, prefix);
                var waypoint = _mapper.Map<Models.Waypoint>(newWaypoint);
                waypoint.ProfileImage = uploadedFileName; // Use uploaded file name / blob URL
                waypoint.UserId = userId;
                _context.Waypoints.Add(waypoint);
                await _context.SaveChangesAsync();
                serviceResponse.Data = waypoint.Id;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error adding waypoint: {ex.Message}";
            }
            return serviceResponse;
        }



        public async Task<ServiceResponse<List<GetWaypointDto>>> DeleteWaypoint(int id)
        {
            var serviceResponse = new ServiceResponse<List<GetWaypointDto>>();
            try
            {
                var waypoint = await _context.Waypoints.FindAsync(id);
                if (waypoint == null)
                {
                    throw new Exception($"Waypoint with ID `{id}` not found");
                }
                if (!string.IsNullOrEmpty(waypoint.ProfileImage))
                {
                    await _blobService.DeleteAsync(waypoint.ProfileImage);
                }
                bool isOrderOne = waypoint.Order == 1;
                _context.Waypoints.Remove(waypoint);
                await _context.SaveChangesAsync();
                if (isOrderOne)
                {
                    var waypointsWithSameVoyageId = await _context.Waypoints
                        .Where(w => w.VoyageId == waypoint.VoyageId)
                        .OrderBy(w => w.Order)
                        .ToListAsync();

                    if (waypointsWithSameVoyageId.Any())
                    {
                        waypointsWithSameVoyageId.First().Order = 1;
                        await _context.SaveChangesAsync();
                    }
                }
                var waypoints = await _context.Waypoints.ToListAsync();
                serviceResponse.Data = waypoints.Select(c => _mapper.Map<GetWaypointDto>(c)).ToList();
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error deleting waypoint: {ex.Message}";
            }
            return serviceResponse;
        }



        public async Task<ServiceResponse<List<GetWaypointDto>>> GetAllWaypoints()
        {
            var serviceResponse = new ServiceResponse<List<GetWaypointDto>>();
            var dbWaypoints = await _context.Waypoints.ToListAsync();
            serviceResponse.Data = dbWaypoints.Select(c => _mapper.Map<GetWaypointDto>(c)).ToList();
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetWaypointDto>> GetWaypointById(int id)
        {
            var serviceResponse = new ServiceResponse<GetWaypointDto>();

            var waypoint = await _context.Waypoints
                .Include(w => w.Voyage)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (waypoint == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Waypoint not found";
                return serviceResponse;
            }

            var waypointDto = _mapper.Map<GetWaypointDto>(waypoint);
            serviceResponse.Data = waypointDto;

            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetWaypointDto>>> GetWaypointsByVoyageId(int voyageId)
        {
            var serviceResponse = new ServiceResponse<List<GetWaypointDto>>();

            var waypoints = await _context.Waypoints
                .Where(w => w.VoyageId == voyageId)
                .OrderBy(w => w.Order)
                .ToListAsync();

            if (waypoints == null || waypoints.Count == 0)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "No waypoints found for the given Voyage Id";
                return serviceResponse;
            }

            var waypointDtos = waypoints.Select(waypoint =>
                _mapper.Map<GetWaypointDto>(waypoint)).ToList();

            serviceResponse.Data = waypointDtos;

            return serviceResponse;
        }
        public async Task<ServiceResponse<List<GetWaypointDto>>> GetWaypointsByCoords(double lat1, double lon1, double lat2, double lon2)
        {
            var serviceResponse = new ServiceResponse<List<GetWaypointDto>>();
            try
            {
                var waypoints = await _context.Waypoints
                    .Where(w =>
                        w.Latitude >= lat1 &&
                        w.Latitude <= lat2 &&
                        w.Longitude >= lon1 &&
                        w.Longitude <= lon2 &&
                        w.Order == 1)
                    .ToListAsync();
                if (waypoints == null || waypoints.Count == 0)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No waypoints found with the specified conditions.";
                    return serviceResponse;
                }
                var waypointDtos = _mapper.Map<List<GetWaypointDto>>(waypoints);
                serviceResponse.Data = waypointDtos;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error retrieving waypoints: {ex.Message}";
            }
            return serviceResponse;
        }

    }
}
