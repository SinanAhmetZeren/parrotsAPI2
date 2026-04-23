using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Dtos.MessageDtos;
using ParrotsAPI2.Helpers;
using ParrotsAPI2.Models;

namespace ParrotsAPI2.Services.Message
{
    public class MessageService : IMessageService
    {

        private readonly DataContext _context;
        private readonly ILogger<MessageService> _logger;

        public MessageService(DataContext context, ILogger<MessageService> logger)
        {
            _context = context;
            _logger = logger;
        }


        public async Task<ServiceResponse<List<GetMessageDto>>> GetMessagesByUserId(string userId)
        {
            var serviceResponse = new ServiceResponse<List<GetMessageDto>>();

            try
            {
                // Step 0: Get current user's encryption key
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (currentUser == null || string.IsNullOrEmpty(currentUser.EncryptionKey))
                    throw new Exception("Current user or encryption key not found.");

                var currentUserKeyBytes = EncryptionHelper.KeyFromBase64(currentUser.EncryptionKey);

                // Single query: join conversations with their last messages and both users
                var results = await _context.Conversations
                    .Where(c => c.User1Id == userId || c.User2Id == userId)
                    .Where(c => c.LastMessageId.HasValue)
                    .OrderByDescending(c => c.LastMessageDate)
                    .Join(_context.Messages,
                        c => c.LastMessageId,
                        m => m.Id,
                        (c, m) => new { Conversation = c, LastMessage = m })
                    .Join(_context.Users,
                        cm => cm.Conversation.User1Id,
                        u => u.Id,
                        (cm, u1) => new { cm.Conversation, cm.LastMessage, User1 = u1 })
                    .Join(_context.Users,
                        cmu => cmu.Conversation.User2Id,
                        u => u.Id,
                        (cmu, u2) => new { cmu.Conversation, cmu.LastMessage, cmu.User1, User2 = u2 })
                    .ToListAsync();

                if (!results.Any())
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No messages found.";
                    return serviceResponse;
                }

                // Map DTOs
                var messageDtos = results
                    .Select(r =>
                    {
                        var c = r.Conversation;
                        var lastMessage = r.LastMessage;
                        var users = new Dictionary<string, AppUser>
                        {
                            [r.User1.Id] = r.User1,
                            [r.User2.Id] = r.User2
                        };

                        // Determine if current user is sender or receiver in this message
                        string decryptedText;
                        try
                        {
                            decryptedText = lastMessage.SenderId == userId
                                ? EncryptionHelper.DecryptString(lastMessage.TextSenderEncrypted, currentUserKeyBytes)
                                : EncryptionHelper.DecryptString(lastMessage.TextReceiverEncrypted, currentUserKeyBytes);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to decrypt message {MessageId} for user {UserId}", lastMessage.Id, userId);
                            decryptedText = string.Empty;
                        }

                        return new GetMessageDto
                        {
                            Id = lastMessage.Id,
                            Text = decryptedText,
                            DateTime = lastMessage.DateTime,

                            SenderId = lastMessage.SenderId,
                            ReceiverId = lastMessage.ReceiverId,

                            SenderProfileUrl = users.GetValueOrDefault(lastMessage.SenderId)?.ProfileImageUrl ?? "",
                            SenderProfileThumbnailUrl = users.GetValueOrDefault(lastMessage.SenderId)?.ProfileImageThumbnailUrl ?? "",
                            SenderUsername = users.GetValueOrDefault(lastMessage.SenderId)?.UserName ?? "",
                            SenderPublicId = users.GetValueOrDefault(lastMessage.SenderId)?.PublicId ?? "",

                            ReceiverProfileUrl = users.GetValueOrDefault(lastMessage.ReceiverId)?.ProfileImageUrl ?? "",
                            ReceiverProfileThumbnailUrl = users.GetValueOrDefault(lastMessage.ReceiverId)?.ProfileImageThumbnailUrl ?? "",
                            ReceiverUsername = users.GetValueOrDefault(lastMessage.ReceiverId)?.UserName ?? "",
                            ReceiverPublicId = users.GetValueOrDefault(lastMessage.ReceiverId)?.PublicId ?? ""
                        };
                    })
                    .Where(dto => dto != null)
                    .ToList()!;

                serviceResponse.Data = messageDtos;
                serviceResponse.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving messages for user {UserId}", userId);
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetMessageDto>>> GetMessagesBetweenUsers(string userId1, string userId2)
        {
            var serviceResponse = new ServiceResponse<List<GetMessageDto>>();

            try
            {
                // Step 0: Get current user's encryption key (userId1)
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId1);
                if (currentUser == null || string.IsNullOrEmpty(currentUser.EncryptionKey))
                    throw new Exception("Current user or encryption key not found.");

                var currentUserKeyBytes = EncryptionHelper.KeyFromBase64(currentUser.EncryptionKey);

                // Step 1: Fetch all messages between these two users
                var messages = await _context.Messages
                    .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2)
                             || (m.SenderId == userId2 && m.ReceiverId == userId1))
                    .OrderBy(m => m.DateTime)
                    .ToListAsync();

                if (!messages.Any())
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No messages found between the given users.";
                    return serviceResponse;
                }

                // Step 2: Fetch both users for usernames and profile URLs
                var users = await _context.Users
                    .Where(u => u.Id == userId1 || u.Id == userId2)
                    .ToDictionaryAsync(u => u.Id);

                if (!users.ContainsKey(userId1) || !users.ContainsKey(userId2))
                    throw new Exception("One or both users not found.");

                // Step 3: Map to DTOs and decrypt using current user's key
                var messageDtos = messages.Select(message =>
                {
                    string decryptedText;
                    try
                    {
                        decryptedText = message.SenderId == userId1
                            ? EncryptionHelper.DecryptString(message.TextSenderEncrypted, currentUserKeyBytes)
                            : EncryptionHelper.DecryptString(message.TextReceiverEncrypted, currentUserKeyBytes);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to decrypt message {MessageId} for user {UserId}", message.Id, userId1);
                        decryptedText = string.Empty;
                    }

                    return new GetMessageDto
                    {
                        Id = message.Id,
                        Text = decryptedText,
                        DateTime = message.DateTime,
                        SenderId = message.SenderId,
                        ReceiverId = message.ReceiverId,
                        SenderProfileUrl = users.GetValueOrDefault(message.SenderId)?.ProfileImageUrl ?? string.Empty,
                        SenderUsername = users.GetValueOrDefault(message.SenderId)?.UserName ?? string.Empty,
                        SenderPublicId = users.GetValueOrDefault(message.SenderId)?.PublicId ?? string.Empty,
                        ReceiverProfileUrl = users.GetValueOrDefault(message.ReceiverId)?.ProfileImageUrl ?? string.Empty,
                        ReceiverUsername = users.GetValueOrDefault(message.ReceiverId)?.UserName ?? string.Empty,
                        ReceiverPublicId = users.GetValueOrDefault(message.ReceiverId)?.PublicId ?? string.Empty
                    };
                }).ToList();

                serviceResponse.Success = true;
                serviceResponse.Data = messageDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving messages between users {UserId1} and {UserId2}", userId1, userId2);
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error retrieving messages: {ex.Message}";
            }

            return serviceResponse;
        }


    }
}
