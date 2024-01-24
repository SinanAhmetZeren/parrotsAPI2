using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Services.User;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {

        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult<ServiceResponse<List<GetUserDto>>>> Get()
        {
            return Ok(await _userService.GetAllUsers());
        }



        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceResponse<GetUserDto>>> GetSingle(int id)
        {
            return Ok(await _userService.GetUserById(id));
        }



        [HttpPost]
        public async Task<ActionResult<ServiceResponse<List<GetUserDto>>>> AddUser(AddUserDto newUser)
        {

            return Ok(await _userService.AddUser(newUser));
        }


        [HttpPut]
        public async Task<ActionResult<ServiceResponse<List<GetUserDto>>>> UpdateUser(UpdateUserDto updatedUser)
        {
            var response = await _userService.UpdateUser(updatedUser);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);
        }



        [HttpPatch("{userId}")]
        public async Task<ActionResult<ServiceResponse<GetUserDto>>> UpdateUser(
            int userId, JsonPatchDocument<UpdateUserDto> patchDoc)
        {
            var response = await _userService.PatchUser(userId, patchDoc, ModelState);

            if (response.Data == null)
            {
                return NotFound(response);
            }

            return Ok(response);
        }




        [HttpDelete("{id}")]
        public async Task<ActionResult<ServiceResponse<GetUserDto>>> DeleteUser(int id)
        {
            var response = await _userService.DeleteUser(id);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);

        }


        [Consumes("multipart/form-data")]
        [HttpPost("{userId}/updateProfileImage")]
        public async Task<ActionResult<ServiceResponse<GetUserDto>>> UpdateProfileImage(int userId, IFormFile imageFile)
        {
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

    }
}



