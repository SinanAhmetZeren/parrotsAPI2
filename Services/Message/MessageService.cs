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
                var sender = await _context.Users.FindAsync(newMessage.SenderId);
                var receiver = await _context.Users.FindAsync(newMessage.ReceiverId);

                if (sender == null || receiver == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Invalid sender or receiver specified";
                    return serviceResponse;
                }

                var message = new Models.Message
                {
                    Text = newMessage.Text,
                    DateTime = DateTime.Now,
                    Rendered = false,
                    ReadByReceiver = false,
                    SenderId = sender.Id,
                    ReceiverId = receiver.Id,
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                var messageDto = _mapper.Map<GetMessageDto>(message);
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
                serviceResponse.Data = messageDto;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error deleting message: {ex.Message}";
            }

            return serviceResponse;
        }
        public async Task<ServiceResponse<List<GetMessageDto>>> GetAllMessages()
        {
            var serviceResponse = new ServiceResponse<List<GetMessageDto>>();

            try
            {
                var messages = await _context.Messages.ToListAsync();
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
        public async Task<ServiceResponse<List<GetMessageDto>>> GetMessagesByUserId(string userId)
        {
            var serviceResponse = new ServiceResponse<List<GetMessageDto>>();

                try
                {
                var latestMessages = await _context.Messages
                    .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                    .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                    .Select(g => g.OrderByDescending(m => m.DateTime).FirstOrDefault())
                    .ToListAsync();

                if (latestMessages == null || latestMessages.Count == 0)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No messages found for the given user ID";
                    return serviceResponse;
                }

                var messageDtos = new List<GetMessageDto>();

                foreach (var message in latestMessages)
                {
                    var senderId = message.SenderId == userId ? message.ReceiverId : message.SenderId;
                    var sender = await _context.Users.FindAsync(senderId);

                    messageDtos.Add(new GetMessageDto
                    {
                        Id = message.Id,
                        Text = message.Text,
                        DateTime = message.DateTime,
                        Rendered = message.Rendered,
                        ReadByReceiver = message.ReadByReceiver,
                        SenderId = message.SenderId,
                        ReceiverId = message.ReceiverId,
                        SenderProfileUrl = sender?.ProfileImageUrl,
                        SenderUsername = sender?.UserName,
                        ReceiverProfileUrl = sender?.ProfileImageUrl,
                        ReceiverUsername = sender?.UserName
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
                var messageDtos = new List<GetMessageDto>();
                foreach (var message in messages)
                {
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