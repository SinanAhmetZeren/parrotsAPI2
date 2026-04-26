using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Dtos.BookmarkDtos;
using ParrotsAPI2.Services.Bookmark;
using System.Security.Claims;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BookmarkController : ControllerBase
    {
        private readonly IBookmarkService _bookmarkService;

        public BookmarkController(IBookmarkService bookmarkService)
        {
            _bookmarkService = bookmarkService;
        }

        [HttpGet("getBookmarks")]
        public async Task<ActionResult<ServiceResponse<List<GetBookmarkDto>>>> GetBookmarks()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            return Ok(await _bookmarkService.GetBookmarks(userId));
        }

        [HttpGet("getBookmarkedUserIds")]
        public async Task<ActionResult<ServiceResponse<List<string>>>> GetBookmarkedUserIds()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            return Ok(await _bookmarkService.GetBookmarkedUserIds(userId));
        }

        [HttpPost("addBookmark/{bookmarkedUserId}")]
        public async Task<ActionResult<ServiceResponse<GetBookmarkDto>>> AddBookmark(string bookmarkedUserId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            return Ok(await _bookmarkService.AddBookmark(userId, bookmarkedUserId));
        }

        [HttpDelete("removeBookmark/{bookmarkedUserId}")]
        public async Task<ActionResult<ServiceResponse<string>>> RemoveBookmark(string bookmarkedUserId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            var response = await _bookmarkService.RemoveBookmark(userId, bookmarkedUserId);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
    }
}
