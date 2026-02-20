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
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ParrotsAPI2.Services.EmailSender;
using Microsoft.Extensions.Caching.Memory;



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
        private readonly string _googleAndroidClientId;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailSender _emailSender;

        public AccountController(
            UserManager<AppUser> userManager,
            TokenService tokenService,
            IOptions<GoogleAuthOptions> googleOptions,
            ILogger<AccountController> logger,
            IEmailSender emailSender
        )
        {
            _tokenService = tokenService;
            _userManager = userManager;
            _googleClientId = googleOptions.Value.ClientId;
            _googleAndroidClientId = googleOptions.Value.AndroidClientId;
            _logger = logger;
            _emailSender = emailSender;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [RateLimit(5, 60, true)]
        public async Task<ActionResult<UserResponseDto>> Login(LoginDto loginDto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var deviceId = Request.Headers["X-Device-Id"].ToString();

            var normalizedEmail = _userManager.NormalizeEmail(loginDto.Email?.Trim());

            AppUser? user = await _userManager.Users
                .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail);

            // ❌ User not found or email not confirmed
            if (user == null || !user.Confirmed)
            {
                _logger.LogWarning(
                    "Login failed. Email not found or not confirmed. Email: {Email}, IP: {IP}, DeviceId: {DeviceId}",
                    loginDto.Email,
                    ip,
                    deviceId
                );

                return Unauthorized("Invalid credentials");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);

            // ❌ Wrong password
            if (!passwordValid)
            {
                _logger.LogWarning(
                    "Login failed. Invalid password. UserId: {UserId}, IP: {IP}, DeviceId: {DeviceId}",
                    user.Id,
                    ip,
                    deviceId
                );

                return Unauthorized("Invalid credentials");
            }

            // ✅ Successful login
            _logger.LogInformation(
                "Login success. UserId: {UserId}, IP: {IP}, DeviceId: {DeviceId}",
                user.Id,
                ip,
                deviceId
            );

            // 🔄 Refresh token handling
            var refreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            var updateResult = await _userManager.UpdateAsync(user);

            // 🚨 Token persistence failure (rare but critical)
            if (!updateResult.Succeeded)
            {
                _logger.LogError(
                    "Refresh token update failed. UserId: {UserId}, IP: {IP}",
                    user.Id,
                    ip
                );

                return StatusCode(500, "Login failed");
            }

            var userResponse = CreateUserObject(user);
            userResponse.RefreshToken = refreshToken;
            userResponse.RefreshTokenExpiryTime = user.RefreshTokenExpiryTime;

            return userResponse;
        }


        [AllowAnonymous]
        [HttpPost("register")]
        [RateLimit(5, 60, true)]
        public async Task<ActionResult<UserResponseDto>> Register(RegisterDto registerDto)
        {
            // 1️⃣ Basic validation
            if (string.IsNullOrWhiteSpace(registerDto.Email) ||
                string.IsNullOrWhiteSpace(registerDto.UserName) ||
                string.IsNullOrWhiteSpace(registerDto.Password))
            {
                _logger.LogWarning(
                    "Register failed: Missing fields. IP: {IP}",
                    HttpContext.Connection.RemoteIpAddress
                );

                return BadRequest("Missing required fields.");
            }

            // 2️⃣ Email blacklist (FAIL FAST)
            if (EmailBlacklister.IsBlacklisted(registerDto.Email))
            {
                _logger.LogWarning(
                    "Blocked register attempt (blacklisted email). Email: {Email}, IP: {IP}",
                    registerDto.Email,
                    HttpContext.Connection.RemoteIpAddress
                );

                return BadRequest("This email domain is not allowed.");
            }

            var normalizedEmail = _userManager.NormalizeEmail(registerDto.Email.Trim());
            var normalizedUserName = _userManager.NormalizeName(registerDto.UserName.Trim());

            var existingUser = await _userManager.Users
                .FirstOrDefaultAsync(u =>
                    u.NormalizedEmail == normalizedEmail ||
                    u.NormalizedUserName == normalizedUserName
                );

            string baseUrl = "https://parrotsstorage.blob.core.windows.net/parrotsuploads/";
            string[] images =
            {
                    $"{baseUrl}parrot-looks.jpg",
                    $"{baseUrl}parrot-looks2.jpg",
                    $"{baseUrl}parrot-looks3.jpg",
                    $"{baseUrl}parrot-looks4.jpg",
                    $"{baseUrl}parrot-looks5.jpg",
                };

            string selectedImage = images[Random.Shared.Next(images.Length)];

            // 🔁 EXISTING USER
            if (existingUser != null)
            {
                // 🚫 Confirmed account → block
                if (existingUser.Confirmed)
                {
                    _logger.LogWarning(
                        "Register attempt for existing confirmed user. Email: {Email}, Username: {Username}, IP: {IP}",
                        registerDto.Email,
                        registerDto.UserName,
                        HttpContext.Connection.RemoteIpAddress
                    );

                    if (existingUser.NormalizedEmail == normalizedEmail &&
                        existingUser.NormalizedUserName == normalizedUserName)
                        ModelState.AddModelError("Email and Username", "Email and Username are already taken");
                    else if (existingUser.NormalizedEmail == normalizedEmail)
                        ModelState.AddModelError("Email", "Email is already taken");
                    else
                        ModelState.AddModelError("Username", "Username is already taken");

                    return ValidationProblem();
                }

                // 🔄 Existing but NOT confirmed → re-register
                _logger.LogInformation(
                    "Re-registering unconfirmed user. UserId: {UserId}, Email: {Email}, IP: {IP}",
                    existingUser.Id,
                    existingUser.Email,
                    HttpContext.Connection.RemoteIpAddress
                );

                string confirmationCode = new CodeGenerator().GenerateCode();

                existingUser.UserName = registerDto.UserName.Trim();
                existingUser.ProfileImageUrl = selectedImage;
                existingUser.ConfirmationCode = confirmationCode;
                existingUser.NormalizedUserName = normalizedUserName;
                existingUser.NormalizedEmail = normalizedEmail;

                var passwordHasher = new PasswordHasher<AppUser>();
                existingUser.PasswordHash =
                    passwordHasher.HashPassword(existingUser, registerDto.Password);

                existingUser.RefreshToken = _tokenService.GenerateRefreshToken();
                existingUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                existingUser.EncryptionKey = GenerateBase64Key();
                existingUser.PublicId = GeneratePublicId();

                var updateResult = await _userManager.UpdateAsync(existingUser);
                if (!updateResult.Succeeded)
                {
                    _logger.LogError(
                        "User update failed during re-register. UserId: {UserId}, Errors: {Errors}",
                        existingUser.Id,
                        string.Join(", ", updateResult.Errors.Select(e => e.Description))
                    );

                    return StatusCode(500);
                }

                SendConfirmationEmailSafe(existingUser);


                var userResponse = CreateUserObject(existingUser);
                userResponse.RefreshToken = existingUser.RefreshToken;
                userResponse.RefreshTokenExpiryTime = existingUser.RefreshTokenExpiryTime;

                _logger.LogInformation(
                    "User re-registered successfully. Email: {Email}, IP: {IP}",
                    existingUser.Email,
                    HttpContext.Connection.RemoteIpAddress
                );

                return userResponse;
            }

            // 🆕 NEW USER
            string newConfirmationCode = new CodeGenerator().GenerateCode();

            var newUser = new AppUser
            {
                Email = registerDto.Email.Trim(),
                UserName = registerDto.UserName.Trim(),
                DisplayEmail = registerDto.Email.Trim(),
                ProfileImageUrl = selectedImage,
                BackgroundImageUrl = $"{baseUrl}amazon.jpeg",
                ConfirmationCode = newConfirmationCode,
                EmailConfirmed = false, // ✅ FIXED
                NormalizedEmail = normalizedEmail,
                NormalizedUserName = normalizedUserName,
                EncryptionKey = GenerateBase64Key(),
                PublicId = GeneratePublicId(),
                Title = "Wanderer",
                Bio = "Exploring new journeys."
            };

            var createResult = await _userManager.CreateAsync(newUser, registerDto.Password);
            if (!createResult.Succeeded)
            {
                _logger.LogError(
                    "User creation failed. Email: {Email}, Errors: {Errors}",
                    newUser.Email,
                    string.Join(", ", createResult.Errors.Select(e => e.Description))
                );

                return BadRequest(createResult.Errors);
            }

            newUser.RefreshToken = _tokenService.GenerateRefreshToken();
            newUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(newUser);

            SendConfirmationEmailSafe(newUser);

            var response = CreateUserObject(newUser);
            response.RefreshToken = newUser.RefreshToken;
            response.RefreshTokenExpiryTime = newUser.RefreshTokenExpiryTime;

            _logger.LogInformation(
                "New user registered successfully. Email: {Email}, IP: {IP}",
                newUser.Email,
                HttpContext.Connection.RemoteIpAddress
            );

            return response;
        }



        [AllowAnonymous]
        [HttpPost("sendCode/{email}")]
        [RateLimit(5, 60, true)]
        public async Task<IActionResult> SendCode(string email)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("SendCode failed: empty email. IP={IP}", ip);
                return BadRequest("Email is required.");
            }
            var normalizedEmail = _userManager.NormalizeEmail(email.Trim());
            _logger.LogInformation(
                "SendCode request received. Email={Email}, IP={IP}",
                email,
                ip
            );

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail && u.Confirmed);

            if (user == null)
            {
                _logger.LogWarning(
                    "SendCode failed: no confirmed user. Email={Email}, IP={IP}",
                    normalizedEmail,
                    ip
                );
                return BadRequest("No confirmed user found with this email.");
            }

            var confirmationCode = new CodeGenerator().GenerateCode();
            user.ConfirmationCode = confirmationCode;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError(
                    "SendCode failed updating user {UserId}",
                    user.Id
                );
                return StatusCode(500, "Failed to generate code.");
            }

            // capture ONLY primitives / strings
            var userEmail = user.Email!;
            var userName = user.UserName ?? string.Empty;
            var userId = user.Id;

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailSender.SendConfirmationEmail(
                        userEmail,
                        confirmationCode,
                        userName
                    );

                    _logger.LogInformation(
                        "SendCode email sent. UserId={UserId}, IP={IP}",
                        userId,
                        ip
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "SendCode email failed. UserId={UserId}, IP={IP}",
                        userId,
                        ip
                    );
                }
            });

            // return Ok("Confirmation code sent.");
            return Ok(new { message = "Confirmation code sent." });

        }


        [AllowAnonymous]
        [HttpPost("resetPassword")]
        [RateLimit(5, 60, true)]
        public async Task<ActionResult<UserResponseDto>> ResetPassword(UpdatePasswordDto updatePasswordDto)
        {
            // Log the attempt
            _logger.LogInformation(
                "ResetPassword attempt. Email: {Email}, IP: {IP}",
                updatePasswordDto.Email,
                HttpContext.Connection.RemoteIpAddress
            );

            var normalizedEmail = _userManager.NormalizeEmail(updatePasswordDto.Email?.Trim());

            var existingUser = await _userManager.Users.FirstOrDefaultAsync(u =>
                u.NormalizedEmail == normalizedEmail &&
                u.ConfirmationCode == updatePasswordDto.ConfirmationCode &&
                u.Confirmed);

            if (existingUser == null)
            {
                _logger.LogWarning(
                    "ResetPassword failed: invalid email or confirmation code. Email: {Email}, IP: {IP}",
                    normalizedEmail,
                    HttpContext.Connection.RemoteIpAddress
                );

                ModelState.AddModelError("email", "Invalid email or confirmation code");
                return ValidationProblem();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
            var result = await _userManager.ResetPasswordAsync(existingUser, token, updatePasswordDto.Password);

            if (!result.Succeeded)
            {
                _logger.LogError(
                    "ResetPassword failed for user {UserId}. Errors: {Errors}, IP: {IP}",
                    existingUser.Id,
                    string.Join(", ", result.Errors.Select(e => e.Description)),
                    HttpContext.Connection.RemoteIpAddress
                );
                return BadRequest("Password reset failed");
            }

            // Password reset succeeded
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
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = existingUser.RefreshTokenExpiryTime
            };

            _logger.LogInformation(
                "ResetPassword succeeded for user {UserId}. Email: {Email}, IP: {IP}",
                existingUser.Id,
                existingUser.Email,
                HttpContext.Connection.RemoteIpAddress
            );

            return Ok(userResponse);
        }


        [AllowAnonymous]
        [HttpPost("confirmCode")]
        [RateLimit(5, 60, true)]
        public async Task<ActionResult<UserResponseDto>> ConfirmCode(ConfirmDto confirmDto)
        {
            var normalizedEmail = _userManager.NormalizeEmail(confirmDto.Email?.Trim());

            _logger.LogInformation(
                "ConfirmCode attempt. Email: {Email}, IP: {IP}",
                normalizedEmail,
                HttpContext.Connection.RemoteIpAddress
            );

            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail);
            if (user == null)
            {
                _logger.LogWarning(
                    "ConfirmCode failed: user not found. Email: {Email}, IP: {IP}",
                    normalizedEmail,
                    HttpContext.Connection.RemoteIpAddress
                );
                return BadRequest("User not found");
            }

            if (user.ConfirmationCode != confirmDto.Code)
            {
                _logger.LogWarning(
                    "ConfirmCode failed: invalid code for user {UserId}. Email: {Email}, IP: {IP}",
                    user.Id,
                    normalizedEmail,
                    HttpContext.Connection.RemoteIpAddress
                );
                return BadRequest("Invalid confirmation code");
            }

            user.Confirmed = true;
            user.ConfirmationCode = null;

            // Generate and assign refresh token
            var refreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError(
                    "ConfirmCode update failed for user {UserId}. Errors: {Errors}, IP: {IP}",
                    user.Id,
                    string.Join(", ", updateResult.Errors.Select(e => e.Description)),
                    HttpContext.Connection.RemoteIpAddress
                );
                return StatusCode(500, "Failed to confirm code");
            }

            _logger.LogInformation(
                "ConfirmCode succeeded for user {UserId}. Email: {Email}, IP: {IP}",
                user.Id,
                normalizedEmail,
                HttpContext.Connection.RemoteIpAddress
            );

            return new UserResponseDto
            {
                Token = _tokenService.CreateToken(user),
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = user.RefreshTokenExpiryTime,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                UserId = user.Id,
                ProfileImageUrl = user.ProfileImageUrl ?? string.Empty,
            };
        }

        // [AllowAnonymous]
        [HttpPost("google-login")]
        [RateLimit(5, 60, true)]
        public async Task<ActionResult<UserResponseDto>> GoogleLogin([FromBody] GoogleLoginDto googleLoginDto)
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"https://www.googleapis.com/oauth2/v1/tokeninfo?access_token={googleLoginDto.AccessToken}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GoogleLogin failed: invalid access token. IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                    return BadRequest("Invalid access token");
                }

                var json = await response.Content.ReadAsStringAsync();
                var tokenInfo = JsonSerializer.Deserialize<GoogleTokenInfo>(json);

                if (!tokenInfo.VerifiedEmail)
                {
                    _logger.LogWarning("GoogleLogin failed: email not verified. Email: {Email}, IP: {IP}", tokenInfo.Email, HttpContext.Connection.RemoteIpAddress);
                    return BadRequest("Email not verified by Google.");
                }

                if (tokenInfo.Audience != _googleClientId && tokenInfo.Audience != _googleAndroidClientId)
                {
                    _logger.LogWarning("GoogleLogin failed: token audience mismatch. Email: {Email}, IP: {IP}", tokenInfo.Email, HttpContext.Connection.RemoteIpAddress);
                    return BadRequest("Token was not issued for this app.");
                }

                var normalizedEmail = _userManager.NormalizeEmail(tokenInfo.Email.Trim());
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

                if (user == null)
                {
                    string baseUrl = "https://parrotsstorage.blob.core.windows.net/parrotsuploads/";
                    string[] images =
                    {
                $"{baseUrl}parrot-looks.jpg",
                $"{baseUrl}parrot-looks2.jpg",
                $"{baseUrl}parrot-looks3.jpg",
                $"{baseUrl}parrot-looks4.jpg",
                $"{baseUrl}parrot-looks5.jpg",
            };

                    string selectedImage = images[Random.Shared.Next(images.Length)];

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
                        BackgroundImageUrl = $"{baseUrl}amazon.jpeg",
                        EmailVisible = true,
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        _logger.LogError(
                            "GoogleLogin failed: could not create user. Email: {Email}, Errors: {Errors}, IP: {IP}",
                            tokenInfo.Email,
                            string.Join(", ", createResult.Errors.Select(e => e.Description)),
                            HttpContext.Connection.RemoteIpAddress
                        );
                        return BadRequest("Failed to create user from Google login");
                    }
                }

                // Generate refresh token
                var refreshToken = _tokenService.GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                await _userManager.UpdateAsync(user);

                _logger.LogInformation("GoogleLogin succeeded. UserId: {UserId}, Email: {Email}, IP: {IP}", user.Id, user.Email, HttpContext.Connection.RemoteIpAddress);

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
                _logger.LogError(ex, "GoogleLogin exception. IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                return BadRequest($"Invalid Google token: {ex.Message}");
            }
        }


        [AllowAnonymous]
        [HttpPost("refresh-token")]
        [RateLimit(5, 60, true)]
        public async Task<ActionResult<UserResponseDto>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            _logger.LogInformation(
                "RefreshToken attempt. Token: {Token}, IP: {IP}",
                refreshTokenDto.RefreshToken,
                HttpContext.Connection.RemoteIpAddress
            );
            var token = refreshTokenDto.RefreshToken?.Trim();
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == token);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "RefreshToken failed: invalid or expired token. Token: {Token}, IP: {IP}",
                    token,
                    HttpContext.Connection.RemoteIpAddress
                );
                return StatusCode(401, new { error = "Invalid or expired refresh token" });
            }

            var newAccessToken = _tokenService.CreateToken(user);
            var userResponse = CreateUserObject(user);
            userResponse.Token = newAccessToken;

            _logger.LogInformation(
                "RefreshToken succeeded. UserId: {UserId}, IP: {IP}",
                user.Id,
                HttpContext.Connection.RemoteIpAddress
            );

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
                RefreshToken = user.RefreshToken ?? string.Empty,
                UnreadMessages = user.UnseenMessages ? "true" : "false"

            };
        }

        private string GenerateBase64Key(int byteLength = 32) // 32 bytes = 256-bit key
        {
            byte[] key = new byte[byteLength];
            RandomNumberGenerator.Fill(key); // cryptographically secure
            return Convert.ToBase64String(key);
        }

        private static string GeneratePublicId(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            char[] id = new char[length];
            byte[] randomBytes = new byte[length];

            RandomNumberGenerator.Fill(randomBytes); // cryptographically secure

            for (int i = 0; i < length; i++)
            {
                id[i] = chars[randomBytes[i] % chars.Length];
            }

            return new string(id);
        }

        private void SendConfirmationEmailSafe(AppUser user) // HELPER FOR REGISTER ENDPOINT
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailSender.SendConfirmationEmail(
                        user.Email!,
                        user.ConfirmationCode!,
                        user.UserName!
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Async confirmation email failed. UserId={UserId}, Email={Email}",
                        user.Id,
                        user.Email
                    );
                }
            });
        }

    }
}

/*
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

*/


/*

 [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<UserResponseDto>> Register(RegisterDto registerDto)
        {


            // 1️⃣ Basic validation
            if (string.IsNullOrWhiteSpace(registerDto.Email) ||
                string.IsNullOrWhiteSpace(registerDto.UserName) ||
                string.IsNullOrWhiteSpace(registerDto.Password))
            {
                return BadRequest("Missing required fields.");
            }

            // 2️⃣ Email blacklist (FAIL FAST)
            if (EmailBlacklister.IsBlacklisted(registerDto.Email))
            {
                _logger.LogWarning("Blocked register attempt: {Email}", registerDto.Email);

                return BadRequest("This email domain is not allowed.");
            }

            CodeGenerator codeGenerator = new CodeGenerator();
            var normalizedEmail = _userManager.NormalizeEmail(registerDto.Email?.Trim());
            var normalizedUserName = _userManager.NormalizeName(registerDto.UserName?.Trim());
            string confirmationCode = codeGenerator.GenerateCode();

            var existingUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail || u.NormalizedUserName == normalizedUserName);

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
            string selectedImage = images[random.Next(0, images.Length)];

            if (existingUser != null)
            {
                if (existingUser.Confirmed)
                {
                    if (existingUser.NormalizedEmail == normalizedEmail &&
                        existingUser.NormalizedUserName == normalizedUserName)
                        ModelState.AddModelError("Email and Username", "Email and Username are already taken");
                    else if (existingUser.NormalizedEmail == normalizedEmail)
                        ModelState.AddModelError("Email", "Email is already taken");
                    else if (existingUser.NormalizedUserName == normalizedUserName)
                        ModelState.AddModelError("Username", "Username is already taken");

                    return ValidationProblem();
                }

                // Existing user not confirmed — update
                existingUser.UserName = registerDto.UserName;
                existingUser.ProfileImageUrl = selectedImage;
                existingUser.ConfirmationCode = confirmationCode;
                existingUser.NormalizedUserName = normalizedUserName;
                existingUser.NormalizedEmail = normalizedEmail;

                var passwordHasher = new PasswordHasher<AppUser>();
                existingUser.PasswordHash = passwordHasher.HashPassword(existingUser, registerDto.Password);
                existingUser.RefreshToken = _tokenService.GenerateRefreshToken();
                existingUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                existingUser.EncryptionKey = GenerateBase64Key();
                existingUser.PublicId = GeneratePublicId();

                var updateResult = await _userManager.UpdateAsync(existingUser);
                if (!updateResult.Succeeded)
                    return StatusCode(500);

                if (IsValidEmail(existingUser.Email))
                {
                    _ = Task.Run(() =>
                        new EmailSender().SendConfirmationEmail(existingUser.Email!, existingUser.ConfirmationCode, existingUser.UserName)
                    );
                }
                else
                {
                    Console.WriteLine($"Invalid email skipped: {existingUser.Email}");
                }


                var userResponse = CreateUserObject(existingUser);
                userResponse.RefreshToken = existingUser.RefreshToken;
                userResponse.RefreshTokenExpiryTime = existingUser.RefreshTokenExpiryTime;
                return userResponse;
            }
            else
            {
                // New user
                var newUser = new AppUser
                {
                    Email = registerDto.Email?.Trim(),
                    UserName = registerDto.UserName?.Trim(),
                    ProfileImageUrl = selectedImage,
                    ConfirmationCode = confirmationCode,
                    BackgroundImageUrl = $"{baseUrl}amazon.jpeg",
                    DisplayEmail = registerDto.Email?.Trim(),
                    EncryptionKey = GenerateBase64Key(),
                    PublicId = GeneratePublicId(),
                    EmailConfirmed = true,
                    NormalizedEmail = normalizedEmail,
                    NormalizedUserName = normalizedUserName,
                    Title = "Wanderer",
                    Bio = "Exploring new journeys."
                };

                var result = await _userManager.CreateAsync(newUser, registerDto.Password);
                if (!result.Succeeded) return BadRequest(result.Errors);

                newUser.RefreshToken = _tokenService.GenerateRefreshToken();
                newUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _userManager.UpdateAsync(newUser);

                // Fire-and-forget email
                if (IsValidEmail(newUser.Email))
                {
                    _ = Task.Run(() =>
                        new EmailSender().SendConfirmationEmail(newUser.Email!, newUser.ConfirmationCode, newUser.UserName)
                    );
                }

                var userResponse = CreateUserObject(newUser);
                userResponse.RefreshToken = newUser.RefreshToken;
                userResponse.RefreshTokenExpiryTime = newUser.RefreshTokenExpiryTime;
                return userResponse;
            }
        }

        */


/*


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
*/


/*


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
                var json = await response.Content.ReadAsStringAsync();
                var tokenInfo = JsonSerializer.Deserialize<GoogleTokenInfo>(json);

                if (!tokenInfo.VerifiedEmail)
                {
                    return BadRequest("Email not verified by Google.");
                }
                if (tokenInfo.Audience != _googleClientId && tokenInfo.Audience != _googleAndroidClientId)
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

*/



/* / [Authorize]
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
}*/


