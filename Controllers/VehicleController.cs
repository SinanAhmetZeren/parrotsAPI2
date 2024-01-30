using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Dtos.VehicleDtos;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public VehicleController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        [HttpGet("GetAllVehicles")]
        public async Task<ActionResult<ServiceResponse<List<GetVehicleDto>>>> Get()
        {
            return Ok(await _vehicleService.GetAllVehicles());
        }


        [HttpGet("GetVehicleById/{id}")]
        public async Task<ActionResult<ServiceResponse<GetVehicleDto>>> GetSingle(int id)
        {
            return Ok(await _vehicleService.GetVehicleById(id));
        }

        [HttpGet("GetVehiclesByUserId/{userId}")]
        public async Task<ActionResult<ServiceResponse<List<GetVehicleDto>>>> GetVehiclesByUserId(string userId)
        {
            return Ok(await _vehicleService.GetVehiclesByUserId(userId));
        }

        [HttpPost("addVehicle")]
        public async Task<ActionResult<ServiceResponse<List<GetVehicleDto>>>> AddVehicle(AddVehicleDto newVehicle)
        {

            return Ok(await _vehicleService.AddVehicle(newVehicle));
        }

        [HttpPut("UpdateVehicle")]
        public async Task<ActionResult<ServiceResponse<List<GetVehicleDto>>>> UpdateVehicle(UpdateVehicleDto updatedVehicle)
        {
            var response = await _vehicleService.UpdateVehicle(updatedVehicle);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpPatch("PatchVehicle/{vehicleId}")]
        public async Task<ActionResult<ServiceResponse<GetVehicleDto>>> PatchVehicle(
            int vehicleId, JsonPatchDocument<UpdateVehicleDto> patchDoc)
        {
            var response = await _vehicleService.PatchVehicle(vehicleId, patchDoc, ModelState);

            if (response.Data == null)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpDelete("DeleteVehicle/{id}")]
        public async Task<ActionResult<ServiceResponse<GetVehicleDto>>> DeleteVehicle(int id)
        {
            var response = await _vehicleService.DeleteVehicle(id);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);

        }

        [Consumes("multipart/form-data")]
        [HttpPost("{vehicleId}/updateProfileImage")]
        public async Task<ActionResult<ServiceResponse<GetVehicleDto>>> UpdateVehicleProfileImage(int vehicleId, IFormFile imageFile)
        {
            var serviceResponse = await _vehicleService.UpdateVehicleProfileImage(vehicleId, imageFile);

            if (serviceResponse.Success)
            {
                return Ok(new { imagePath = serviceResponse.Data });
            }
            else
            {
                return BadRequest(new { message = serviceResponse.Message });
            }
        }

        [Consumes("multipart/form-data")]
        [HttpPost("{vehicleId}/addVehicleImage")]
        public async Task<ActionResult<ServiceResponse<GetVehicleDto>>> AddVehicleImage(int vehicleId, IFormFile imageFile)
        {
            var serviceResponse = await _vehicleService.AddVehicleImage(vehicleId, imageFile);

            if (serviceResponse.Success)
            {
                return Ok(new { imagePath = serviceResponse.Data });
            }
            else
            {
                return BadRequest(new { message = serviceResponse.Message });
            }
        }

        
    }
}
