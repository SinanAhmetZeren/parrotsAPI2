using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Dtos.GroupDtos;
using ParrotsAPI2.Services.Group;

namespace ParrotsAPI2.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
        {
            var result = await _groupService.CreateGroup(dto);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result.Data);
        }

        [HttpPost("{groupId}/add/{userId}")]
        public async Task<IActionResult> AddMember(int groupId, string userId, [FromQuery] string requesterId)
        {
            var result = await _groupService.AddMember(groupId, userId, requesterId);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result.Data);
        }

        [HttpDelete("{groupId}/remove/{userId}")]
        public async Task<IActionResult> RemoveMember(int groupId, string userId, [FromQuery] string requesterId)
        {
            var result = await _groupService.RemoveMember(groupId, userId, requesterId);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result.Data);
        }

        [HttpDelete("{groupId}/exit/{userId}")]
        public async Task<IActionResult> ExitGroup(int groupId, string userId)
        {
            var result = await _groupService.ExitGroup(groupId, userId);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result.Data);
        }

        [HttpGet("{groupId}")]
        public async Task<IActionResult> GetGroupById(int groupId, [FromQuery] string userId)
        {
            var result = await _groupService.GetGroupById(groupId, userId);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result.Data);
        }

        [HttpGet("{groupId}/messages/{userId}")]
        public async Task<IActionResult> GetGroupMessages(int groupId, string userId)
        {
            var result = await _groupService.GetGroupMessages(groupId, userId);
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result.Data);
        }
    }
}
