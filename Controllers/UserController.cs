using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Services.User;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize]
    [AllowAnonymous] // Uncomment this line to allow anonymous access

    public class UserController : ControllerBase
    {

        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }



        [AllowAnonymous]
        [HttpGet("getUserById/{id}")]
        public async Task<ActionResult<ServiceResponse<GetUserDto>>> GetSingle(string id)
        {
            /*
                        var requestUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (requestUserId == null)
                        {
                            return Unauthorized(new ServiceResponse<string>
                            {
                                Success = false,
                                Message = "User identity not found."
                            });
                        }
            */
            return Ok(await _userService.GetUserById(id));
        }

        [AllowAnonymous]
        [HttpGet("getUserByPublicId/{publicId}")]
        public async Task<ActionResult<ServiceResponse<GetUserDto>>> GetSingleWithPublicId(string publicId)
        {
            /*
                        var requestUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (requestUserId == null)
                        {
                            return Unauthorized(new ServiceResponse<string>
                            {
                                Success = false,
                                Message = "User identity not found."
                            });
                        }
            */
            return Ok(await _userService.GetUserByPublicId(publicId));
        }


        /*
                [HttpPost("AddUser")]
                [AllowAnonymous]
                public async Task<ActionResult<ServiceResponse<List<GetUserDto>>>> AddUser(AddUserDto newUser)
                {

                    return Ok(await _userService.AddUser(newUser));
                }
        */
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

        /*
                [HttpPut("updateUnseen")]
                public async Task<ActionResult<ServiceResponse<List<GetUserDto>>>> UpdateUserUnseenMessage(UpdateUserUnseenMessageDto updatedUser)
                {
                    var response = await _userService.UpdateUserUnseenMessage(updatedUser);
                    if (response.Data == null)
                    {
                        return NotFound(response);
                    }
                    return Ok(response);
                }
        */



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





        [Consumes("multipart/form-data")]
        [HttpPost("{userId}/updateProfileImage")]
        public async Task<ActionResult<ServiceResponse<GetUserDto>>> UpdateProfileImage(string userId, IFormFile imageFile)
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

        [HttpGet("searchUsers/{username}")]
        public async Task<ActionResult<ServiceResponse<List<UserDto>>>> GetUsersByUsername(string username)
        {
            return Ok(await _userService.GetUsersByUsername(username));
        }

    }
}



