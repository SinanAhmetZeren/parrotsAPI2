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

        public async Task<ServiceResponse<GetMessageDto>> AddMessage(AddMessageDto newMessage)
        {
            var serviceResponse = new ServiceResponse<GetMessageDto>();

            try
            {
                // Check that sender and receiver exist
                var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == newMessage.SenderId);
                var receiverExists = await _context.Users.AnyAsync(u => u.Id == newMessage.ReceiverId);

                if (sender == null || !receiverExists)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Invalid sender or receiver specified";
                    return serviceResponse;
                }

                // Ensure sender has an encryption key
                if (string.IsNullOrEmpty(sender.EncryptionKey))
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Sender does not have an encryption key";
                    return serviceResponse;
                }

                // Convert sender's Base64 key to byte[]
                var keyBytes = EncryptionHelper.KeyFromBase64(sender.EncryptionKey);

                // Encrypt the message text using sender's key
                var encryptedText = EncryptionHelper.EncryptString(newMessage.Text, keyBytes);

                // Save encrypted message
                var message = new Models.Message
                {
                    Text = encryptedText,
                    DateTime = DateTime.UtcNow,
                    Rendered = false,
                    ReadByReceiver = false,
                    SenderId = newMessage.SenderId,
                    ReceiverId = newMessage.ReceiverId
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Map to DTO (optional: decrypt before sending back)
                var messageDto = _mapper.Map<GetMessageDto>(message);
                serviceResponse.Success = true;
                serviceResponse.Data = messageDto;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error adding message: {ex.Message}";
            }

            return serviceResponse;
        }
        public async Task<ServiceResponse<GetMessageDto>> DeleteMessage(int id)
        {
            var serviceResponse = new ServiceResponse<GetMessageDto>();

            try
            {
                var message = await _context.Messages.FindAsync(id);

                if (message == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Message not found";
                    return serviceResponse;
                }

                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();

                var messageDto = _mapper.Map<GetMessageDto>(message);
                serviceResponse.Success = true;
                serviceResponse.Data = messageDto;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error deleting message: {ex.Message}";
            }

            return serviceResponse;
        }
        public async Task<ServiceResponse<GetMessageDto>> GetMessageById(int id)
        {
            var serviceResponse = new ServiceResponse<GetMessageDto>();

            try
            {
                var message = await _context.Messages.FindAsync(id);

                if (message == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Message not found";
                    return serviceResponse;
                }

                // Get receiver to retrieve encryption key
                var receiver = await _context.Users.FindAsync(message.ReceiverId);
                if (receiver == null || string.IsNullOrEmpty(receiver.EncryptionKey))
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Receiver or encryption key not found";
                    return serviceResponse;
                }

                // Convert key from Base64 to byte[]
                var keyBytes = EncryptionHelper.KeyFromBase64(receiver.EncryptionKey);

                // Decrypt the message text
                var decryptedText = EncryptionHelper.DecryptString(message.Text, keyBytes);
                message.Text = decryptedText;

                var messageDto = _mapper.Map<GetMessageDto>(message);
                serviceResponse.Success = true;
                serviceResponse.Data = messageDto;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error retrieving message: {ex.Message}";
            }

            return serviceResponse;
        }
        public async Task<ServiceResponse<List<GetMessageDto>>> GetMessagesByReceiverId(string receiverId)
        {
            var serviceResponse = new ServiceResponse<List<GetMessageDto>>();

            try
            {
                var messages = await _context.Messages
                    .Where(m => m.ReceiverId == receiverId)
                    .ToListAsync();

                if (messages == null || messages.Count == 0)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No messages found for the given receiver ID";
                    return serviceResponse;
                }

                // Get receiver to retrieve encryption key
                var receiver = await _context.Users.FindAsync(receiverId); // <-- CHANGED
                if (receiver == null || string.IsNullOrEmpty(receiver.EncryptionKey)) // <-- CHANGED
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Receiver or encryption key not found"; // <-- CHANGED
                    return serviceResponse;
                }

                var keyBytes = EncryptionHelper.KeyFromBase64(receiver.EncryptionKey); // <-- CHANGED

                // Decrypt each message
                foreach (var msg in messages) // <-- CHANGED
                {
                    msg.Text = EncryptionHelper.DecryptString(msg.Text, keyBytes); // <-- CHANGED
                }

                var messageDtos = _mapper.Map<List<GetMessageDto>>(messages);
                serviceResponse.Success = true; // <-- CHANGED: previously not set
                serviceResponse.Data = messageDtos;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error retrieving messages: {ex.Message}";
            }

            return serviceResponse;
        }
        public async Task<ServiceResponse<List<GetMessageDto>>> GetMessagesBySenderId(string senderId)
        {
            var serviceResponse = new ServiceResponse<List<GetMessageDto>>();

            try
            {
                var messages = await _context.Messages
                    .Where(m => m.SenderId == senderId)
                    .ToListAsync();

                if (messages == null || messages.Count == 0)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No messages found for the given sender ID";
                    return serviceResponse;
                }

                // Decrypt each message using the receiver's encryption key
                foreach (var msg in messages) // <-- CHANGED
                {
                    var receiver = await _context.Users.FindAsync(msg.ReceiverId); // <-- CHANGED
                    if (receiver != null && !string.IsNullOrEmpty(receiver.EncryptionKey)) // <-- CHANGED
                    {
                        var keyBytes = EncryptionHelper.KeyFromBase64(receiver.EncryptionKey); // <-- CHANGED
                        msg.Text = EncryptionHelper.DecryptString(msg.Text, keyBytes); // <-- CHANGED
                    }
                    else
                    {
                        msg.Text = "[Unable to decrypt]"; // <-- CHANGED: optional fallback
                    }
                }

                var messageDtos = _mapper.Map<List<GetMessageDto>>(messages);
                serviceResponse.Success = true; // <-- CHANGED: ensure success flag is set
                serviceResponse.Data = messageDtos;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error retrieving messages: {ex.Message}";
            }

            return serviceResponse;
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
                    .Where(id => id != null)
                    .Distinct()
                    .ToList();

                // Step 3: Fetch all related users in one query
                var users = await _context.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);

                // Step 4: Map messages to DTOs with decryption
                var messageDtos = latestMessages
                    .Where(message => message != null)
                    .Select(message =>
                    {
                        // Determine whose key to use for decryption
                        string? keyString = (message.ReceiverId == userId)
                            ? users[message.ReceiverId!].EncryptionKey
                            : users[message.SenderId!].EncryptionKey;

                        var keyBytes = EncryptionHelper.KeyFromBase64(keyString);

                        string decryptedText = EncryptionHelper.DecryptString(message.Text, keyBytes);

                        return new GetMessageDto
                        {
                            Id = message.Id,
                            Text = decryptedText,
                            DateTime = message.DateTime,
                            Rendered = message.Rendered,
                            ReadByReceiver = message.ReadByReceiver,
                            SenderId = message.SenderId,
                            ReceiverId = message.ReceiverId,
                            SenderProfileUrl = users[message.SenderId!].ProfileImageUrl ?? string.Empty,
                            SenderUsername = users[message.SenderId!].UserName ?? string.Empty,
                            ReceiverProfileUrl = users[message.ReceiverId!].ProfileImageUrl ?? string.Empty,
                            ReceiverUsername = users[message.ReceiverId!].UserName ?? string.Empty
                        };
                    })
                    .ToList();

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


        public async Task<ServiceResponse<List<GetMessageDto>>> GetMessagesByUserId(string userId)
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
                    Text = message?.Text ?? string.Empty,
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


        public async Task<ServiceResponse<List<GetMessageDto>>> GetMessagesBetweenUsers(string userId1, string userId2)
        {
            var serviceResponse = new ServiceResponse<List<GetMessageDto>>();

            try
            {
                // Fetch messages between the two users
                var messages = await _context.Messages
                    .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2)
                             || (m.SenderId == userId2 && m.ReceiverId == userId1))
                    .OrderBy(m => m.DateTime)
                    .ToListAsync();

                if (!messages.Any())
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No messages found between the given users";
                    return serviceResponse;
                }

                // Fetch both users and ensure they exist
                var users = await _context.Users
                    .Where(u => u.Id == userId1 || u.Id == userId2)
                    .ToDictionaryAsync(u => u.Id);

                if (!users.ContainsKey(userId1) || !users.ContainsKey(userId2))
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "One or both users not found";
                    return serviceResponse;
                }

                // Map messages to DTOs with decryption
                var messageDtos = messages.Select(message =>
                {
                    var sender = users[message.SenderId];
                    var receiver = users[message.ReceiverId];

                    // Determine which key to use for decryption (receiver’s key)
                    string key = receiver.EncryptionKey ?? throw new Exception("Receiver does not have an encryption key");
                    var keyBytes = EncryptionHelper.KeyFromBase64(key);
                    string decryptedText = EncryptionHelper.DecryptString(message.Text, keyBytes);

                    return new GetMessageDto
                    {
                        Id = message.Id,
                        Text = decryptedText,
                        DateTime = message.DateTime,
                        Rendered = message.Rendered,
                        ReadByReceiver = message.ReadByReceiver,
                        SenderId = message.SenderId,
                        ReceiverId = message.ReceiverId,
                        SenderProfileUrl = sender.ProfileImageUrl ?? string.Empty,
                        SenderUsername = sender.UserName ?? string.Empty,
                        ReceiverProfileUrl = receiver.ProfileImageUrl ?? string.Empty,
                        ReceiverUsername = receiver.UserName ?? string.Empty
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

/*
                
                
                        public async Task<ServiceResponse<GetMessageDto>> AddMessage2(AddMessageDto newMessage)
        {
            var serviceResponse = new ServiceResponse<GetMessageDto>();

            try
            {
                var senderExists = await _context.Users.AnyAsync(u => u.Id == newMessage.SenderId);
                var receiverExists = await _context.Users.AnyAsync(u => u.Id == newMessage.ReceiverId);
                if (!senderExists || !receiverExists)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Invalid sender or receiver specified";
                    return serviceResponse;
                }
                var message = new Models.Message
                {
                    Text = newMessage.Text,
                    DateTime = DateTime.UtcNow,
                    Rendered = false,
                    ReadByReceiver = false,
                    SenderId = newMessage.SenderId,
                    ReceiverId = newMessage.ReceiverId
                };
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                var messageDto = _mapper.Map<GetMessageDto>(message);
                serviceResponse.Success = true;
                serviceResponse.Data = messageDto;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error adding message: {ex.Message}";
            }

            return serviceResponse;
        }

                
                
                public async Task<ServiceResponse<GetMessageDto>> UpdateMessage(UpdateMessageDto updatedMessage)
                {
                    var serviceResponse = new ServiceResponse<GetMessageDto>();

                    try
                    {
                        var message = await _context.Messages.FindAsync(updatedMessage.Id);

                        if (message == null)
                        {
                            serviceResponse.Success = false;
                            serviceResponse.Message = "Message not found";
                            return serviceResponse;
                        }

                        message.Rendered = updatedMessage.Rendered;
                        message.ReadByReceiver = updatedMessage.ReadByReceiver;
                        await _context.SaveChangesAsync();
                        var updatedMessageDto = _mapper.Map<GetMessageDto>(message);
                        serviceResponse.Data = updatedMessageDto;
                    }
                    catch (Exception ex)
                    {
                        serviceResponse.Success = false;
                        serviceResponse.Message = $"Error updating message: {ex.Message}";
                    }

                    return serviceResponse;
                }
        */