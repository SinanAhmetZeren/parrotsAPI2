using Microsoft.AspNetCore.Mvc.ModelBinding;
using ParrotsAPI2.Dtos.WaypointDtos;

namespace ParrotsAPI2.Services.Waypoint
{
    public interface IWaypointService
    {
        Task<ServiceResponse<List<GetWaypointDto>>> GetAllWaypoints();
        Task<ServiceResponse<GetWaypointDto>> GetWaypointById(int id);
        Task<ServiceResponse<List<GetWaypointDto>>> GetWaypointsByVoyageId(int voyageId);
        Task<ServiceResponse<List<GetWaypointDto>>> AddWaypoint(AddWaypointDto newWaypoint);
        Task<ServiceResponse<List<GetWaypointDto>>> DeleteWaypoint(int id);

    }
}
