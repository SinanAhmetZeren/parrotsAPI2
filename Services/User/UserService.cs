using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Dtos.User;
using ParrotsAPI2.Dtos.VehicleImageDtos;
using ParrotsAPI2.Models;
using System.Diagnostics;
using Microsoft.Extensions.Logging; // Ensure you have a logger


namespace ParrotsAPI2.Services.User
{
    public class UserService : IUserService
    {


        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly ILogger<UserService> _logger; 


        public UserService(IMapper mapper, DataContext context, ILogger<UserService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;

        }

        public async Task<ServiceResponse<List<GetUserDto>>> AddUser(AddUserDto newUser)
        {
            var serviceResponse = new ServiceResponse<List<GetUserDto>>();

            if (newUser.ImageFile != null && newUser.ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(newUser.ImageFile.FileName);
                var filePath = Path.Combine("Uploads/UserImages/", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newUser.ImageFile.CopyToAsync(stream);
                }
                newUser.ProfileImageUrl = "/Uploads/UserImages/" + fileName; 
            }

            var user = _mapper.Map<AppUser>(newUser);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            var updatedUsers = await _context.Users.ToListAsync();
            serviceResponse.Data = updatedUsers.Select(c => _mapper.Map<GetUserDto>(c)).ToList();
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetUserDto>>> DeleteUser(string id)
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
            var dbUsers = await _context.Users
                .Include(u => u.SentMessages)
                .Include(u => u.ReceivedMessages)
                .ToListAsync();
            serviceResponse.Data = dbUsers.Select(c => _mapper.Map<GetUserDto>(c)).ToList();
            return serviceResponse;

        }

        public async Task<ServiceResponse<GetUserDto>> GetUserById(string id)
        {
            var serviceResponse = new ServiceResponse<GetUserDto>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (string.IsNullOrEmpty(id))
            {
                serviceResponse.Data = null;
                serviceResponse.Message = "Id is null";
                stopwatch.Stop();
                _logger.LogInformation($"GetUserById request took {stopwatch.ElapsedMilliseconds} ms");
                return serviceResponse;
            }

            if (id == "null") {
                serviceResponse.Data = null;
                serviceResponse.Message = "Id is null";
                return serviceResponse; 
                };

            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.SentMessages)
                .Include(u => u.ReceivedMessages)
                .Include(u => u.Vehicles)
                .Include(u => u.Voyages)
                .Include(u => u.Bids)
                .AsSplitQuery() 
                .FirstOrDefaultAsync(c => c.Id == id);

            stopwatch.Stop();

            var usersVehicles = user.Vehicles;
            List<GetUsersVehiclesDto> vehicleDtos = _mapper.Map<List<GetUsersVehiclesDto>>(usersVehicles);

            var usersVoyages = user.Voyages;
            List<GetUsersVoyagesDto> voyageDtos = _mapper.Map<List<GetUsersVoyagesDto>>(usersVoyages);

            if (user != null)
            {
                var userDto = new GetUserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Title = user.Title,
                    Bio = user.Bio,
                    Email = user.Email,
                    Instagram = user.Instagram,
                    Facebook = user.Facebook,
                    PhoneNumber = user.PhoneNumber,
                    Youtube = user.Youtube,
                    ProfileImageUrl = user.ProfileImageUrl,
                    BackgroundImageUrl = user.BackgroundImageUrl,
                    ImageFile = null,
                    UnseenMessages = user.UnseenMessages,
                    UsersVehicles = vehicleDtos,
                    UsersVoyages = voyageDtos,
                    EmailVisible = user.EmailVisible,
                    
                };

                serviceResponse.Data = userDto;
            }
            else
            {
                serviceResponse.Message = "User not found";
            }
            _logger.LogInformation($"GetUserById request took {stopwatch.ElapsedMilliseconds} ms");

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
                user.UserName = updatedUser.UserName;
                user.Title = updatedUser.Title;
                user.Bio = updatedUser.Bio;
                user.Email = updatedUser.Email;
                user.Instagram = updatedUser.Instagram;
                user.Facebook = updatedUser.Facebook;
                user.PhoneNumber = updatedUser.PhoneNumber;
                user.Youtube = updatedUser.Youtube;
                user.ProfileImageUrl = updatedUser.ProfileImageUrl;
                user.BackgroundImageUrl = updatedUser.BackgroundImageUrl;

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

        public async Task<ServiceResponse<GetUserDto>> PatchUser(string userId,[FromBody]JsonPatchDocument<UpdateUserDto> patchDoc,ModelStateDictionary modelState)
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

        public async Task<ServiceResponse<GetUserDto>> UpdateUserProfileImage(string userId, IFormFile imageFile)
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
                user.ProfileImageUrl = fileName;
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

        public async Task<ServiceResponse<GetUserDto>> UpdateUserBackgroundImage(string userId, IFormFile imageFile)
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
                user.BackgroundImageUrl =  fileName;
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

        public async Task<ServiceResponse<List<UserDto>>> GetUsersByUsername(string username)
        {
            var serviceResponse = new ServiceResponse<List<UserDto>>();

            if (username  == "null")
            {
                serviceResponse.Data = null;
                serviceResponse.Message = "Username is null";
                return serviceResponse;
            }

            var searchUsers = await _context.Users
                        .Where(u => u.UserName.Contains(username)).ToListAsync();

            if (searchUsers != null && searchUsers.Any())
            {
                var userDtos = searchUsers.Select(user => new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    ProfileImageUrl = user.ProfileImageUrl,
                }).ToList();

                serviceResponse.Data = userDtos;
            }
            else
            {
                serviceResponse.Message = "No users found with the specified username";
            }

            return serviceResponse;
        }


    }


}
