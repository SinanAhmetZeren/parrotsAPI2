using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Dtos.WaypointDtos;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize]

    public class WaypointController : ControllerBase
    {
        private readonly IWaypointService _waypointService;
        private readonly IVoyageService _voyageService;

        public WaypointController(IWaypointService waypointService, IVoyageService voyageService)
        {
            _waypointService = waypointService;
            _voyageService = voyageService;
        }

/*
        [HttpGet("GetAllWaypoints")]
        public async Task<ActionResult<ServiceResponse<List<GetWaypointDto>>>> Get()
        {
            return Ok(await _waypointService.GetAllWaypoints());
        }
*/

/*
        [HttpGet("GetWaypoint/{id}")]
        public async Task<ActionResult<ServiceResponse<GetWaypointDto>>> GetSingle(int id)
        {
            return Ok(await _waypointService.GetWaypointById(id));
        }
*/

/*
        [HttpGet("GetWaypointByVoyageId/{voyageId}")]
        public async Task<ActionResult<ServiceResponse<List<GetWaypointDto>>>> GetWaypointsByVoyageId(int voyageId)
        {
            return Ok(await _waypointService.GetWaypointsByVoyageId(voyageId));
        }
*/

        [HttpPost("AddWaypoint")]
        public async Task<ActionResult<ServiceResponse<List<GetWaypointDto>>>> AddWaypoint(AddWaypointDto newWaypoint)
        {

            var requestUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (requestUserId == null)
            {
                return Unauthorized(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "User identity not found."
                });
            }
            var voyageResponse = await _voyageService.GetUnconfirmedVoyageById(newWaypoint.VoyageId);
            if (voyageResponse == null || voyageResponse.Data == null)
            {
                return NotFound(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Image not found."
                });
            }
            if (voyageResponse.Data?.UserId != requestUserId)
            {
                return Forbid();
            }
            return Ok(await _waypointService.AddWaypoint(newWaypoint, userId: requestUserId));
        }


        [HttpDelete("DeleteWaypoint/{id}")]
        public async Task<ActionResult<ServiceResponse<GetVoyageDto>>> DeleteWaypoint(int id)
        {
            var requestUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (requestUserId == null)
            {
                return Unauthorized(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "User identity not found."
                });
            }
            var waypointResponse = await _waypointService.GetWaypointById(id);
            if (waypointResponse == null || waypointResponse.Data == null)
            {
                return NotFound(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Image not found."
                });
            }
            if (waypointResponse.Data?.UserId != requestUserId)
            {
                return Forbid();
            }
            var response = await _waypointService.DeleteWaypoint(id);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);

        }

        /*
        [HttpGet("getWaypointsByCoords")]
        public async Task<IActionResult> GetWaypointsByCoords(double lat1, double lon1, double lat2, double lon2)
        {
            var serviceResponse = await _waypointService.GetWaypointsByCoords(lat1, lon1, lat2, lon2);

            if (serviceResponse.Success)
            {
                return Ok(serviceResponse.Data);
            }

            return BadRequest(serviceResponse.Message);
        }
        */
    }
}
