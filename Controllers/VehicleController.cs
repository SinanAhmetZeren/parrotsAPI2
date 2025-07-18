using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Dtos.VehicleDtos;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize]
    
        public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public VehicleController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

/*
        [HttpGet("GetAllVehicles")]
        public async Task<ActionResult<ServiceResponse<List<GetVehicleDto>>>> Get()
        {
            return Ok(await _vehicleService.GetAllVehicles());
        }
*/

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


        [HttpGet("GetVehiclesImagesByVehicleId/{vehicleId}")]
        public async Task<ActionResult<ServiceResponse<List<GetVehicleDto>>>> GetVehicleImagesByVehicleId(int vehicleId)
        {
            return Ok(await _vehicleService.GetVehicleImagesByVehicleId(vehicleId));
        }

        [HttpPost("addVehicle")]
        public async Task<ActionResult<ServiceResponse<List<GetVehicleDto>>>> AddVehicle(AddVehicleDto newVehicle)
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
            if (newVehicle.UserId != requestUserId)
            {
                return Forbid(); 
            }

            return Ok(await _vehicleService.AddVehicle(newVehicle));
        }

        [HttpPost("confirmVehicle/{vehicleId}")]
        public async Task<ActionResult<ServiceResponse<List<GetVehicleDto>>>> ConfirmVehicle(int vehicleId)
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

            // Fetch the vehicle (assuming you have a method for this)
            var vehicle = await _vehicleService.GetUnconfirmedVehicleById(vehicleId);
            if (vehicle == null)
            {
                return NotFound(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Vehicle not found."
                });
            }

            // Check if the vehicle belongs to the current user
            if (vehicle?.Data?.UserId != requestUserId)
            {
                return Forbid();
            }

            return Ok(await _vehicleService.ConfirmVehicle(vehicleId));
        }

/*
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
*/

        [HttpPatch("PatchVehicle/{vehicleId}")]
        public async Task<ActionResult<ServiceResponse<GetVehicleDto>>> PatchVehicle(
            int vehicleId, JsonPatchDocument<UpdateVehicleDto> patchDoc)
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

            var vehicle = await _vehicleService.GetVehicleById(vehicleId);
            if (vehicle == null)
            {
                return NotFound(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Vehicle not found."
                });
            }

            if (vehicle?.Data?.UserId != requestUserId)
            {
                return Forbid();
            }


            var response = await _vehicleService.PatchVehicle(vehicleId, patchDoc, ModelState);

            if (response.Data == null)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpDelete("DeleteVehicle/{id}")]
        public async Task<ActionResult<ServiceResponse<string>>> DeleteVehicle(int id)
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

            var vehicle = await _vehicleService.GetVehicleById(id);
            if (vehicle == null)
            {
                return NotFound(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Vehicle not found."
                });
            }

            if (vehicle?.Data?.UserId != requestUserId)
            {
                return Forbid();
            }


            var response = await _vehicleService.DeleteVehicle(id);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);

        }

        [HttpDelete("checkAndDeleteVehicle/{id}")]
        public async Task<ActionResult<ServiceResponse<string>>> CheckAndDeleteVehicle(int id)
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

            var vehicle = await _vehicleService.GetVehicleById(id);
            if (vehicle == null)
            {
                return NotFound(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Vehicle not found."
                });
            }

            if (vehicle?.Data?.UserId != requestUserId)
            {
                return Forbid();
            }


            var response = await _vehicleService.CheckAndDeleteVehicle(id);
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
            var requestUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (requestUserId == null)
            {
                return Unauthorized(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "User identity not found."
                });
            }

            var vehicle = await _vehicleService.GetVehicleById(vehicleId);
            if (vehicle == null)
            {
                return NotFound(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Vehicle not found."
                });
            }

            if (vehicle?.Data?.UserId != requestUserId)
            {
                return Forbid();
            }

            var serviceResponse = await _vehicleService.UpdateVehicleProfileImage(vehicleId, imageFile );
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


            var requestUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (requestUserId == null)
            {
                return Unauthorized(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "User identity not found."
                });
            }

            var vehicle = await _vehicleService.GetUnconfirmedVehicleById(vehicleId);
            if (vehicle == null)
            {
                return NotFound(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Vehicle not found."
                });
            }

            if (vehicle?.Data?.UserId != requestUserId)
            {
                return Forbid();
            }
            var serviceResponse = await _vehicleService.AddVehicleImage(vehicleId, imageFile, userId: requestUserId);
            if (serviceResponse.Success)
            {
                return Ok(new { imagePath = serviceResponse.Data });
            }
            else
            {
                return BadRequest(new { message = serviceResponse.Message });
            }
        }

        [HttpDelete("DeleteVehicleImage/{imageId}")]
        public async Task<ActionResult<ServiceResponse<GetVehicleDto>>> DeleteVehicleImage(int imageId)
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

            var imageResponse = await _vehicleService.GetVehicleImageById(imageId);
            if (imageResponse == null || imageResponse.Data == null)
            {
                return NotFound(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Image not found."
                });
            }

            if (imageResponse.Data?.UserId != requestUserId)
            {
                return Forbid();
            }

            var response = await _vehicleService.DeleteVehicleImage(imageId);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);

        }

    }
}
