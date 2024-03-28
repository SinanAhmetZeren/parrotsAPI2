using Microsoft.AspNetCore.Mvc.ModelBinding;
using ParrotsAPI2.Dtos.MessageDtos;

namespace ParrotsAPI2.Services.Message
{
    public interface IMessageService
    {
        Task<ServiceResponse<List<GetMessageDto>>> GetAllMessages();
        Task<ServiceResponse<GetMessageDto>> GetMessageById(int id);
        Task<ServiceResponse<List<GetMessageDto>>> GetMessagesBySenderId(string senderId);
        Task<ServiceResponse<List<GetMessageDto>>> GetMessagesByReceiverId(string receiverId);
        Task<ServiceResponse<List<GetMessageDto>>> GetMessagesByUserId(string userId);
        Task<ServiceResponse<GetMessageDto>> AddMessage(AddMessageDto newMessage);
        Task<ServiceResponse<GetMessageDto>> UpdateMessage(UpdateMessageDto updatedMessage);
        Task<ServiceResponse<GetMessageDto>> DeleteMessage(int id);
        Task<ServiceResponse<List<GetMessageDto>>> GetMessagesBetweenUsers(string userId1, string userId2);

    }
}
