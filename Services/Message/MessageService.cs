using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Dtos.MessageDtos;
using ParrotsAPI2.Helpers;

namespace ParrotsAPI2.Services.Message
{
    public class MessageService : IMessageService
    {

        private readonly DataContext _context;
        public MessageService(DataContext context)
        {
            _context = context;
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

                // Step 1: Get conversations for the user
                var conversations = await _context.Conversations
                    .Where(c => c.User1Id == userId || c.User2Id == userId)
                    .OrderByDescending(c => c.LastMessageDate)
                    .ToListAsync();

                if (!conversations.Any())
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No messages found.";
                    return serviceResponse;
                }

                // Step 2: Get last messages for each conversation
                var lastMessageIds = conversations
                    .Where(c => c.LastMessageId.HasValue)
                    .Select(c => c.LastMessageId.Value)
                    .ToList();

                var lastMessages = await _context.Messages
                    .Where(m => lastMessageIds.Contains(m.Id))
                    .ToDictionaryAsync(m => m.Id);

                // Step 3: Get all users involved in these conversations
                var userIds = conversations
                    .SelectMany(c => new[] { c.User1Id, c.User2Id })
                    .Distinct()
                    .ToList();

                var users = await _context.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);

                // Step 4: Map DTOs
                var messageDtos = conversations
                    .Select(c =>
                    {
                        if (!c.LastMessageId.HasValue || !lastMessages.ContainsKey(c.LastMessageId.Value))
                            return null;

                        var lastMessage = lastMessages[c.LastMessageId.Value];

                        // Determine if current user is sender or receiver in this message
                        var decryptedText = lastMessage.SenderId == userId
                            ? EncryptionHelper.DecryptString(lastMessage.TextSenderEncrypted, currentUserKeyBytes)
                            : EncryptionHelper.DecryptString(lastMessage.TextReceiverEncrypted, currentUserKeyBytes);

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
                    var decryptedText = message.SenderId == userId1
                        ? EncryptionHelper.DecryptString(message.TextSenderEncrypted, currentUserKeyBytes)
                        : EncryptionHelper.DecryptString(message.TextReceiverEncrypted, currentUserKeyBytes);

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
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error retrieving messages: {ex.Message}";
            }

            return serviceResponse;
        }


    }
}
