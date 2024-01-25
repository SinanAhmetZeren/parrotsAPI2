using Microsoft.AspNetCore.Mvc.ModelBinding;
using ParrotsAPI2.Dtos.MessageDtos;

namespace ParrotsAPI2.Services.Message
{
    public interface IMessageService
    {
        Task<ServiceResponse<List<GetMessageDto>>> GetAllMessages();
        Task<ServiceResponse<GetMessageDto>> GetMessageById(int id);
        Task<ServiceResponse<List<GetMessageDto>>> GetMessagesBySenderId(int senderId);
        Task<ServiceResponse<List<GetMessageDto>>> GetMessagesByReceiverId(int receiverId);
        Task<ServiceResponse<List<GetMessageDto>>> GetMessagesByUserId(int userId);
        Task<ServiceResponse<GetMessageDto>> AddMessage(AddMessageDto newMessage);
        Task<ServiceResponse<GetMessageDto>> UpdateMessage(UpdateMessageDto updatedMessage);
        Task<ServiceResponse<GetMessageDto>> DeleteMessage(int id);

    }
}
