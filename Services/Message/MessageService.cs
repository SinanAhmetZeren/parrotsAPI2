using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Dtos.MessageDtos;
using ParrotsAPI2.Dtos.WaypointDtos;

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

                var messageDto = _mapper.Map<GetMessageDto>(message);
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

                var messageDtos = _mapper.Map<List<GetMessageDto>>(messages);
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

                var messageDtos = _mapper.Map<List<GetMessageDto>>(messages);
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
                var messageDtos = new List<GetMessageDto>();
                foreach (var message in latestMessages)
                {
                    if (message == null) continue;

                    var sender = await _context.Users.FindAsync(message.SenderId);
                    var receiver = await _context.Users.FindAsync(message.ReceiverId);

                    messageDtos.Add(new GetMessageDto
                    {
                        Id = message.Id,
                        Text = message.Text,
                        DateTime = message.DateTime,
                        Rendered = message.Rendered,
                        ReadByReceiver = message.ReadByReceiver,
                        SenderId = message.SenderId,
                        ReceiverId = message.ReceiverId,
                        SenderProfileUrl = sender != null && sender.ProfileImageUrl != null ? sender.ProfileImageUrl : string.Empty,
                        SenderUsername = sender != null && sender.UserName != null ? sender.UserName : string.Empty,
                        ReceiverProfileUrl = receiver != null && receiver.ProfileImageUrl != null ? receiver.ProfileImageUrl : string.Empty,
                        ReceiverUsername = receiver != null && receiver.UserName != null ? receiver.UserName : string.Empty,

                    });
                }
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
                    .SelectMany(m => new[] { m.SenderId, m.ReceiverId })
                    .Distinct()
                    .ToList();

                // Step 3: Fetch all related users in one query
                var users = await _context.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);

                // Step 4: Map messages to DTOs with user info


                var messageDtos = latestMessages.Where(message => message != null).Select(message => new GetMessageDto
                {
                    Id = message.Id,
                    Text = message.Text,
                    DateTime = message.DateTime,
                    Rendered = message.Rendered,
                    ReadByReceiver = message.ReadByReceiver,
                    SenderId = message.SenderId,
                    ReceiverId = message.ReceiverId,
                    SenderProfileUrl = users.GetValueOrDefault(message.SenderId)?.ProfileImageUrl ?? string.Empty,
                    SenderUsername = users.GetValueOrDefault(message.SenderId)?.UserName ?? string.Empty,
                    ReceiverProfileUrl = users.GetValueOrDefault(message.ReceiverId)?.ProfileImageUrl ?? string.Empty,
                    ReceiverUsername = users.GetValueOrDefault(message.ReceiverId)?.UserName ?? string.Empty
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
        public async Task<ServiceResponse<List<GetMessageDto>>> GetMessagesBetweenUsers(string userId1, string userId2)
        {
            var serviceResponse = new ServiceResponse<List<GetMessageDto>>();

            try
            {
                var messages = await _context.Messages
                    .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) || (m.SenderId == userId2 && m.ReceiverId == userId1))
                    .ToListAsync();
                if (messages == null || messages.Count == 0)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No messages found between the given users";
                    return serviceResponse;
                }

                var user1 = await _context.Users.FindAsync(userId1);
                var user2 = await _context.Users.FindAsync(userId2);

                var messageDtos = new List<GetMessageDto>();
                foreach (var message in messages)
                {
                    // var sender = await _context.Users.FindAsync(message.SenderId);
                    // var receiver = await _context.Users.FindAsync(message.ReceiverId);

                    var sender = message.SenderId == userId1 ? user1 : user2;
                    var receiver = message.ReceiverId == userId1 ? user1 : user2;

                    messageDtos.Add(new GetMessageDto
                    {
                        Id = message.Id,
                        Text = message.Text,
                        DateTime = message.DateTime,
                        Rendered = message.Rendered,
                        ReadByReceiver = message.ReadByReceiver,
                        SenderId = message.SenderId,
                        ReceiverId = message.ReceiverId,
                        //SenderProfileUrl = sender?.ProfileImageUrl,
                        //SenderUsername = sender?.UserName,
                        //ReceiverProfileUrl = receiver?.ProfileImageUrl,
                        //ReceiverUsername = receiver?.UserName
                    });
                }
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