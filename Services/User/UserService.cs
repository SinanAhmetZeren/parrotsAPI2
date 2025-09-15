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
        private readonly BlobService _blobService; // 🟢 CHANGED

        // 🟢 CHANGED - Added BlobService to constructor
        public UserService(IMapper mapper, DataContext context, ILogger<UserService> logger, BlobService blobService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _blobService = blobService;
        }


        // 🔹 Helper method for uploading images
        private async Task<string> UploadImageToBlobAsync(IFormFile file)
        {
            const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

            if (file == null || file.Length == 0)
                throw new ArgumentException("No image provided");

            if (file.Length > MaxFileSize)
                throw new ArgumentException("Image size exceeds 5MB limit");

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            return await _blobService.UploadAsync(file.OpenReadStream(), fileName);
        }


        public async Task<ServiceResponse<List<GetUserDto>>> AddUser(AddUserDto newUser)
        {
            var serviceResponse = new ServiceResponse<List<GetUserDto>>();

            try
            {
                // Check if username or email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == newUser.UserName || u.Email == newUser.Email);

                if (existingUser != null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Username or email already exists.";
                    return serviceResponse;
                }

                // Handle image upload using BlobService helper
                if (newUser.ImageFile != null && newUser.ImageFile.Length > 0)
                {
                    try
                    {
                        newUser.ProfileImageUrl = await UploadImageToBlobAsync(newUser.ImageFile);
                    }
                    catch (Exception ex)
                    {
                        serviceResponse.Success = false;
                        serviceResponse.Message = $"Error uploading image: {ex.Message}";
                        return serviceResponse;
                    }
                }

                // Map and add new user
                var user = _mapper.Map<AppUser>(newUser);
                user.DisplayEmail = newUser.Email;
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Return updated user list
                var updatedUsers = await _context.Users.ToListAsync();
                serviceResponse.Data = updatedUsers.Select(u => _mapper.Map<GetUserDto>(u)).ToList();
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error adding user: {ex.Message}";
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

            // Validate input id early
            if (string.IsNullOrEmpty(id) || id == "null")
            {
                serviceResponse.Data = null;
                serviceResponse.Message = "Id is null or invalid";
                stopwatch.Stop();
                _logger.LogInformation($"GetUserById request took {stopwatch.ElapsedMilliseconds} ms");
                return serviceResponse;
            }

            // Fetch user with related entities using AsNoTracking and split query for better performance on multiple Includes
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

            if (user == null)
            {
                serviceResponse.Message = "User not found";
                _logger.LogInformation($"GetUserById request took {stopwatch.ElapsedMilliseconds} ms");
                return serviceResponse;
            }

            var confirmedVehicles = (user?.Vehicles != null)
            ? user.Vehicles.Where(v => v.Confirmed && !v.IsDeleted).ToList()
            : new List<Models.Vehicle>();

            var vehicleDtos = _mapper.Map<List<GetUsersVehiclesDto>>(confirmedVehicles);

            var confirmedVoyages = (user?.Voyages != null)
                ? user.Voyages.Where(v => v.Confirmed && !v.IsDeleted).ToList()
                : new List<Models.Voyage>();

            var voyageDtos = _mapper.Map<List<GetUsersVoyagesDto>>(confirmedVoyages);

            // Create DTO explicitly instead of AutoMapper for this part - can also be mapped if preferred
            var userDto = new GetUserDto
            {
                Id = user?.Id ?? string.Empty,
                UserName = user?.UserName ?? string.Empty,
                Title = user?.Title ?? string.Empty,
                Bio = user?.Bio ?? string.Empty,
                Email = user?.Email ?? string.Empty,
                DisplayEmail = user?.DisplayEmail ?? string.Empty,
                Instagram = user?.Instagram ?? string.Empty,
                Twitter = user?.Twitter ?? string.Empty,
                Tiktok = user?.Tiktok ?? string.Empty,
                Linkedin = user?.Linkedin ?? string.Empty,
                Facebook = user?.Facebook ?? string.Empty,
                PhoneNumber = user?.PhoneNumber ?? string.Empty,
                Youtube = user?.Youtube ?? string.Empty,
                ProfileImageUrl = user?.ProfileImageUrl ?? string.Empty,
                BackgroundImageUrl = user?.BackgroundImageUrl ?? string.Empty,
                ImageFile = default!,
                UnseenMessages = user != null ? user.UnseenMessages : false,
                UsersVehicles = vehicleDtos,
                UsersVoyages = voyageDtos,
                // EmailVisible = user!= null ? user.EmailVisible : false,
                EmailVisible = true,
            };

            serviceResponse.Data = userDto;
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
                // user.Email = updatedUser.Email;
                user.DisplayEmail = updatedUser.DisplayEmail;
                user.Instagram = updatedUser.Instagram;
                user.Facebook = updatedUser.Facebook;
                user.Twitter = updatedUser.Twitter;
                user.Tiktok = updatedUser.Tiktok;
                user.Linkedin = updatedUser.Linkedin;
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

        public async Task<ServiceResponse<GetUserDto>> PatchUser(
            string userId,
            [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc,
            ModelStateDictionary modelState)
        {
            var serviceResponse = new ServiceResponse<GetUserDto>();
            try
            {
                // ✅ Check if patchDoc is null before proceeding
                if (patchDoc == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Patch document cannot be null.";
                    return serviceResponse;
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    // ✅ Clean error message when user not found
                    serviceResponse.Success = false;
                    serviceResponse.Message = $"User with ID `{userId}` not found.";
                    return serviceResponse;
                }

                // ✅ Map AppUser → UpdateUserDto
                var userDto = _mapper.Map<UpdateUserDto>(user);

                // ✅ Apply patch and pass ModelState for validation feedback
                patchDoc.ApplyTo(userDto, modelState);

                // ✅ Use ModelStateDictionary to catch validation errors
                if (!modelState.IsValid)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "Invalid model state after applying patch operations.";
                    return serviceResponse;
                }

                // ✅ Map patched DTO back to entity
                _mapper.Map(userDto, user);

                // ✅ Mark entity as modified and persist changes
                _context.Users.Attach(user);
                _context.Entry(user).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                // ✅ Map result to GetUserDto for response
                serviceResponse.Data = _mapper.Map<GetUserDto>(user);
            }
            catch (Exception ex)
            {
                // ✅ Clean exception handling with message
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error while patching user: {ex.Message}";
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<GetUserDto>> UpdateUserProfileImage(string userId, IFormFile imageFile)
        {
            var serviceResponse = new ServiceResponse<GetUserDto>();

            // Check for null image file first
            if (imageFile == null || imageFile.Length == 0)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "No image provided";
                return serviceResponse;
            }

            const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
            if (imageFile.Length > MaxFileSize)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Image size exceeds 5MB limit";
                return serviceResponse;
            }

            // Find the user
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "User not found";
                return serviceResponse;
            }

            try
            {
                // Upload image to Blob storage
                var fileName = await UploadImageToBlobAsync(imageFile);
                user.ProfileImageUrl = fileName;

                await _context.SaveChangesAsync();
                serviceResponse.Data = _mapper.Map<GetUserDto>(user);
                serviceResponse.Success = true;
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Image upload failed: {ex.Message}";
            }

            return serviceResponse;
        }


        public async Task<ServiceResponse<GetUserDto>> UpdateUserBackgroundImage(string userId, IFormFile imageFile)
        {
            var serviceResponse = new ServiceResponse<GetUserDto>();

            if (imageFile == null || imageFile.Length == 0)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "No image provided";
                return serviceResponse;
            }

            try
            {
                // Use helper function to handle upload and validation
                var fileName = await UploadImageToBlobAsync(imageFile);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "User not found";
                    return serviceResponse;
                }

                user.BackgroundImageUrl = fileName;
                await _context.SaveChangesAsync();

                serviceResponse.Data = _mapper.Map<GetUserDto>(user);
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = $"Image upload failed: {ex.Message}";
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
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(username))
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Username is null or empty";
                serviceResponse.Data = null;
                return serviceResponse;
            }
            var searchUsers = await _context.Users
                .Where(u => u.UserName != null && u.UserName.Contains(username))
                .ToListAsync();

            if (searchUsers.Any())
            {
                serviceResponse.Data = searchUsers.Select(user => new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    ProfileImageUrl = user.ProfileImageUrl,
                }).ToList();

                serviceResponse.Success = true;
            }
            else
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "No users found with the specified username";
                serviceResponse.Data = new List<UserDto>();
            }

            return serviceResponse;
        }


    }
}
