using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Dtos.WaypointDtos;
using Microsoft.AspNetCore.Authorization;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class WaypointController : ControllerBase
    {
        private readonly IWaypointService _waypointService;

        public WaypointController(IWaypointService waypointService)
        {
            _waypointService = waypointService;
        }

        [HttpGet("GetAllWaypoints")]
        public async Task<ActionResult<ServiceResponse<List<GetWaypointDto>>>> Get()
        {
            return Ok(await _waypointService.GetAllWaypoints());
        }

        [HttpGet("GetWaypoint/{id}")]
        public async Task<ActionResult<ServiceResponse<GetWaypointDto>>> GetSingle(int id)
        {
            return Ok(await _waypointService.GetWaypointById(id));
        }

        [HttpGet("GetWaypointByVoyageId/{voyageId}")]
        public async Task<ActionResult<ServiceResponse<List<GetWaypointDto>>>> GetWaypointsByVoyageId(int voyageId)
        {
            return Ok(await _waypointService.GetWaypointsByVoyageId(voyageId));
        }

        [HttpPost("AddWaypoint")]
        public async Task<ActionResult<ServiceResponse<List<GetWaypointDto>>>> AddWaypoint(AddWaypointDto newWaypoint)
        {

            return Ok(await _waypointService.AddWaypoint(newWaypoint));
        }

        [HttpDelete("DeleteWaypoint/{id}")]
        public async Task<ActionResult<ServiceResponse<GetVoyageDto>>> DeleteWaypoint(int id)
        {
            var response = await _waypointService.DeleteWaypoint(id);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);

        }

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
    }
}
