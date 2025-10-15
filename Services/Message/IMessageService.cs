using Microsoft.AspNetCore.Mvc.ModelBinding;
using ParrotsAPI2.Dtos.MessageDtos;

namespace ParrotsAPI2.Services.Message
{
    public interface IMessageService
    {
        Task<ServiceResponse<List<GetMessageDto>>> GetMessagesByUserId(string userId);
        Task<ServiceResponse<List<GetMessageDto>>> GetMessagesBetweenUsers(string userId1, string userId2);

    }
}


