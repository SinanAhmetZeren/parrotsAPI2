using ParrotsAPI2.Dtos.GroupDtos;
using ParrotsAPI2.Models;

namespace ParrotsAPI2.Services.Group
{
    public interface IGroupService
    {
        Task<ServiceResponse<GetGroupDto>> CreateGroup(CreateGroupDto dto);
        Task<ServiceResponse<GetGroupDto>> AddMember(int groupId, string userId, string requesterId);
        Task<ServiceResponse<GetGroupDto>> RemoveMember(int groupId, string userId, string requesterId);
        Task<ServiceResponse<GetGroupDto>> ExitGroup(int groupId, string userId);
        Task<ServiceResponse<List<GetGroupMessageDto>>> GetGroupMessages(int groupId, string userId);
        Task<ServiceResponse<GetGroupDto>> GetGroupById(int groupId, string userId);
    }
}
