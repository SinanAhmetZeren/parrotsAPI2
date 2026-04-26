using ParrotsAPI2.Dtos.BookmarkDtos;

namespace ParrotsAPI2.Services.Bookmark
{
    public interface IBookmarkService
    {
        Task<ServiceResponse<List<GetBookmarkDto>>> GetBookmarks(string bookmarkerId);
        Task<ServiceResponse<List<string>>> GetBookmarkedUserIds(string bookmarkerId);
        Task<ServiceResponse<GetBookmarkDto>> AddBookmark(string bookmarkerId, string bookmarkedUserId);
        Task<ServiceResponse<string>> RemoveBookmark(string bookmarkerId, string bookmarkedUserId);
    }
}
