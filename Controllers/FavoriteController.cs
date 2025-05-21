using Microsoft.AspNetCore.Http; 
using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Dtos.FavoriteDtos;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class FavoriteController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;

        public FavoriteController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

/*
        [HttpGet("getFavoritesByUserId/{userId}")]
        public async Task<ActionResult<ServiceResponse<List<GetFavoriteDto>>>> GetFavoritesByUserId(string userId)
        {
            return Ok(await _favoriteService.GetFavoritesByUserId(userId));
        }
*/

        [HttpGet("getFavoriteVoyagesByUserId/{userId}")]
        public async Task<ActionResult<ServiceResponse<List<GetFavoriteDto>>>> GetFavoriteVoyagesByUserId(string userId)
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
            if (userId != requestUserId)
            {
                return Forbid();
            }
            return Ok(await _favoriteService.GetFavoriteVoyagesByUserId(userId));
        }


        [HttpGet("getFavoriteVehiclesByUserId/{userId}")]
        public async Task<ActionResult<ServiceResponse<List<GetFavoriteDto>>>> GetFavoriteVehiclesByUserId(string userId)
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
            if (userId != requestUserId)
            {
                return Forbid();
            }
            return Ok(await _favoriteService.GetFavoriteVehiclesByUserId(userId));
        }


        [HttpGet("getFavoriteVehicleIdsByUserId/{userId}")]
        public async Task<ActionResult<ServiceResponse<List<int>>>> GetFavoriteVehicleIdsByUserId(string userId)
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
            if (userId != requestUserId)
            {
                return Forbid();
            }
            return Ok(await _favoriteService.GetFavoriteVehicleIdsByUserId(userId));
        }


        [HttpGet("getFavoriteVoyageIdsByUserId/{userId}")]
        public async Task<ActionResult<ServiceResponse<List<int>>>> GetFavoriteVoyageIdsByUserId(string userId)
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
            if (userId != requestUserId)
            {
                return Forbid();
            }
            return Ok(await _favoriteService.GetFavoriteVoyageIdsByUserId(userId));
        }

        [HttpPost("addFavorite")]
        public async Task<ActionResult<ServiceResponse<List<GetFavoriteDto>>>> AddFavorite(AddFavoriteDto newFavorite)
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
            if (newFavorite.UserId != requestUserId)
            {
                return Forbid();
            }

            return Ok(await _favoriteService.AddFavorite(newFavorite));
        }

        [HttpDelete("deleteFavoriteVoyage/{userId}/{voyageId}")]
        public async Task<ActionResult<ServiceResponse<string>>> DeleteFavoriteVoyage(string userId, int voyageId)
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
            if (userId != requestUserId)
            {
                return Forbid();
            }

            var response = await _favoriteService.DeleteFavoriteVoyage(userId, voyageId);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);

        }

        [HttpDelete("deleteFavoriteVehicle/{userId}/{vehicleId}")]
        public async Task<ActionResult<ServiceResponse<string>>> DeleteFavoriteVehicle(string userId, int vehicleId)
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
            if (userId != requestUserId)
            {
                return Forbid();
            }

            var response = await _favoriteService.DeleteFavoriteVehicle(userId, vehicleId);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);

        }


    }
}
