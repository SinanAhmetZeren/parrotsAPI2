using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ParrotsAPI2.Dtos.User;

namespace ParrotsAPI2.Services.User
{
    public interface IUserService
    {
        Task<ServiceResponse<List<GetUserDto>>> GetAllUsers();
        Task<ServiceResponse<GetUserDto>> GetUserById(int id);
        Task<ServiceResponse<List<GetUserDto>>> AddUser(AddUserDto newUser);
        Task<ServiceResponse<GetUserDto>> UpdateUser(UpdateUserDto updatedUser);
        Task<ServiceResponse<List<GetUserDto>>> DeleteUser(int id);
        Task<ServiceResponse<GetUserDto>> PatchUser(int userId, JsonPatchDocument<UpdateUserDto> patchDoc, ModelStateDictionary modelState);
        Task<ServiceResponse<GetUserDto>> UpdateUserProfileImage(int userId, IFormFile imageFile);


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