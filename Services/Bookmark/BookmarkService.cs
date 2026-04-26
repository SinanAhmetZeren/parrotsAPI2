using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Dtos.BookmarkDtos;

namespace ParrotsAPI2.Services.Bookmark
{
    public class BookmarkService : IBookmarkService
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;

        public BookmarkService(IMapper mapper, DataContext context)
        {
            _mapper = mapper;
            _context = context;
        }

        public async Task<ServiceResponse<List<GetBookmarkDto>>> GetBookmarks(string bookmarkerId)
        {
            var response = new ServiceResponse<List<GetBookmarkDto>>();
            var bookmarks = await _context.UserBookmarks
                .Where(b => b.BookmarkerId == bookmarkerId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var bookmarkedUserIds = bookmarks.Select(b => b.BookmarkedUserId).ToList();
            var users = await _context.Users
                .Where(u => bookmarkedUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            response.Data = bookmarks.Select(b =>
            {
                var dto = _mapper.Map<GetBookmarkDto>(b);
                if (users.TryGetValue(b.BookmarkedUserId, out var user))
                {
                    dto.UserName = user.UserName ?? string.Empty;
                    dto.ProfileImageUrl = user.ProfileImageUrl ?? string.Empty;
                    dto.ProfileImageThumbnailUrl = user.ProfileImageThumbnailUrl ?? string.Empty;
                    dto.PublicId = user.PublicId ?? string.Empty;
                }
                return dto;
            }).ToList();

            return response;
        }

        public async Task<ServiceResponse<List<string>>> GetBookmarkedUserIds(string bookmarkerId)
        {
            var response = new ServiceResponse<List<string>>();
            response.Data = await _context.UserBookmarks
                .Where(b => b.BookmarkerId == bookmarkerId)
                .Select(b => b.BookmarkedUserId)
                .ToListAsync();
            return response;
        }

        public async Task<ServiceResponse<GetBookmarkDto>> AddBookmark(string bookmarkerId, string bookmarkedUserId)
        {
            var response = new ServiceResponse<GetBookmarkDto>();

            if (bookmarkerId == bookmarkedUserId)
            {
                response.Success = false;
                response.Message = "Cannot bookmark yourself.";
                return response;
            }

            var exists = await _context.UserBookmarks
                .AnyAsync(b => b.BookmarkerId == bookmarkerId && b.BookmarkedUserId == bookmarkedUserId);

            if (exists)
            {
                response.Success = false;
                response.Message = "Already bookmarked.";
                return response;
            }

            var bookmarkedUser = await _context.Users.FindAsync(bookmarkedUserId);
            if (bookmarkedUser == null)
            {
                response.Success = false;
                response.Message = "User not found.";
                return response;
            }

            var bookmark = new UserBookmark
            {
                BookmarkerId = bookmarkerId,
                BookmarkedUserId = bookmarkedUserId,
            };

            _context.UserBookmarks.Add(bookmark);
            await _context.SaveChangesAsync();

            var dto = _mapper.Map<GetBookmarkDto>(bookmark);
            dto.UserName = bookmarkedUser.UserName ?? string.Empty;
            dto.ProfileImageUrl = bookmarkedUser.ProfileImageUrl ?? string.Empty;
            dto.ProfileImageThumbnailUrl = bookmarkedUser.ProfileImageThumbnailUrl ?? string.Empty;
            dto.PublicId = bookmarkedUser.PublicId ?? string.Empty;

            response.Data = dto;
            return response;
        }

        public async Task<ServiceResponse<string>> RemoveBookmark(string bookmarkerId, string bookmarkedUserId)
        {
            var response = new ServiceResponse<string>();

            var bookmark = await _context.UserBookmarks
                .FirstOrDefaultAsync(b => b.BookmarkerId == bookmarkerId && b.BookmarkedUserId == bookmarkedUserId);

            if (bookmark == null)
            {
                response.Success = false;
                response.Message = "Bookmark not found.";
                return response;
            }

            _context.UserBookmarks.Remove(bookmark);
            await _context.SaveChangesAsync();

            response.Data = bookmarkedUserId;
            return response;
        }

    }
}
