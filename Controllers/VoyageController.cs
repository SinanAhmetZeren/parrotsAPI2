using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VoyageController : ControllerBase
    {
        private readonly IVoyageService _voyageService;
        private readonly ILogger<VoyageController> _logger;

        public VoyageController(IVoyageService voyageService, ILogger<VoyageController> logger)
        {
            _voyageService = voyageService;
            _logger = logger;
        }



        [AllowAnonymous]
        [HttpGet("GetVoyageById/{id}")]
        public async Task<ActionResult<ServiceResponse<GetVoyageDto>>> GetSingle(int id)
        {
            return Ok(await _voyageService.GetVoyageById(id));
        }

        [HttpGet("GetVoyageByIdAdmin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ServiceResponse<GetVoyageDto>>> GetSingleAdmin(int id)
        {

            return Ok(await _voyageService.GetVoyageByIdAdmin(id));
        }



        [AllowAnonymous]
        [HttpGet("GetVoyageByUserId/{userId}")]
        public async Task<ActionResult<ServiceResponse<List<GetVoyageDto>>>> GetVoyagesByUserId(string userId)
        {
            return Ok(await _voyageService.GetVoyagesByUserId(userId));
        }



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

            return Ok(await _voyageService.AddVoyage(newVoyage, userId: requestUserId));
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



        [HttpPatch("PatchVoyageAdmin/{voyageId}")]
        [Authorize(Roles = "Admin")]

        public async Task<ActionResult<ServiceResponse<GetVoyageDto>>> PatchVoyage(
            int voyageId, JsonPatchDocument<UpdateVoyageDto> patchDoc)
        {


            var voyageResponse = await _voyageService.GetVoyageByIdAdmin(voyageId);
            if (voyageResponse.Data == null)
            {
                return NotFound(voyageResponse);
            }

            var response = await _voyageService.PatchVoyageAdmin(voyageId, patchDoc, ModelState);

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
            if (!IsValidImage(imageFile, out var imageError)) return imageError!;

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

            var serviceResponse = await _voyageService.UpdateVoyageProfileImage(voyageId, imageFile, userId: requestUserId);

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
            if (!IsValidImage(imageFile, out var imageError)) return imageError!;

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
                return Ok(new
                {
                    message = "Voyage image deleted successfully",
                    voyageDetails = serviceResponse.Data
                });
            }
            else
            {
                return BadRequest(new { message = serviceResponse.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("GetVoyagesByCoords/{lat1}/{lat2}/{lon1}/{lon2}")]
        public async Task<ActionResult<ServiceResponse<List<GetVoyageDto>>>> GetVoyagesByCoordinates(double lat1, double lat2, double lon1, double lon2)
        {
            return Ok(await _voyageService.GetVoyagesByCoordinates(lat1, lat2, lon1, lon2));
        }

        [AllowAnonymous]
        [HttpGet("GetVoyageIdsByCoords/{lat1}/{lat2}/{lon1}/{lon2}")]
        public async Task<ActionResult<ServiceResponse<List<GetVoyageDto>>>> GetVoyageIdsByCoordinates(double lat1, double lat2, double lon1, double lon2)
        {
            return Ok(await _voyageService.GetVoyageIdsByCoordinates(lat1, lat2, lon1, lon2));
        }

        [AllowAnonymous]
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
                _logger.LogError(ex, "GetFilteredVoyages failed");
                return BadRequest("An error occurred while filtering voyages.");
            }
        }


        private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

        private bool IsValidImage(IFormFile file, out ActionResult? error)
        {
            if (file == null || file.Length == 0)
            {
                error = BadRequest(new { message = "No image provided." });
                return false;
            }
            if (!AllowedImageTypes.Contains(file.ContentType.ToLower()))
            {
                error = BadRequest(new { message = "Invalid file type. Only JPEG, PNG, GIF, and WEBP are allowed." });
                return false;
            }
            error = null;
            return true;
        }


        [HttpPost("AddPlace")]
        public async Task<ActionResult<ServiceResponse<GetVoyageDto>>> AddPlace([FromBody] AddPlaceDto newPlace)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "User identity not found."
                });
            }
            return Ok(await _voyageService.AddPlace(newPlace, userId));
        }


    }
}
