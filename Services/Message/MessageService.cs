using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Dtos.MessageDtos;
using ParrotsAPI2.Dtos.WaypointDtos;
using ParrotsAPI2.Helpers;

namespace ParrotsAPI2.Services.Message
{
    public class MessageService : IMessageService
    {

        private readonly IMapper _mapper;
        private readonly DataContext _context;
        public MessageService(IMapper mapper, DataContext context)
        {
            _context = context;
            _mapper = mapper;
        }


        public async Task<ServiceResponse<List<GetMessageDto>>> GetMessagesByUserId_oldest(string userId)
        {
            var serviceResponse = new ServiceResponse<List<GetMessageDto>>();

            try
            {
                // Step 1: Get the latest message from each conversation
                var latestMessages = await _context.Messages
                    .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                    .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                    .Select(g => g.OrderByDescending(m => m.DateTime).FirstOrDefault())
                    .ToListAsync();

                if (!latestMessages.Any())
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No messages found for the given user ID.";
                    return serviceResponse;
                }

                // Step 2: Extract all user IDs involved in the latest messages
                var userIds = latestMessages
                    .SelectMany(m => new[] { m?.SenderId, m?.ReceiverId })
                    .Distinct()
                    .ToList();

                // Step 3: Fetch all related users in one query
                var users = await _context.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);

                // Step 4: Map messages to DTOs with user info


                var messageDtos = latestMessages.Where(message => message != null).Select(message => new GetMessageDto
                {
                    Id = message?.Id ?? 0,
                    Text = message?.TextSenderEncrypted ?? string.Empty,
                    DateTime = message?.DateTime ?? DateTime.MinValue,
                    Rendered = message?.Rendered ?? false,
                    ReadByReceiver = message?.ReadByReceiver ?? false,
                    SenderId = message?.SenderId ?? string.Empty,
                    ReceiverId = message?.ReceiverId ?? string.Empty,
                    SenderProfileUrl = ((IReadOnlyDictionary<string?, AppUser>)users).GetValueOrDefault(message?.SenderId)?.ProfileImageUrl ?? string.Empty,
                    SenderUsername = ((IReadOnlyDictionary<string?, AppUser>)users).GetValueOrDefault(message?.SenderId)?.UserName ?? string.Empty,
                    ReceiverProfileUrl = ((IReadOnlyDictionary<string?, AppUser>)users).GetValueOrDefault(message?.ReceiverId)?.ProfileImageUrl ?? string.Empty,
                    ReceiverUsername = ((IReadOnlyDictionary<string?, AppUser>)users).GetValueOrDefault(message?.ReceiverId)?.UserName ?? string.Empty
                }).ToList();

                serviceResponse.Data = messageDtos;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error retrieving messages: {ex.Message}";
            }

            return serviceResponse;
        }


        public async Task<ServiceResponse<List<GetMessageDto>>> GetMessagesByUserId_older(string userId)
        {
            var serviceResponse = new ServiceResponse<List<GetMessageDto>>();

            try
            {
                // Step 0: Get current user's encryption key
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (currentUser == null || string.IsNullOrEmpty(currentUser.EncryptionKey))
                    throw new Exception("Current user or encryption key not found.");

                var currentUserKeyBytes = EncryptionHelper.KeyFromBase64(currentUser.EncryptionKey);

                // // Step 1: Get the latest message from each conversation
                // var latestMessages2 = await _context.Messages
                //     .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                //     .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                //     .Select(g => g.OrderByDescending(m => m.DateTime).FirstOrDefault())
                //     .ToListAsync();

                // Step 1: Get the latest message from each conversation USING ConversationKey index
                var latestMessages = await _context.Messages
                    .Where(m => (m.SenderId == userId || m.ReceiverId == userId)
                                && m.ConversationKey != null)
                    .GroupBy(m => m.ConversationKey)
                    .Select(g => g
                        .OrderByDescending(m => m.DateTime)
                        .FirstOrDefault())
                    .ToListAsync();

                if (!latestMessages.Any())
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No messages found for the given user ID.";
                    return serviceResponse;
                }

                // Step 2: Extract all user IDs involved in the latest messages
                var userIds = latestMessages
                    .SelectMany(m => new[] { m?.SenderId, m?.ReceiverId })
                    .Distinct()
                    .ToList();

                // Step 3: Fetch all related users for usernames and profile URLs
                var users = await _context.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);

                // Step 4: Map messages to DTOs with decrypted text for current user
                var messageDtos = latestMessages
                    .Where(message => message != null)
                    .Select(message =>
                    {
                        var decryptedText = message.SenderId == userId
                            ? EncryptionHelper.DecryptString(message.TextSenderEncrypted, currentUserKeyBytes)
                            : EncryptionHelper.DecryptString(message.TextReceiverEncrypted, currentUserKeyBytes);

                        return new GetMessageDto
                        {
                            Id = message.Id,
                            Text = decryptedText,
                            DateTime = message.DateTime,
                            Rendered = message.Rendered,
                            ReadByReceiver = message.ReadByReceiver,
                            SenderId = message.SenderId,
                            ReceiverId = message.ReceiverId,
                            SenderProfileUrl = users.GetValueOrDefault(message.SenderId)?.ProfileImageUrl ?? string.Empty,
                            SenderUsername = users.GetValueOrDefault(message.SenderId)?.UserName ?? string.Empty,
                            SenderPublicId = users.GetValueOrDefault(message.SenderId)?.PublicId ?? string.Empty,
                            ReceiverProfileUrl = users.GetValueOrDefault(message.ReceiverId)?.ProfileImageUrl ?? string.Empty,
                            ReceiverUsername = users.GetValueOrDefault(message.ReceiverId)?.UserName ?? string.Empty,
                            ReceiverPublicId = users.GetValueOrDefault(message.ReceiverId)?.PublicId ?? string.Empty
                        };
                    })
                    .ToList();

                serviceResponse.Data = messageDtos;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error retrieving messages: {ex.Message}";
            }

            return serviceResponse;
        }

        /*
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

                        // Step 1: Get conversations (FAST)
                        var conversations = await _context.Conversations
                            .Where(c => c.SenderId == userId || c.ReceiverId == userId)
                            .OrderByDescending(c => c.LastMessageDate)
                            .ToListAsync();

                        if (!conversations.Any())
                        {
                            serviceResponse.Success = false;
                            serviceResponse.Message = "No messages found.";
                            return serviceResponse;
                        }

                        // Step 2: Get users
                        var userIds = conversations
                            .SelectMany(c => new[] { c.SenderId, c.ReceiverId })
                            .Distinct()
                            .ToList();

                        var users = await _context.Users
                            .Where(u => userIds.Contains(u.Id))
                            .ToDictionaryAsync(u => u.Id);

                        // Step 3: Map DTO
                        var messageDtos = conversations
                            .Select(c =>
                            {
                                var decryptedText =
                                    c.SenderId == userId
                                    ? EncryptionHelper.DecryptString(c.TextSenderEncrypted, currentUserKeyBytes)
                                    : EncryptionHelper.DecryptString(c.TextReceiverEncrypted, currentUserKeyBytes);

                                return new GetMessageDto
                                {
                                    Id = c.LastMessageId ?? 0,
                                    Text = decryptedText,
                                    DateTime = c.LastMessageDate,

                                    SenderId = c.SenderId,
                                    ReceiverId = c.ReceiverId,

                                    SenderProfileUrl = users.GetValueOrDefault(c.SenderId)?.ProfileImageUrl ?? "",
                                    SenderUsername = users.GetValueOrDefault(c.SenderId)?.UserName ?? "",
                                    SenderPublicId = users.GetValueOrDefault(c.SenderId)?.PublicId ?? "",

                                    ReceiverProfileUrl = users.GetValueOrDefault(c.ReceiverId)?.ProfileImageUrl ?? "",
                                    ReceiverUsername = users.GetValueOrDefault(c.ReceiverId)?.UserName ?? "",
                                    ReceiverPublicId = users.GetValueOrDefault(c.ReceiverId)?.PublicId ?? ""
                                };
                            })
                            .ToList();

                        serviceResponse.Data = messageDtos;
                    }
                    catch (Exception ex)
                    {
                        serviceResponse.Success = false;
                        serviceResponse.Message = ex.Message;
                    }

                    return serviceResponse;
                }

        */
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
                            SenderUsername = users.GetValueOrDefault(lastMessage.SenderId)?.UserName ?? "",
                            SenderPublicId = users.GetValueOrDefault(lastMessage.SenderId)?.PublicId ?? "",

                            ReceiverProfileUrl = users.GetValueOrDefault(lastMessage.ReceiverId)?.ProfileImageUrl ?? "",
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
                        Rendered = message.Rendered,
                        ReadByReceiver = message.ReadByReceiver,
                        SenderId = message.SenderId,
                        ReceiverId = message.ReceiverId,
                        SenderProfileUrl = users.GetValueOrDefault(message.SenderId)?.ProfileImageUrl ?? string.Empty,
                        SenderUsername = users.GetValueOrDefault(message.SenderId)?.UserName ?? string.Empty,
                        ReceiverProfileUrl = users.GetValueOrDefault(message.ReceiverId)?.ProfileImageUrl ?? string.Empty,
                        ReceiverUsername = users.GetValueOrDefault(message.ReceiverId)?.UserName ?? string.Empty
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
