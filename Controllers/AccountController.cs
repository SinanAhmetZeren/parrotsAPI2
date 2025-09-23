using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Dtos.RegisterLoginDtos;
using ParrotsAPI2.Helpers;
using ParrotsAPI2.Models;
using ParrotsAPI2.Services.Token;
using Google.Apis.Auth;
using System.Text.Json;
using Microsoft.Extensions.Options;


namespace API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly TokenService _tokenService;
        private readonly string _googleClientId;

        public AccountController(
            UserManager<AppUser> userManager,
            TokenService tokenService,
            IOptions<GoogleAuthOptions> googleOptions
        )
        {
            _tokenService = tokenService;
            _userManager = userManager;
            _googleClientId = googleOptions.Value.ClientId;

        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<UserResponseDto>> Login(LoginDto loginDto)
        {
            var normalizedEmail = _userManager.NormalizeEmail(loginDto.Email);
            AppUser? user = await _userManager.Users
                .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail);

            if (user == null || !user.Confirmed)
            {
                return Unauthorized("User not found or not confirmed");
            }
            var result = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (result)
            {
                // return CreateUserObject(user);
                var refreshToken = _tokenService.GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // refresh token valid for 7 days
                //user.RefreshTokenExpiryTime = DateTime.UtcNow.AddSeconds(20); // refresh token valid for 7 days
                var updatedUser = await _userManager.UpdateAsync(user);

                var userResponse = CreateUserObject(user);
                userResponse.RefreshToken = refreshToken;  // Add refresh token to response
                userResponse.RefreshTokenExpiryTime = user.RefreshTokenExpiryTime;
                return userResponse;
            }
            return Unauthorized("Invalid password");
        }



        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<UserResponseDto>> Register(RegisterDto registerDto)
        {
            CodeGenerator codeGenerator = new CodeGenerator();
            var normalizedEmail = _userManager.NormalizeEmail(registerDto.Email);
            var normalizedUserName = _userManager.NormalizeName(registerDto.UserName);
            string confirmationCode = codeGenerator.GenerateCode();

            var existingUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail || u.NormalizedUserName == normalizedUserName);

            if (existingUser != null)
            {
                if (existingUser.Confirmed)
                {
                    if (existingUser.NormalizedEmail == normalizedEmail &&
                        existingUser.NormalizedUserName == normalizedUserName)
                    {
                        ModelState.AddModelError("Email and Username", "Email and Username are already taken");
                    }
                    else if (existingUser.NormalizedEmail == normalizedEmail)
                    {
                        ModelState.AddModelError("Email", "Email is already taken");
                    }
                    else if (existingUser.NormalizedUserName == normalizedUserName)
                    {
                        ModelState.AddModelError("Username", "Username is already taken");
                    }
                    return ValidationProblem();
                }
                else
                {
                    // USER EXISTS AND NOT CONFIRMED
                    // https://parrotsstorage.blob.core.windows.net/parrotsuploads/parrot-looks.jpg
                    //string[] images = { "parrot-looks.jpg", "parrot-looks2.jpg", "parrot-looks3.jpg", "parrot-looks4.jpg", "parrot-looks5.jpg" };

                    string baseUrl = "https://parrotsstorage.blob.core.windows.net/parrotsuploads/";
                    string[] images =
                    {
                        $"{baseUrl}parrot-looks.jpg",
                        $"{baseUrl}parrot-looks2.jpg",
                        $"{baseUrl}parrot-looks3.jpg",
                        $"{baseUrl}parrot-looks4.jpg",
                        $"{baseUrl}parrot-looks5.jpg",
                        $"{baseUrl}parrot-looks6.jpg"
                    };
                    Random random = new Random();
                    int randomIndex = random.Next(0, images.Length);
                    string selectedImage = images[randomIndex];

                    existingUser.UserName = registerDto.UserName;
                    existingUser.ProfileImageUrl = selectedImage;
                    existingUser.ConfirmationCode = confirmationCode;
                    existingUser.NormalizedUserName = _userManager.NormalizeName(registerDto.UserName);
                    existingUser.NormalizedEmail = _userManager.NormalizeEmail(registerDto.Email);

                    var passwordHasher = new PasswordHasher<AppUser>();
                    existingUser.PasswordHash = passwordHasher.HashPassword(existingUser, registerDto.Password);

                    // Generate refresh token here
                    var refreshToken = _tokenService.GenerateRefreshToken();
                    existingUser.RefreshToken = refreshToken;
                    existingUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                    var updateResult = await _userManager.UpdateAsync(existingUser);
                    if (!updateResult.Succeeded)
                    {
                        return StatusCode(500);
                    }

                    EmailSender emailSender = new EmailSender();
                    _ = emailSender.SendConfirmationEmail(existingUser.Email ?? string.Empty, existingUser.ConfirmationCode, existingUser.UserName);

                    var userResponse = CreateUserObject(existingUser);
                    userResponse.RefreshToken = refreshToken;  // return refresh token
                    userResponse.RefreshTokenExpiryTime = existingUser.RefreshTokenExpiryTime;

                    return userResponse;
                }
            }
            else
            {
                // USER DOES NOT EXIST
                //string[] images = { "parrot-looks.jpg", "parrot-looks2.jpg", "parrot-looks3.jpg", "parrot-looks4.jpg", "parrot-looks5.jpg" };

                string baseUrl = "https://parrotsstorage.blob.core.windows.net/parrotsuploads/";
                string[] images =
                {
                        $"{baseUrl}parrot-looks.jpg",
                        $"{baseUrl}parrot-looks2.jpg",
                        $"{baseUrl}parrot-looks3.jpg",
                        $"{baseUrl}parrot-looks4.jpg",
                        $"{baseUrl}parrot-looks5.jpg",
                    };

                Random random = new Random();
                int randomIndex = random.Next(0, images.Length);
                string selectedImage = images[randomIndex];

                var newUser = new AppUser
                {
                    Email = registerDto.Email,
                    UserName = registerDto.UserName,
                    ProfileImageUrl = selectedImage,
                    ConfirmationCode = confirmationCode,
                    BackgroundImageUrl = "https://parrotsstorage.blob.core.windows.net/parrotsuploads/amazon.jpeg",
                    DisplayEmail = registerDto.Email,
                };

                newUser.EmailConfirmed = true;
                newUser.NormalizedEmail = _userManager.NormalizeEmail(registerDto.Email);
                newUser.NormalizedUserName = _userManager.NormalizeName(registerDto.UserName);

                var result = await _userManager.CreateAsync(newUser, registerDto.Password);
                if (result.Succeeded)
                {
                    // Generate refresh token here
                    var refreshToken = _tokenService.GenerateRefreshToken();
                    newUser.RefreshToken = refreshToken;
                    newUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                    await _userManager.UpdateAsync(newUser);

                    EmailSender emailSender = new EmailSender();
                    _ = emailSender.SendConfirmationEmail(newUser.Email, confirmationCode, newUser.UserName);

                    var userResponse = CreateUserObject(newUser);
                    userResponse.RefreshToken = refreshToken;  // return refresh token
                    userResponse.RefreshTokenExpiryTime = newUser.RefreshTokenExpiryTime;

                    return userResponse;
                }
                else
                {
                    return BadRequest(result.Errors);
                }
            }
        }





        [AllowAnonymous]
        [HttpPost("sendCode/{email}")]
        public async Task<ActionResult<UserResponseDto>> SendCode(string email)
        {
            Console.WriteLine("email: ", email);
            var normalizedEmail = _userManager.NormalizeEmail(email);
            Console.WriteLine("normalizedEmail", normalizedEmail);
            var existingConfirmedUser = await _userManager.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail && u.Confirmed);
            Console.WriteLine("existingUser", existingConfirmedUser);

            if (existingConfirmedUser != null)
            {
                CodeGenerator codeGenerator = new CodeGenerator();
                string confirmationCode = codeGenerator.GenerateCode();
                existingConfirmedUser.ConfirmationCode = confirmationCode;
                var updateResult = await _userManager.UpdateAsync(existingConfirmedUser);

                if (updateResult.Succeeded)
                {
                    EmailSender emailSender = new EmailSender();
                    var emailResult = emailSender.SendConfirmationEmail(normalizedEmail, confirmationCode, existingConfirmedUser.UserName ?? string.Empty);
                    return Ok();
                }
            }
            return BadRequest();
        }



        [AllowAnonymous]
        [HttpPost("resetPassword")]
        public async Task<ActionResult<UserResponseDto>> ResetPassword(UpdatePasswordDto updatePasswordDto)
        {

            CodeGenerator codeGenerator = new CodeGenerator();
            string confirmationCode = codeGenerator.GenerateCode();

            var normalizedEmail = _userManager.NormalizeEmail(updatePasswordDto.Email);

            var existingUser = await _userManager.Users.FirstOrDefaultAsync(u =>
                u.NormalizedEmail == normalizedEmail &&
                u.ConfirmationCode == updatePasswordDto.ConfirmationCode &&
                u.Confirmed);

            if (existingUser == null)
            {
                ModelState.AddModelError("email", "Invalid email or confirmation code");
                return ValidationProblem();
            }


            var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
            var result = await _userManager.ResetPasswordAsync(existingUser, token, updatePasswordDto.Password);

            if (result.Succeeded)
            {
                existingUser.ConfirmationCode = null;

                // Generate and save new refresh token
                var refreshToken = _tokenService.GenerateRefreshToken();
                existingUser.RefreshToken = refreshToken;
                existingUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                await _userManager.UpdateAsync(existingUser);

                var userResponse = new UserResponseDto
                {
                    Email = existingUser.Email ?? string.Empty,
                    UserName = existingUser.UserName ?? string.Empty,
                    UserId = existingUser.Id,
                    ProfileImageUrl = existingUser.ProfileImageUrl ?? string.Empty,
                    Token = _tokenService.CreateToken(existingUser),
                    RefreshToken = refreshToken, // include refresh token here
                    RefreshTokenExpiryTime = existingUser.RefreshTokenExpiryTime
                };

                return Ok(userResponse);
            }
            else
            {
                return BadRequest("Password reset failed");
            }


        }

        [AllowAnonymous]
        [HttpPost("confirmCode")]
        public async Task<ActionResult<UserResponseDto>> ConfirmCode(ConfirmDto confirmDto)
        {
            var normalizedEmail = _userManager.NormalizeEmail(confirmDto.Email);
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            if (user.ConfirmationCode != confirmDto.Code)
            {
                return BadRequest("Invalid confirmation code");
            }

            user.Confirmed = true;
            user.ConfirmationCode = null;

            // Generate and assign refresh token
            var refreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _userManager.UpdateAsync(user);

            return new UserResponseDto
            {
                Token = _tokenService.CreateToken(user),
                RefreshToken = refreshToken, // include refresh token here
                RefreshTokenExpiryTime = user.RefreshTokenExpiryTime,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                UserId = user.Id,
                ProfileImageUrl = user.ProfileImageUrl ?? string.Empty,
            };

        }

        // [Authorize]
        [AllowAnonymous]  // remove later !!!!!!!!

        [HttpGet("getCurrentUser")]
        public async Task<ActionResult<UserResponseDto>> GetCurrentUser()
        {

            var normalizedEmail = User.FindFirstValue(ClaimTypes.Email)?.ToUpper();
            var user = await _userManager.Users
                .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail);
            if (user != null)
            {
                return CreateUserObject(user);
            }
            else
            {
                return BadRequest("User not found");
            }
        }

        // [AllowAnonymous]
        [HttpPost("google-login")]
        public async Task<ActionResult<UserResponseDto>> GoogleLogin([FromBody] GoogleLoginDto googleLoginDto)
        {
            try
            {
                // Validate Google ID token

                // var payload = await GoogleJsonWebSignature.ValidateAsync(googleLoginDto.AccessToken);

                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"https://www.googleapis.com/oauth2/v1/tokeninfo?access_token={googleLoginDto.AccessToken}");

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Invalid access token");
                }
                var a = 0;
                var json = await response.Content.ReadAsStringAsync();
                var tokenInfo = JsonSerializer.Deserialize<GoogleTokenInfo>(json);
                var b = 0;

                if (!tokenInfo.VerifiedEmail)
                {
                    return BadRequest("Email not verified by Google.");
                }
                if (tokenInfo.Audience != _googleClientId)
                {
                    return BadRequest("Token was not issued for this app.");
                }

                // Check if user already exists by normalized email
                var normalizedEmail = _userManager.NormalizeEmail(tokenInfo.Email);
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

                if (user == null)
                {
                    // New user - create it
                    //string[] images = { "parrot-looks.jpg", "parrot-looks2.jpg", "parrot-looks3.jpg", "parrot-looks4.jpg", "parrot-looks5.jpg" };

                    string baseUrl = "https://parrotsstorage.blob.core.windows.net/parrotsuploads/";
                    string[] images =
                    {
                        $"{baseUrl}parrot-looks.jpg",
                        $"{baseUrl}parrot-looks2.jpg",
                        $"{baseUrl}parrot-looks3.jpg",
                        $"{baseUrl}parrot-looks4.jpg",
                        $"{baseUrl}parrot-looks5.jpg",

                    };

                    Random random = new Random();
                    int randomIndex = random.Next(0, images.Length);
                    string selectedImage = images[randomIndex];
                    user = new AppUser
                    {
                        Email = tokenInfo.Email,
                        DisplayEmail = tokenInfo.Email,
                        UserName = tokenInfo.Email.Split('@')[0],
                        EmailConfirmed = true,
                        Confirmed = true,
                        NormalizedEmail = normalizedEmail,
                        NormalizedUserName = _userManager.NormalizeName(tokenInfo.Email),
                        ProfileImageUrl = selectedImage,
                        BackgroundImageUrl = "https://parrotsstorage.blob.core.windows.net/parrotsuploads/amazon.jpeg",
                        EmailVisible = true,
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        return BadRequest("Failed to create user from Google login");
                    }
                }
                // Generate refresh token and assign it
                var refreshToken = _tokenService.GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                await _userManager.UpdateAsync(user);

                // Return JWT token + refresh token
                return new UserResponseDto
                {

                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    UserId = user.Id,
                    ProfileImageUrl = user.ProfileImageUrl ?? string.Empty,
                    Token = _tokenService.CreateToken(user),
                    RefreshToken = refreshToken,
                    RefreshTokenExpiryTime = user.RefreshTokenExpiryTime

                };
            }
            catch (Exception ex)
            {
                return BadRequest($"Invalid Google token: {ex.Message}");
            }
        }


        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<UserResponseDto>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshTokenDto.RefreshToken);
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return StatusCode(401, new { error = "Invalid or expired refresh token" });

            }
            var newAccessToken = _tokenService.CreateToken(user);
            var userResponse = CreateUserObject(user);
            userResponse.Token = newAccessToken;

            return userResponse;
        }



        private UserResponseDto CreateUserObject(AppUser user)
        {
            return new UserResponseDto
            {
                Token = _tokenService.CreateToken(user),
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                UserId = user.Id,
                ProfileImageUrl = user.ProfileImageUrl ?? string.Empty,
                RefreshToken = user.RefreshToken ?? string.Empty

            };
        }


    }
}


