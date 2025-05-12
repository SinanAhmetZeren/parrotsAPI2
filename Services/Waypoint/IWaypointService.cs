using Microsoft.AspNetCore.Mvc.ModelBinding;
using ParrotsAPI2.Dtos.WaypointDtos;

namespace ParrotsAPI2.Services.Waypoint
{
    public interface IWaypointService
    {
        Task<ServiceResponse<List<GetWaypointDto>>> GetAllWaypoints();
        Task<ServiceResponse<GetWaypointDto>> GetWaypointById(int id);
        Task<ServiceResponse<List<GetWaypointDto>>> GetWaypointsByVoyageId(int voyageId);
        Task<ServiceResponse<int>> AddWaypoint(AddWaypointDto newWaypoint);
        Task<ServiceResponse<List<GetWaypointDto>>> DeleteWaypoint(int id);
        Task<ServiceResponse<List<GetWaypointDto>>> GetWaypointsByCoords(double lat1, double lon1, double lat2, double lon2);
    }
}
