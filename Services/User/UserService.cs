using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Dtos.User;
using ParrotsAPI2.Dtos.VehicleImageDtos;
using ParrotsAPI2.Models;
using ParrotsAPI2.Services;
using System.Diagnostics;
using Microsoft.Extensions.Logging; // Ensure you have a logger


namespace ParrotsAPI2.Services.User
{
    public class UserService : IUserService
    {


        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly IBlobService _blobService; // 🟢 CHANGED

        // 🟢 CHANGED - Added BlobService to constructor
        public UserService(IMapper mapper, DataContext context, ILogger<UserService> logger, IBlobService blobService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _blobService = blobService;
        }


        // 🔹 Helper: process image and upload full + thumbnail versions
        private async Task<(string fullPath, string thumbPath)> ProcessAndUploadAsync(IFormFile file, string prefix)
        {
            const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

            if (file == null || file.Length == 0)
                throw new ArgumentException("No image provided");

            if (file.Length > MaxFileSize)
                throw new ArgumentException("Image size exceeds 5MB limit");

            var guid = Guid.NewGuid().ToString();
            var fullKey = $"{prefix.TrimEnd('/')}/{guid}.webp";
            var thumbKey = $"{prefix.TrimEnd('/')}/{guid}_thumb.webp";

            var (fullStream, thumbStream) = await ImageProcessor.ProcessAsync(file.OpenReadStream());

            var fullPath = await _blobService.UploadAsync(fullStream, fullKey, "image/webp");
            var thumbPath = await _blobService.UploadAsync(thumbStream, thumbKey, "image/webp");

            return (fullPath, thumbPath);
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

                // Map and add new user first (without image)
                var user = _mapper.Map<AppUser>(newUser);
                user.DisplayEmail = newUser.Email;

                _context.Users.Add(user);
                await _context.SaveChangesAsync(); // generates user.Id

                // If an image was provided, upload with userId-based prefix
                if (newUser.ImageFile != null && newUser.ImageFile.Length > 0)
                {
                    try
                    {
                        var prefix = $"user-images/{user.Id}";
                        var (fullPath, thumbPath) = await ProcessAndUploadAsync(newUser.ImageFile, prefix);

                        user.ProfileImageUrl = fullPath;
                        user.ProfileImageThumbnailUrl = thumbPath;
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading image for new user {UserId}", user.Id);
                        serviceResponse.Success = false;
                        serviceResponse.Message = $"Error uploading image: {ex.Message}";
                        return serviceResponse;
                    }
                }

                // Return updated user list
                var updatedUsers = await _context.Users.ToListAsync();
                serviceResponse.Data = updatedUsers.Select(u => _mapper.Map<GetUserDto>(u)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user");
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

            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Vehicles)
                .Include(u => u.Voyages)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id);

            stopwatch.Stop();

            if (user == null)
            {
                serviceResponse.Message = "User not found";
                _logger.LogInformation($"GetUserById request took {stopwatch.ElapsedMilliseconds} ms");
                return serviceResponse;
            }

            var confirmedVehicles = user.Vehicles?.Where(v => v.Confirmed && !v.IsDeleted).ToList() ?? new List<Models.Vehicle>();
            var confirmedVoyages = user.Voyages?.Where(v => v.Confirmed && !v.IsDeleted && v.PlaceType == 0).ToList() ?? new List<Models.Voyage>();

            var userDto = new GetUserDto
            {
                Id = user.Id,
                PublicId = user.PublicId ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Title = user.Title ?? string.Empty,
                Bio = user.Bio ?? string.Empty,
                Email = user.Email ?? string.Empty,
                DisplayEmail = user.DisplayEmail ?? string.Empty,
                Instagram = user.Instagram ?? string.Empty,
                Twitter = user.Twitter ?? string.Empty,
                Tiktok = user.Tiktok ?? string.Empty,
                Linkedin = user.Linkedin ?? string.Empty,
                Facebook = user.Facebook ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Youtube = user.Youtube ?? string.Empty,
                ProfileImageUrl = user.ProfileImageUrl ?? string.Empty,
                ProfileImageThumbnailUrl = user.ProfileImageThumbnailUrl ?? string.Empty,
                BackgroundImageUrl = user.BackgroundImageUrl ?? string.Empty,
                UnseenMessages = user.UnseenMessages,
                EmailVisible = user.EmailVisible,
                ParrotCoinBalance = user.ParrotCoinBalance,
                UsersVehicles = _mapper.Map<List<GetUsersVehiclesDto>>(confirmedVehicles),
                UsersVoyages = _mapper.Map<List<GetUsersVoyagesDto>>(confirmedVoyages),
            };

            serviceResponse.Data = userDto;
            _logger.LogInformation($"GetUserById request took {stopwatch.ElapsedMilliseconds} ms");
            return serviceResponse;
        }


        public async Task<ServiceResponse<GetUserDto>> GetSingleUserByUsername(string username)
        {
            var serviceResponse = new ServiceResponse<GetUserDto>(); //xxxxxxx
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Validate input id early
            if (string.IsNullOrEmpty(username) || username == "null")
            {
                serviceResponse.Data = null;
                serviceResponse.Message = "username is null or invalid";
                stopwatch.Stop();
                _logger.LogInformation($"GetSingleUserByUsername request took {stopwatch.ElapsedMilliseconds} ms");
                return serviceResponse;
            }

            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Vehicles)
                .Include(u => u.Voyages)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.UserName == username);

            stopwatch.Stop();

            if (user == null)
            {
                serviceResponse.Message = "User not found";
                _logger.LogInformation($"GetSingleUserByUsername request took {stopwatch.ElapsedMilliseconds} ms");
                return serviceResponse;
            }

            var confirmedVehicles = user.Vehicles?.Where(v => v.Confirmed && !v.IsDeleted).ToList() ?? new List<Models.Vehicle>();
            var confirmedVoyages = user.Voyages?.Where(v => v.Confirmed && !v.IsDeleted && v.PlaceType == 0).ToList() ?? new List<Models.Voyage>();

            var userDto = new GetUserDto
            {
                Id = user.Id,
                PublicId = user.PublicId ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Title = user.Title ?? string.Empty,
                Bio = user.Bio ?? string.Empty,
                Email = user.Email ?? string.Empty,
                DisplayEmail = user.DisplayEmail ?? string.Empty,
                Instagram = user.Instagram ?? string.Empty,
                Twitter = user.Twitter ?? string.Empty,
                Tiktok = user.Tiktok ?? string.Empty,
                Linkedin = user.Linkedin ?? string.Empty,
                Facebook = user.Facebook ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Youtube = user.Youtube ?? string.Empty,
                ProfileImageUrl = user.ProfileImageUrl ?? string.Empty,
                ProfileImageThumbnailUrl = user.ProfileImageThumbnailUrl ?? string.Empty,
                BackgroundImageUrl = user.BackgroundImageUrl ?? string.Empty,
                UnseenMessages = user.UnseenMessages,
                EmailVisible = user.EmailVisible,
                ParrotCoinBalance = user.ParrotCoinBalance,
                UsersVehicles = _mapper.Map<List<GetUsersVehiclesDto>>(confirmedVehicles),
                UsersVoyages = _mapper.Map<List<GetUsersVoyagesDto>>(confirmedVoyages),
            };

            serviceResponse.Data = userDto;
            _logger.LogInformation($"GetSingleUserByUsername request took {stopwatch.ElapsedMilliseconds} ms");
            return serviceResponse;
        }

        public async Task<ServiceResponse<UserDto>> GetSingleUserByUsername2(string username)
        {
            var serviceResponse = new ServiceResponse<UserDto>();

            if (string.IsNullOrWhiteSpace(username))
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Username is null or empty";
                serviceResponse.Data = null;
                return serviceResponse;
            }

            // Find the first matching user (exclude "admin")
            var user = await _context.Users
                .Where(u => u.UserName != null
                            && u.UserName.Contains(username)
                            && u.UserName.ToLower() != "admin")
                .FirstOrDefaultAsync();

            if (user != null)
            {
                serviceResponse.Data = new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    ProfileImageUrl = user.ProfileImageUrl,
                    ProfileImageThumbnailUrl = user.ProfileImageThumbnailUrl,
                    PublicId = user.PublicId
                };
                serviceResponse.Success = true;
            }
            else
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "No user found with the specified username";
                serviceResponse.Data = null;
            }

            return serviceResponse;
        }



        public async Task<ServiceResponse<GetUserDto>> GetUserByPublicId(string publicId)
        {
            var serviceResponse = new ServiceResponse<GetUserDto>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Validate input id early
            if (string.IsNullOrEmpty(publicId) || publicId == "null")
            {
                serviceResponse.Data = null;
                serviceResponse.Message = "PublicId is null or invalid";
                stopwatch.Stop();
                _logger.LogInformation($"GetUserByPublicId request took {stopwatch.ElapsedMilliseconds} ms");
                return serviceResponse;
            }

            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Vehicles)
                .Include(u => u.Voyages)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.PublicId == publicId);

            stopwatch.Stop();

            if (user == null)
            {
                serviceResponse.Message = "User not found";
                _logger.LogInformation($"GetUserByPublicId request took {stopwatch.ElapsedMilliseconds} ms");
                return serviceResponse;
            }

            var confirmedVehicles = user.Vehicles?.Where(v => v.Confirmed && !v.IsDeleted).ToList() ?? new List<Models.Vehicle>();
            var confirmedVoyages = user.Voyages?.Where(v => v.Confirmed && !v.IsDeleted && v.PlaceType == 0).ToList() ?? new List<Models.Voyage>();

            var userDto = new GetUserDto
            {
                Id = user.Id,
                PublicId = user.PublicId ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Title = user.Title ?? string.Empty,
                Bio = user.Bio ?? string.Empty,
                Email = user.Email ?? string.Empty,
                DisplayEmail = user.DisplayEmail ?? string.Empty,
                Instagram = user.Instagram ?? string.Empty,
                Twitter = user.Twitter ?? string.Empty,
                Tiktok = user.Tiktok ?? string.Empty,
                Linkedin = user.Linkedin ?? string.Empty,
                Facebook = user.Facebook ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Youtube = user.Youtube ?? string.Empty,
                ProfileImageUrl = user.ProfileImageUrl ?? string.Empty,
                ProfileImageThumbnailUrl = user.ProfileImageThumbnailUrl ?? string.Empty,
                BackgroundImageUrl = user.BackgroundImageUrl ?? string.Empty,
                UnseenMessages = user.UnseenMessages,
                EmailVisible = user.EmailVisible,
                ParrotCoinBalance = user.ParrotCoinBalance,
                UsersVehicles = _mapper.Map<List<GetUsersVehiclesDto>>(confirmedVehicles),
                UsersVoyages = _mapper.Map<List<GetUsersVoyagesDto>>(confirmedVoyages),
            };

            serviceResponse.Data = userDto;
            _logger.LogInformation($"GetUserByPublicId request took {stopwatch.ElapsedMilliseconds} ms");
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
                _logger.LogError(ex, "Error updating user {UserId}", updatedUser.Id);
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
                _logger.LogError(ex, "Error patching user {UserId}", userId);
                // ✅ Clean exception handling with message
                serviceResponse.Success = false;
                serviceResponse.Message = $"Error while patching user: {ex.Message}";
            }

            return serviceResponse;
        }


        public async Task<ServiceResponse<GetUserDto>> PatchUserAdmin(
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
                _logger.LogError(ex, "Error patching user {UserId} (admin)", userId);
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
                var prefix = $"user-images/{userId}";
                var (fullPath, thumbPath) = await ProcessAndUploadAsync(imageFile, prefix);
                user.ProfileImageUrl = fullPath;
                user.ProfileImageThumbnailUrl = thumbPath;

                await _context.SaveChangesAsync();
                serviceResponse.Data = _mapper.Map<GetUserDto>(user);
                serviceResponse.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile image for user {UserId}", userId);
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
                var prefix = $"user-images/{userId}";
                var (fullPath, _) = await ProcessAndUploadAsync(imageFile, prefix);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = "User not found";
                    return serviceResponse;
                }

                user.BackgroundImageUrl = fullPath;
                await _context.SaveChangesAsync();

                serviceResponse.Data = _mapper.Map<GetUserDto>(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading background image for user {UserId}", userId);
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
                _logger.LogError(ex, "Error updating unseen messages for user {UserId}", updatedUser.Id);
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
                 .Where(u => u.UserName != null
                             && EF.Functions.ILike(u.UserName, $"%{username}%")
                             && u.UserName.ToLower() != "admin") // exclude "admin"
                 .ToListAsync();

            if (searchUsers.Any())
            {
                serviceResponse.Data = searchUsers.Select(user => new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    ProfileImageUrl = user.ProfileImageUrl,
                    ProfileImageThumbnailUrl = user.ProfileImageThumbnailUrl,
                    PublicId = user.PublicId
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

        public async Task<ServiceResponse<int>> PurchaseCoins(string userId, int coins, decimal eurAmount, string PaymentProviderId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new ServiceResponse<int>
                {
                    Success = false,
                    Message = "User not found."
                };
            }
            user.ParrotCoinBalance += coins;
            var purchase = new CoinPurchase
            {
                UserId = userId,
                CoinsAmount = coins,
                EurAmount = eurAmount,
                Status = "completed",
                CreatedAt = DateTime.UtcNow,
                PaymentProviderId = PaymentProviderId
            };
            _context.CoinPurchases.Add(purchase);
            await _context.SaveChangesAsync();
            return new ServiceResponse<int>
            {
                Data = user.ParrotCoinBalance,
                Success = true,
                Message = $"{coins} coins deposited successfully and purchase recorded."
            };
        }

        public async Task<ServiceResponse<int>> ClaimFreeCoins(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new ServiceResponse<int> { Success = false, Message = "User not found." };
            }
            if (user.ParrotCoinBalance >= 500)
            {
                return new ServiceResponse<int> { Success = false, Message = "Balance must be below 500 to claim free coins." };
            }
            user.ParrotCoinBalance += 100;
            var purchase = new CoinPurchase
            {
                UserId = userId,
                CoinsAmount = 100,
                EurAmount = 0,
                Status = "completed",
                CreatedAt = DateTime.UtcNow,
                PaymentProviderId = "free_claim"
            };
            _context.CoinPurchases.Add(purchase);
            await _context.SaveChangesAsync();
            return new ServiceResponse<int>
            {
                Data = user.ParrotCoinBalance,
                Success = true,
                Message = "100 free coins claimed successfully."
            };
        }



        public async Task<ServiceResponse<int>> SendParrotCoins(string userId, string receiverId, int coins)
        {
            var user = await _context.Users.FindAsync(userId);
            var receiver = await _context.Users.FindAsync(receiverId);
            if (user == null || receiver == null)
            {
                return new ServiceResponse<int>
                {
                    Success = false,
                    Message = "User or receiver not found."
                };
            }

            if (user.ParrotCoinBalance < coins)
            {
                return new ServiceResponse<int>
                {
                    Success = false,
                    Message = "Insufficient balance."
                };
            }

            var result = new ServiceResponse<int>();
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                // Re-fetch inside transaction to get latest balance and prevent race conditions
                _context.ChangeTracker.Clear();
                var sender = await _context.Users.FindAsync(userId);
                var recv = await _context.Users.FindAsync(receiverId);

                if (sender == null || recv == null || sender.ParrotCoinBalance < coins)
                {
                    result.Success = false;
                    result.Message = "Insufficient balance.";
                    return;
                }

                sender.ParrotCoinBalance -= coins;
                recv.ParrotCoinBalance += coins;

                _context.CoinTransactions.Add(new CoinTransaction
                {
                    UserId = userId,
                    Amount = -coins,
                    Type = "send_parrotCoins",
                    Description = $"Sent to {recv.UserName}",
                    VoyageId = 0,
                    CreatedAt = DateTime.UtcNow
                });
                _context.CoinTransactions.Add(new CoinTransaction
                {
                    UserId = receiverId,
                    Amount = coins,
                    Type = "receive_parrotCoins",
                    Description = $"Received from {sender.UserName}",
                    VoyageId = 0,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.Data = sender.ParrotCoinBalance;
                result.Success = true;
                result.Message = $"{coins} coins sent successfully and recorded.";
            });

            return result;
        }



        public async Task<ServiceResponse<ParrotCoinSummaryDto>> GetParrotCoinBalanceAndPurchases(string userId)
        {
            var user = await _context.Users
                .Include(u => u.CoinPurchases)
                .Include(u => u.CoinTransactions)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new ServiceResponse<ParrotCoinSummaryDto>
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            var dto = new ParrotCoinSummaryDto
            {
                Balance = user.ParrotCoinBalance,
                Purchases = (user.CoinPurchases ?? Enumerable.Empty<CoinPurchase>())
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new CoinPurchaseDto
                    {
                        Id = p.Id,
                        EurAmount = p.EurAmount,
                        CoinsAmount = p.CoinsAmount,
                        Status = p.Status ?? string.Empty,
                        PaymentProviderId = p.PaymentProviderId,
                        CreatedAt = p.CreatedAt
                    })
                    .ToList(),
                Transactions = (user.CoinTransactions ?? Enumerable.Empty<CoinTransaction>())
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new CoinTransactionDto
                    {
                        Id = p.Id,
                        CoinsAmount = p.Amount,
                        Description = p.Description,
                        CreatedAt = p.CreatedAt
                    })
                    .ToList()
            };
            var a = 0;
            return new ServiceResponse<ParrotCoinSummaryDto>
            {
                Data = dto,
                Success = true,
                Message = "Balance and purchases retrieved successfully."
            };
        }


    }
}