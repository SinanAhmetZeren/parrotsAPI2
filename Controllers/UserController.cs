using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Services.User;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {

        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }



        [HttpGet("getUserById/{id}")]
        public async Task<ActionResult<ServiceResponse<GetUserDto>>> GetSingle(string id)
        {
            return Ok(await _userService.GetUserById(id));
        }

        [HttpGet("getUserByPublicId/{publicId}")]
        public async Task<ActionResult<ServiceResponse<GetUserDto>>> GetSingleWithPublicId(string publicId)
        {
            return Ok(await _userService.GetUserByPublicId(publicId));
        }


        [HttpPut("UpdateUser")]
        public async Task<ActionResult<ServiceResponse<List<GetUserDto>>>> UpdateUser(UpdateUserDto updatedUser)
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

            if (requestUserId != updatedUser.Id)
            {
                return Forbid();
            }

            var response = await _userService.UpdateUser(updatedUser);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);
        }


        [HttpPatch("PatchUser/{userId}")]
        public async Task<ActionResult<ServiceResponse<GetUserDto>>> UpdateUser(
            string userId, JsonPatchDocument<UpdateUserDto> patchDoc)
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

            if (requestUserId != userId)
            {
                return Forbid();
            }

            var response = await _userService.PatchUser(userId, patchDoc, ModelState);

            if (response.Data == null)
            {
                return NotFound(response);
            }

            return Ok(response);
        }


        [HttpPatch("PatchUserAdmin/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ServiceResponse<GetUserDto>>> UpdateUserAdmin(
            string userId, JsonPatchDocument<UpdateUserDto> patchDoc)
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

            var response = await _userService.PatchUser(userId, patchDoc, ModelState);

            if (response.Data == null)
            {
                return NotFound(response);
            }

            return Ok(response);
        }


        [Consumes("multipart/form-data")]
        [HttpPost("{userId}/updateProfileImage")]
        public async Task<ActionResult<ServiceResponse<GetUserDto>>> UpdateProfileImage(string userId, IFormFile imageFile)
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

            if (requestUserId != userId)
            {
                return Forbid();
            }

            var serviceResponse = await _userService.UpdateUserProfileImage(userId, imageFile);
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
        [HttpPost("{userId}/updateBackgroundImage")]
        public async Task<ActionResult<ServiceResponse<GetUserDto>>> UpdateBackgroundImage(string userId, IFormFile imageFile)
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

            if (requestUserId != userId)
            {
                return Forbid();
            }

            var serviceResponse = await _userService.UpdateUserBackgroundImage(userId, imageFile);

            if (serviceResponse.Success)
            {
                return Ok(new { imagePath = serviceResponse.Data });
            }
            else
            {
                return BadRequest(new { message = serviceResponse.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("searchUsers/{username}")]
        public async Task<ActionResult<ServiceResponse<List<UserDto>>>> GetUsersByUsername(string username)
        {
            return Ok(await _userService.GetUsersByUsername(username));
        }



        [HttpGet("singleUserByUsername/{username}")]
        [Authorize(Roles = "Admin")]

        public async Task<ActionResult<ServiceResponse<UserDto>>> GetSingleUserByUserName(string username)
        {

            // admin check 

            return Ok(await _userService.GetSingleUserByUsername(username));
        }


        [HttpPost("PurchaseCoins")]
        public async Task<ActionResult<ServiceResponse<GetUserDto>>> PurchaseCoins(UserDepositCoinsDto deposit)
        {
            // Get user ID from JWT token
            var requestUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (requestUserId == null)
            {
                return Unauthorized(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "User identity not found."
                });
            }

            // Ensure the request is for the current user
            if (requestUserId != deposit.UserId)
            {
                return Forbid();
            }

            // Call service to add coins and create CoinPurchase record
            var response = await _userService.PurchaseCoins(
                deposit.UserId,
                deposit.Coins,
                deposit.EurAmount,
                deposit.PaymentProviderId
                );

            if (response.Data == null)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("ClaimFreeCoins")]
        public async Task<ActionResult<ServiceResponse<int>>> ClaimFreeCoins()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new ServiceResponse<string> { Success = false, Message = "User identity not found." });
            }
            var response = await _userService.ClaimFreeCoins(userId);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost("SendParrotCoins")]
        public async Task<ActionResult<ServiceResponse<GetUserDto>>> SendParrotCoins(UserSendCoinsDto deposit)
        {
            // Get user ID from JWT token
            var requestUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (requestUserId == null)
            {
                return Unauthorized(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "User identity not found."
                });
            }

            // Ensure the request is for the current user
            if (requestUserId != deposit.UserId)
            {
                return Forbid();
            }

            // Call service to add coins and create CoinPurchase record
            var response = await _userService.SendParrotCoins(
                deposit.UserId,
                deposit.ReceiverId,
                deposit.Coins
                );

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }




        [HttpGet("parrotCoinBalance/{userId}")]
        public async Task<ActionResult<ServiceResponse<ParrotCoinSummaryDto>>> GetParrotCoinBalanceAndPurchases(string userId)
        {

            // Get user ID from JWT token
            var requestUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (requestUserId == null)
            {
                return Unauthorized(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "User identity not found."
                });
            }

            // Ensure the request is for the current user
            if (requestUserId != userId)
            {
                return Forbid();
            }

            return Ok(await _userService.GetParrotCoinBalanceAndPurchases(userId));
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


    }
}



