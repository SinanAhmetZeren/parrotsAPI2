using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Dtos.User;

namespace ParrotsAPI2.Services.User
{
    public class UserService : IUserService
    {


        private readonly IMapper _mapper;
        private readonly DataContext _context;

        public UserService(IMapper mapper, DataContext context)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ServiceResponse<List<GetUserDto>>> AddUser(AddUserDto newUser)
        {
            var serviceResponse = new ServiceResponse<List<GetUserDto>>();

            if (newUser.ImageFile != null && newUser.ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(newUser.ImageFile.FileName);
                //var filePath = Path.Combine("wwwroot/images/", fileName);
                var filePath = Path.Combine("Uploads/UserImages/", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newUser.ImageFile.CopyToAsync(stream);
                }
                //newUser.ProfileImageUrl = "/images/" + fileName;
                newUser.ProfileImageUrl = "/Uploads/UserImages/" + fileName; 
            }

            var user = _mapper.Map<Models.User>(newUser);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var updatedUsers = await _context.Users.ToListAsync();
            serviceResponse.Data = updatedUsers.Select(c => _mapper.Map<GetUserDto>(c)).ToList();

            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetUserDto>>> DeleteUser(int id)
        {
            var serviceResponse = new ServiceResponse<List<GetUserDto>>();
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    throw new Exception($"User with ID `{id}` not found");
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                var users = await _context.Users.ToListAsync();
                serviceResponse.Data = users.Select(c => _mapper.Map<GetUserDto>(c)).ToList();
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetUserDto>>> GetAllUsers()
        {
            var serviceResponse = new ServiceResponse<List<GetUserDto>>();
            var dbUsers = await _context.Users.ToListAsync();
            serviceResponse.Data = dbUsers.Select(c => _mapper.Map<GetUserDto>(c)).ToList();
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetUserDto>> GetUserById(int id)
        {
            var serviceResponse = new ServiceResponse<GetUserDto>();
            var dbUsers = await _context.Users.ToListAsync();
            var user = dbUsers.FirstOrDefault(c => c.Id == id);
            serviceResponse.Data = _mapper.Map<GetUserDto>(user);
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetUserDto>> UpdateUser(UpdateUserDto updatedUser)
        {
            var serviceResponse = new ServiceResponse<GetUserDto>();
            try
            {
                var user = await _context.Users.FindAsync(updatedUser.Id);
                if (user == null)
                {
                    throw new Exception($"Character with ID `{updatedUser.Id}` not found");
                }
                user.Name = updatedUser.Name;
                user.Title = updatedUser.Title;
                user.Bio = updatedUser.Bio;
                user.Email = updatedUser.Email;
                user.Instagram = updatedUser.Instagram;
                user.Facebook = updatedUser.Facebook;
                user.PhoneNumber = updatedUser.PhoneNumber;
                user.ProfileImageUrl = updatedUser.ProfileImageUrl;

            await _context.SaveChangesAsync();
                    serviceResponse.Data = _mapper.Map<GetUserDto>(user);
                }
                catch (Exception ex)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = ex.Message;
                }

                return serviceResponse;
        }

        public async Task<ServiceResponse<GetUserDto>> PatchUser(int userId,[FromBody]JsonPatchDocument<UpdateUserDto> patchDoc,ModelStateDictionary modelState)
        {
            var serviceResponse = new ServiceResponse<GetUserDto>();
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new Exception($"User with ID `{userId}` not found");
                }

                var userDto = _mapper.Map<UpdateUserDto>(user);
                patchDoc.ApplyTo(userDto, modelState);

                if (!modelState.IsValid)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Invalid model state after patch operations";
                    return serviceResponse;
                }
                _mapper.Map(userDto, user);
                _context.Users.Attach(user);
                _context.Entry(user).State = EntityState.Modified;

                await _context.SaveChangesAsync();
                serviceResponse.Data = _mapper.Map<GetUserDto>(user);
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<GetUserDto>> UpdateUserProfileImage(int userId, IFormFile imageFile)
        {
            var serviceResponse = new ServiceResponse<GetUserDto>();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "User not found";
                return serviceResponse;
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine("Uploads/UserImages/", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                user.ProfileImageUrl = "/Uploads/UserImages/" + fileName;
                await _context.SaveChangesAsync();
                var userDto = _mapper.Map<GetUserDto>(user);

                serviceResponse.Success = true;
                serviceResponse.Data = userDto;
            }
            else
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "No image provided";
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<GetUserDto>> UpdateUserUnseenMessage(UpdateUserUnseenMessageDto updatedUser)
        {
            var serviceResponse = new ServiceResponse<GetUserDto>();
            try
            {
                var user = await _context.Users.FindAsync(updatedUser.Id);
                if (user == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "User not found";
                    return serviceResponse;
                }
                user.UnseenMessages = updatedUser.UnseenMessages;
                await _context.SaveChangesAsync();
                var updatedUserDto = _mapper.Map<GetUserDto>(user);
                serviceResponse.Data = updatedUserDto;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error updating user unseen messages: {ex.Message}";
            }
            return serviceResponse;
        }
    }


}
