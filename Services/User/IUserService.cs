using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ParrotsAPI2.Dtos.User;

namespace ParrotsAPI2.Services.User
{
    public interface IUserService
    {
        Task<ServiceResponse<List<GetUserDto>>> GetAllUsers();
        Task<ServiceResponse<GetUserDto>> GetUserById(string id);
        Task<ServiceResponse<GetUserDto>> GetSingleUserByUsername(string username);
        Task<ServiceResponse<GetUserDto>> GetUserByPublicId(string publicId);
        Task<ServiceResponse<List<UserDto>>> GetUsersByUsername(string username);
        Task<ServiceResponse<List<GetUserDto>>> AddUser(AddUserDto newUser);
        Task<ServiceResponse<GetUserDto>> UpdateUser(UpdateUserDto updatedUser);
        Task<ServiceResponse<GetUserDto>> UpdateUserUnseenMessage(UpdateUserUnseenMessageDto updatedUser);
        Task<ServiceResponse<GetUserDto>> PatchUser(string userId, JsonPatchDocument<UpdateUserDto> patchDoc, ModelStateDictionary modelState);
        Task<ServiceResponse<GetUserDto>> PatchUserAdmin(string userId, JsonPatchDocument<UpdateUserDto> patchDoc, ModelStateDictionary modelState);
        Task<ServiceResponse<GetUserDto>> UpdateUserProfileImage(string userId, IFormFile imageFile);
        Task<ServiceResponse<GetUserDto>> UpdateUserBackgroundImage(string userId, IFormFile imageFile);
        Task<ServiceResponse<int>> PurchaseCrackers(string userId, int crackers, decimal eurAmount, string PaymentProviderId);
        Task<ServiceResponse<int>> ClaimFreeCrackers(string userId);
        Task<ServiceResponse<int>> SendParrotCrackers(string userId, string receiverId, int crackers);
        Task<ServiceResponse<ParrotCrackerSummaryDto>> GetParrotCrackerBalanceAndPurchases(string userId);
        Task<ServiceResponse<int>> DeductCrackerForAsk(string userId);



    }
}

/*

namespace ParrotsAPI2.Services
{
    public interface ICharacterService
    {

        Task<ServiceResponse<List<GetCharacterDto>>> GetAllCharacters();
        Task<ServiceResponse<GetCharacterDto>> GetCharacterById(int id);
        Task<ServiceResponse<List<GetCharacterDto>>> AddCharacter(AddCharacterDto newCharacter);
        Task<ServiceResponse<GetCharacterDto>> UpdateCharacter(UpdateCharacterDto updatedCharacter);
        Task<ServiceResponse<List<GetCharacterDto>>> DeleteCharacter(int id);

    }
}
*/