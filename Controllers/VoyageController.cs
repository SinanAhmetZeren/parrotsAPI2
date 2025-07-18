using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize]
    public class VoyageController : ControllerBase
    {
        private readonly IVoyageService _voyageService;

        public VoyageController(IVoyageService voyageService)
        {
            _voyageService = voyageService;
        }

        /*
        [HttpGet("GetAllVoyages")]
        public async Task<ActionResult<ServiceResponse<List<GetVoyageDto>>>> Get()
        {
            return Ok(await _voyageService.GetAllVoyages());
        }
        */

        [HttpGet("GetVoyageById/{id}")]
        public async Task<ActionResult<ServiceResponse<GetVoyageDto>>> GetSingle(int id)
        {
            return Ok(await _voyageService.GetVoyageById(id));
        }


        [HttpGet("GetVoyageByUserId/{userId}")]
        public async Task<ActionResult<ServiceResponse<List<GetVoyageDto>>>> GetVoyagesByUserId(string userId)
        {
            return Ok(await _voyageService.GetVoyagesByUserId(userId));
        }

        /*
        [HttpGet("GetVoyageByVehicleId/{vehicleId}")]
        public async Task<ActionResult<ServiceResponse<List<GetVoyageDto>>>> GetVoyagesByVehicleId(int vehicleId)
        {
            return Ok(await _voyageService.GetVoyagesByVehicleId(vehicleId));
        }
        */

        [HttpPost("AddVoyage")]
        public async Task<ActionResult<ServiceResponse<List<GetVoyageDto>>>> AddVoyage(AddVoyageDto newVoyage)
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

            if (requestUserId != newVoyage.UserId)
            {
                return Forbid();
            }

            return Ok(await _voyageService.AddVoyage(newVoyage));
        }


        [HttpPost("ConfirmVoyage/{voyageId}")]
        public async Task<IActionResult> ConfirmVoyage(int voyageId)
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

            var voyageResponse = await _voyageService.GetUnconfirmedVoyageById(voyageId);

            if (voyageResponse.Data == null)
            {
                return NotFound(voyageResponse);
            }
            if (requestUserId != voyageResponse.Data.UserId)
            {
                return Forbid();
            }
     
            var response = await _voyageService.ConfirmVoyage(voyageId);
            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

/*
        [HttpPut("UpdateVoyage")]
        public async Task<ActionResult<ServiceResponse<List<GetVoyageDto>>>> UpdateVoyage(UpdateVoyageDto updatedVoyage)
        {
            var response = await _voyageService.UpdateVoyage(updatedVoyage);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);
        }
*/


        [HttpPatch("PatchVoyage/{voyageId}")]
        public async Task<ActionResult<ServiceResponse<GetVoyageDto>>> PatchVoyage(
            int voyageId, JsonPatchDocument<UpdateVoyageDto> patchDoc)
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

            var voyageResponse = await _voyageService.GetVoyageById(voyageId);
            if (voyageResponse.Data == null)
            {
                return NotFound(voyageResponse);
            }
            if (requestUserId != voyageResponse.Data.UserId)
            {
                return Forbid();
            }

            var response = await _voyageService.PatchVoyage(voyageId, patchDoc, ModelState);

            if (response.Data == null)
            {
                return NotFound(response);
            }

            return Ok(response);
        }


        [HttpDelete("DeleteVoyage/{id}")]
        public async Task<ActionResult<ServiceResponse<GetVoyageDto>>> DeleteVoyage(int id)
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

            var voyageResponse = await _voyageService.GetVoyageById(id);
            if (voyageResponse.Data == null)
            {
                return NotFound(voyageResponse);
            }
            if (requestUserId != voyageResponse.Data.UserId)
            {
                return Forbid();
            }

            var response = await _voyageService.DeleteVoyage(id);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);

        }

        [HttpDelete("checkAndDeleteVoyage/{id}")]
        public async Task<ActionResult<ServiceResponse<GetVoyageDto>>> CheckAndDeleteVoyage(int id)
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

            var voyageResponse = await _voyageService.GetVoyageById(id);
            if (voyageResponse.Data == null)
            {
                return NotFound(voyageResponse);
            }
            if (requestUserId != voyageResponse.Data.UserId)
            {
                return Forbid();
            }

            var response = await _voyageService.CheckAndDeleteVoyage(id);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);

        }

        [Consumes("multipart/form-data")]
        [HttpPost("{voyageId}/updateProfileImage")]
        public async Task<ActionResult<ServiceResponse<GetVoyageDto>>> UpdateVoyageProfileImage(int voyageId, IFormFile imageFile)
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

            var voyageResponse = await _voyageService.GetVoyageById(voyageId);
            if (voyageResponse.Data == null)
            {
                return NotFound(voyageResponse);
            }
            if (requestUserId != voyageResponse.Data.UserId)
            {
                return Forbid();
            }

            var serviceResponse = await _voyageService.UpdateVoyageProfileImage(voyageId, imageFile);

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
        [HttpPost("{voyageId}/addVoyageImage")]
        public async Task<ActionResult<ServiceResponse<string>>> AddVoyageImage(int voyageId, IFormFile imageFile)
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

            var voyageResponse = await _voyageService.GetUnconfirmedVoyageById(voyageId);
            if (voyageResponse.Data == null)
            {
                return NotFound(voyageResponse);
            }
            if (requestUserId != voyageResponse.Data.UserId)
            {
                return Forbid();
            }

            var serviceResponse = await _voyageService.AddVoyageImage(voyageId, imageFile, userId: requestUserId);

            if (serviceResponse.Success)
            {
                return Ok(new { imagePath = serviceResponse.Data });
            }
            else
            {
                return BadRequest(new { message = serviceResponse.Message });
            }
        }

        [HttpDelete("{voyageImageId}/deleteVoyageImage")]
        public async Task<ActionResult<ServiceResponse<string>>> DeleteVoyageImage(int voyageImageId)
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

            var imageResponse = await _voyageService.GetVoyageImageById(voyageImageId);
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


            var serviceResponse = await _voyageService.DeleteVoyageImage(voyageImageId);
            if (serviceResponse.Success)
            {
                return Ok(new { message = "Voyage image deleted successfully", 
                    voyageDetails = serviceResponse.Data });
            }
            else
            {
                return BadRequest(new { message = serviceResponse.Message });
            }
        }

        [HttpGet("GetVoyagesByCoords/{lat1}/{lat2}/{lon1}/{lon2}")]
        public async Task<ActionResult<ServiceResponse<List<GetVoyageDto>>>> GetVoyagesByCoordinates(double lat1, double lat2, double lon1, double lon2)
        {
            return Ok(await _voyageService.GetVoyagesByCoordinates(lat1, lat2, lon1, lon2));
        }

        [HttpGet("GetVoyageIdsByCoords/{lat1}/{lat2}/{lon1}/{lon2}")]
        public async Task<ActionResult<ServiceResponse<List<GetVoyageDto>>>> GetVoyageIdsByCoordinates(double lat1, double lat2, double lon1, double lon2)
        {
            return Ok(await _voyageService.GetVoyageIdsByCoordinates(lat1, lat2, lon1, lon2));
        }

        [HttpGet("GetFilteredVoyages")]
        public async Task<ActionResult<ServiceResponse<List<GetVoyageDto>>>> GetFilteredVoyages(
            [FromQuery] double? lat1,
            [FromQuery] double? lat2,
            [FromQuery] double? lon1,
            [FromQuery] double? lon2,
            [FromQuery] int? vacancy,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] VehicleType? vehicleType)
        {
            try
            {
                var result = await _voyageService.GetFilteredVoyages(lat1, lat2, lon1, lon2, vacancy, vehicleType, startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

    }
}
