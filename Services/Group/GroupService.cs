using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Data;
using ParrotsAPI2.Dtos.GroupDtos;
using ParrotsAPI2.Helpers;
using ParrotsAPI2.Models;
using ParrotsAPI2.Services.Notifications;

namespace ParrotsAPI2.Services.Group
{
    public class GroupService : IGroupService
    {
        private readonly DataContext _context;
        private readonly ILogger<GroupService> _logger;
        private readonly ExpoPushService _expoPush;

        public GroupService(DataContext context, ILogger<GroupService> logger, ExpoPushService expoPush)
        {
            _context = context;
            _logger = logger;
            _expoPush = expoPush;
        }

        public async Task<ServiceResponse<GetGroupDto>> CreateGroup(CreateGroupDto dto)
        {
            var response = new ServiceResponse<GetGroupDto>();
            try
            {
                var keyBytes = new byte[32];
                System.Security.Cryptography.RandomNumberGenerator.Fill(keyBytes);
                var encryptionKey = Convert.ToBase64String(keyBytes);

                var group = new GroupConversation
                {
                    Name = dto.Name,
                    CreatorId = dto.CreatorId,
                    EncryptionKey = encryptionKey,
                    CreatedAt = DateTime.UtcNow
                };

                _context.GroupConversations.Add(group);
                await _context.SaveChangesAsync();

                // Add creator as first member
                var member = new GroupMember
                {
                    GroupConversationId = group.Id,
                    UserId = dto.CreatorId
                };
                _context.GroupMembers.Add(member);
                await _context.SaveChangesAsync();

                response.Data = await BuildGetGroupDto(group.Id);
                response.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group");
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<GetGroupDto>> AddMember(int groupId, string userId, string requesterId)
        {
            var response = new ServiceResponse<GetGroupDto>();
            try
            {
                var group = await _context.GroupConversations.FindAsync(groupId);
                if (group == null)
                {
                    response.Success = false;
                    response.Message = "Group not found.";
                    return response;
                }

                if (group.CreatorId != requesterId)
                {
                    response.Success = false;
                    response.Message = "Only the group creator can add members.";
                    return response;
                }

                var alreadyMember = await _context.GroupMembers
                    .AnyAsync(m => m.GroupConversationId == groupId && m.UserId == userId);
                if (alreadyMember)
                {
                    response.Success = false;
                    response.Message = "User is already a member.";
                    return response;
                }

                _context.GroupMembers.Add(new GroupMember { GroupConversationId = groupId, UserId = userId });
                await _context.SaveChangesAsync();

                response.Data = await BuildGetGroupDto(groupId);
                response.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding member to group {GroupId}", groupId);
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<GetGroupDto>> RemoveMember(int groupId, string userId, string requesterId)
        {
            var response = new ServiceResponse<GetGroupDto>();
            try
            {
                var group = await _context.GroupConversations.FindAsync(groupId);
                if (group == null)
                {
                    response.Success = false;
                    response.Message = "Group not found.";
                    return response;
                }

                if (group.CreatorId != requesterId)
                {
                    response.Success = false;
                    response.Message = "Only the creator can remove members.";
                    return response;
                }

                var member = await _context.GroupMembers
                    .FirstOrDefaultAsync(m => m.GroupConversationId == groupId && m.UserId == userId);
                if (member == null)
                {
                    response.Success = false;
                    response.Message = "User is not a member.";
                    return response;
                }

                _context.GroupMembers.Remove(member);
                await _context.SaveChangesAsync();

                response.Data = await BuildGetGroupDto(groupId);
                response.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member from group {GroupId}", groupId);
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<GetGroupDto>> ExitGroup(int groupId, string userId)
        {
            var response = new ServiceResponse<GetGroupDto>();
            try
            {
                var member = await _context.GroupMembers
                    .FirstOrDefaultAsync(m => m.GroupConversationId == groupId && m.UserId == userId);
                if (member == null)
                {
                    response.Success = false;
                    response.Message = "User is not a member of this group.";
                    return response;
                }

                _context.GroupMembers.Remove(member);
                await _context.SaveChangesAsync();

                response.Data = await BuildGetGroupDto(groupId);
                response.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exiting group {GroupId} for user {UserId}", groupId, userId);
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<List<GetGroupMessageDto>>> GetGroupMessages(int groupId, string userId)
        {
            var response = new ServiceResponse<List<GetGroupMessageDto>>();
            try
            {
                var isMember = await _context.GroupMembers
                    .AnyAsync(m => m.GroupConversationId == groupId && m.UserId == userId);
                if (!isMember)
                {
                    response.Success = false;
                    response.Message = "Access denied.";
                    return response;
                }

                var group = await _context.GroupConversations.FindAsync(groupId);
                if (group == null)
                {
                    response.Success = false;
                    response.Message = "Group not found.";
                    return response;
                }

                var groupConvKey = $"group_{groupId}";
                var unreadRow = await _context.UnreadConversations
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.ConversationKey == groupConvKey);
                if (unreadRow != null && unreadRow.Count > 0)
                {
                    unreadRow.Count = 0;
                    unreadRow.LastUpdated = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    var newTotal = await _context.UnreadConversations
                        .Where(u => u.UserId == userId)
                        .SumAsync(u => u.Count);
                    var reader = await _context.Users.FindAsync(userId);
                    if (reader != null && !string.IsNullOrEmpty(reader.ExpoPushToken))
                        _ = _expoPush.SendSilentBadgeUpdateAsync(reader.ExpoPushToken, newTotal);
                }

                var keyBytes = EncryptionHelper.KeyFromBase64(group.EncryptionKey);

                var messages = await _context.GroupMessages
                    .Where(m => m.GroupConversationId == groupId)
                    .OrderBy(m => m.DateTime)
                    .Join(_context.Users,
                        m => m.SenderId,
                        u => u.Id,
                        (m, u) => new { Message = m, Sender = u })
                    .ToListAsync();

                var dtos = messages.Select(x =>
                {
                    string decrypted;
                    try { decrypted = EncryptionHelper.DecryptString(x.Message.Text, keyBytes); }
                    catch { decrypted = string.Empty; }

                    return new GetGroupMessageDto
                    {
                        Id = x.Message.Id,
                        GroupConversationId = x.Message.GroupConversationId,
                        SenderId = x.Message.SenderId,
                        SenderUsername = x.Sender.UserName ?? string.Empty,
                        SenderProfileUrl = x.Sender.ProfileImageUrl ?? string.Empty,
                        SenderProfileThumbnailUrl = x.Sender.ProfileImageThumbnailUrl ?? string.Empty,
                        SenderPublicId = x.Sender.PublicId ?? string.Empty,
                        Text = decrypted,
                        DateTime = x.Message.DateTime
                    };
                }).ToList();

                response.Data = dtos;
                response.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for group {GroupId}", groupId);
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<GetGroupDto>> GetGroupById(int groupId, string userId)
        {
            var response = new ServiceResponse<GetGroupDto>();
            try
            {
                var isMember = await _context.GroupMembers
                    .AnyAsync(m => m.GroupConversationId == groupId && m.UserId == userId);
                if (!isMember)
                {
                    response.Success = false;
                    response.Message = "Not a member of this group.";
                    return response;
                }
                response.Data = await BuildGetGroupDto(groupId);
                response.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching group {GroupId} for user {UserId}", groupId, userId);
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        private async Task<GetGroupDto> BuildGetGroupDto(int groupId)
        {
            var group = await _context.GroupConversations
                .Include(g => g.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return new GetGroupDto();

            return new GetGroupDto
            {
                Id = group.Id,
                Name = group.Name,
                CreatorId = group.CreatorId,
                LastMessageDate = group.LastMessageDate,
                Members = group.Members.Select(m => new GroupMemberDto
                {
                    UserId = m.UserId,
                    Username = m.User.UserName ?? string.Empty,
                    ProfileImageUrl = m.User.ProfileImageUrl ?? string.Empty,
                    ProfileImageThumbnailUrl = m.User.ProfileImageThumbnailUrl ?? string.Empty,
                    PublicId = m.User.PublicId ?? string.Empty
                }).ToList()
            };
        }
    }
}
