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


        public async Task<ServiceResponse<List<GetMessageDto>>> GetMessagesByUserId2(string userId)
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
