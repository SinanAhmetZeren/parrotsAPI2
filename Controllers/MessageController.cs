using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Dtos.MessageDtos;
using ParrotsAPI2.Dtos.WaypointDtos;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize]

    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }


        [HttpGet("getMessageByuserId/{userId}")]
        public async Task<ActionResult<ServiceResponse<List<GetMessageDto>>>> GetMessagesByUserId(string userId)
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

            return Ok(await _messageService.GetMessagesByUserId(userId));
        }

        [HttpGet("getMessagesBetweenUsers/{user1Id}/{user2Id}")]
        public async Task<ActionResult<ServiceResponse<List<GetMessageDto>>>> GetMessagesBetweenUsers(string user1Id, string user2Id)
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

            if (requestUserId != user1Id && requestUserId != user2Id)
            {
                return Forbid();
            }
            return Ok(await _messageService.GetMessagesBetweenUsers(user1Id, user2Id));
        }


    }
}
