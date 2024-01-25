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
                    Sender = sender,
                    ReceiverId = receiver.Id,
                    Receiver = receiver
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
        public async Task<ServiceResponse<List<GetMessageDto>>> GetMessagesByReceiverId(int receiverId)
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
        public async Task<ServiceResponse<List<GetMessageDto>>> GetMessagesBySenderId(int senderId)
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
        public async Task<ServiceResponse<List<GetMessageDto>>> GetMessagesByUserId(int userId)
        {
            var serviceResponse = new ServiceResponse<List<GetMessageDto>>();

            try
            {
                var messages = await _context.Messages
                    .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                    .ToListAsync();

                if (messages == null || messages.Count == 0)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "No messages found for the given user ID";
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

    }
}