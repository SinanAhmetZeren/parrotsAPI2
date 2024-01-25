using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Dtos.WaypointDtos;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WaypointController : ControllerBase
    {
        private readonly IWaypointService _waypointService;

        public WaypointController(IWaypointService waypointService)
        {
            _waypointService = waypointService;
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult<ServiceResponse<List<GetWaypointDto>>>> Get()
        {
            return Ok(await _waypointService.GetAllWaypoints());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceResponse<GetWaypointDto>>> GetSingle(int id)
        {
            return Ok(await _waypointService.GetWaypointById(id));
        }

        [HttpGet("voyageId/{voyageId}")]
        public async Task<ActionResult<ServiceResponse<List<GetWaypointDto>>>> GetWaypointsByVoyageId(int voyageId)
        {
            return Ok(await _waypointService.GetWaypointsByVoyageId(voyageId));
        }

        [HttpPost]
        public async Task<ActionResult<ServiceResponse<List<GetWaypointDto>>>> AddVoyage(AddWaypointDto newWaypoint)
        {

            return Ok(await _waypointService.AddWaypoint(newWaypoint));
        }


        [HttpDelete("{id}")]
        public async Task<ActionResult<ServiceResponse<GetVoyageDto>>> DeleteWaypoint(int id)
        {
            var response = await _waypointService.DeleteWaypoint(id);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);

        }

    }
}
