using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Dtos.MessageDtos;
using ParrotsAPI2.Dtos.WaypointDtos;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }


        [HttpGet("GetAll")]
        public async Task<ActionResult<ServiceResponse<List<GetMessageDto>>>> Get()
        {
            return Ok(await _messageService.GetAllMessages());
        }


        [HttpGet("getsingleMessage/{id}")]
        public async Task<ActionResult<ServiceResponse<GetMessageDto>>> GetSingle(int id)
        {
            return Ok(await _messageService.GetMessageById(id));
        }


        [HttpGet("getMessageByuserId/{userId}")]
        public async Task<ActionResult<ServiceResponse<List<GetMessageDto>>>> GetMessagesByUserId(string userId)
        {
            return Ok(await _messageService.GetMessagesByUserId(userId));
        }


        [HttpGet("getMessageBysenderId/{senderId}")]
        public async Task<ActionResult<ServiceResponse<List<GetMessageDto>>>> GetMessagesBySenderId(string senderId)
        {
            return Ok(await _messageService.GetMessagesBySenderId(senderId));
        }


        [HttpGet("getMessageByreceiverId/{receiverId}")]
        public async Task<ActionResult<ServiceResponse<List<GetMessageDto>>>> GetMessagesByReceiverId(string receiverId)
        {
            return Ok(await _messageService.GetMessagesByReceiverId(receiverId));
        }


        [HttpPost("addMessage")]
        public async Task<ActionResult<ServiceResponse<List<GetMessageDto>>>> AddMessage(AddMessageDto newMessage)
        {

            return Ok(await _messageService.AddMessage(newMessage));
        }

        [HttpDelete("deleteMessage/{id}")]
        public async Task<ActionResult<ServiceResponse<GetVoyageDto>>> DeleteMessage(int id)
        {
            var response = await _messageService.DeleteMessage(id);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);

        }

        [HttpPut("updateMessage")]
        public async Task<ActionResult<ServiceResponse<List<GetVehicleDto>>>> UpdateMessage(UpdateMessageDto updatedMessage)
        {
            var response = await _messageService.UpdateMessage(updatedMessage);
            if (response.Data == null)
            {
                return NotFound(response);
            }
            return Ok(response);
        }
    }
}
