using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Dtos.BidDtos;
using ParrotsAPI2.Dtos.VoyageImageDtos;
using ParrotsAPI2.Dtos.WaypointDtos;
using ParrotsAPI2.Models;

namespace ParrotsAPI2.Services.Waypoint
{
    public class WaypointService2 : IWaypointService
    {

        private readonly IMapper _mapper;
        private readonly DataContext _context;

        public WaypointService2(IMapper mapper, DataContext context)
        {
            _context = context;
            _mapper = mapper;
        }




        public async Task<ServiceResponse<int>> AddWaypoint(AddWaypointDto newWaypoint, string userId)
        {
            var serviceResponse = new ServiceResponse<int>();

            // Check if input is valid
            if (newWaypoint == null || newWaypoint.ImageFile == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Waypoint or ImageFile is null.";
                return serviceResponse;
            }

            // Prepare file name and path
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(newWaypoint.ImageFile.FileName);
            var filePath = Path.Combine("Uploads/WaypointImages/", fileName);

            try
            {
                // Ensure the directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newWaypoint.ImageFile.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error saving image: {ex.Message}";
                return serviceResponse;
            }

            // Map DTO to entity and save to DB
            var waypoint = _mapper.Map<Models.Waypoint>(newWaypoint);
            waypoint.ProfileImage = fileName;
            waypoint.UserId = userId;

            _context.Waypoints.Add(waypoint);
            await _context.SaveChangesAsync();

            serviceResponse.Data = waypoint.Id;
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

                // Check if the deleted waypoint has order = 1
                bool isOrderOne = waypoint.Order == 1;

                _context.Waypoints.Remove(waypoint);
                await _context.SaveChangesAsync();

                if (isOrderOne)
                {
                    // Find other waypoints with the same voyageId
                    var waypointsWithSameVoyageId = await _context.Waypoints
                        .Where(w => w.VoyageId == waypoint.VoyageId)
                        .OrderBy(w => w.Order)
                        .ToListAsync();

                    if (waypointsWithSameVoyageId.Any())
                    {
                        // Update the waypoint with the smallest order to order = 1
                        waypointsWithSameVoyageId.First().Order = 1;
                    }

                    await _context.SaveChangesAsync();
                }

                var voyages = await _context.Waypoints.ToListAsync();
                serviceResponse.Data = voyages.Select(c => _mapper.Map<GetWaypointDto>(c)).ToList();
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
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
