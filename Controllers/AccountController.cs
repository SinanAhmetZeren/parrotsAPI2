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


namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly TokenService _tokenService;

        public AccountController(UserManager<AppUser> userManager, TokenService tokenService)
        {
            _tokenService = tokenService;
            _userManager = userManager;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<UserResponseDto>> Login(LoginDto loginDto)
        {
            var normalizedEmail = _userManager.NormalizeEmail(loginDto.Email);
            var user = await _userManager.Users
                .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail);

            if (user == null || !user.Confirmed)
            {
                return Unauthorized("User not found or not confirmed");
            }
            var result = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (result)
            {
                return CreateUserObject(user);
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

                    else if (existingUser.NormalizedEmail == normalizedEmail )
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

                    string[] images = { "parrot-looks.jpg", "parrot-looks2.jpg", "parrot-looks3.jpg", "parrot-looks4.jpg", "parrot-looks5.jpg" };
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
                    
                    var updateResult = await _userManager.UpdateAsync(existingUser);
                    if (!updateResult.Succeeded)
                    {
                        return StatusCode(500); 
                    }   

                    EmailSender emailSender = new EmailSender();
                    _ = emailSender.SendConfirmationEmail(existingUser.Email ?? string.Empty, existingUser.ConfirmationCode, existingUser.UserName);

                    return CreateUserObject(existingUser);
                    }
                }

            else
            {
                // USER DOES NOT EXIST

                string[] images = { "parrot-looks.jpg", "parrot-looks2.jpg", "parrot-looks3.jpg", "parrot-looks4.jpg", "parrot-looks5.jpg" };
                Random random = new Random();
                int randomIndex = random.Next(0, images.Length);
                string selectedImage = images[randomIndex];

                var newUser = new AppUser
                {
                    Email = registerDto.Email,
                    UserName = registerDto.UserName,
                    ProfileImageUrl = selectedImage,
                    ConfirmationCode = confirmationCode,
                    BackgroundImageUrl = "amazon.jpeg",
                    };

                newUser.EmailConfirmed = true;
                newUser.NormalizedEmail = _userManager.NormalizeEmail(registerDto.Email); 
                newUser.NormalizedUserName = _userManager.NormalizeName (registerDto.UserName);

                var result = await _userManager.CreateAsync(newUser, registerDto.Password);
                if (result.Succeeded)
                {
                    EmailSender emailSender = new EmailSender();
                    _ = emailSender.SendConfirmationEmail(newUser.Email, confirmationCode, newUser.UserName);
                    return CreateUserObject(newUser);
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
            Console.WriteLine("email: ",email);
            var normalizedEmail = _userManager.NormalizeEmail(email);
            Console.WriteLine("normalizedEmail",normalizedEmail);
            var existingConfirmedUser = await _userManager.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail && u.Confirmed);
            Console.WriteLine("existingUser",existingConfirmedUser);
            
            if (existingConfirmedUser != null)
            {
                CodeGenerator codeGenerator = new CodeGenerator();
                string confirmationCode = codeGenerator.GenerateCode();
                existingConfirmedUser.ConfirmationCode = confirmationCode;
                var updateResult = await _userManager.UpdateAsync(existingConfirmedUser);

                if (updateResult.Succeeded)
                {
                    EmailSender emailSender = new EmailSender();
                    _ = emailSender.SendConfirmationEmail(normalizedEmail, confirmationCode, existingConfirmedUser.UserName ?? string.Empty);
                    return Ok();

                }
                Console.WriteLine("hello");
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
                await _userManager.UpdateAsync(existingUser);
                var userResponse = new UserResponseDto
                {
                    Email = existingUser.Email ?? string.Empty,
                    UserName = existingUser.UserName ?? string.Empty,
                    UserId = existingUser.Id,
                    ProfileImageUrl = existingUser.ProfileImageUrl ?? string.Empty,
                    Token = _tokenService.CreateToken(existingUser) 
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
            await _userManager.UpdateAsync(user);

            return new UserResponseDto
            {
                Token = _tokenService.CreateToken(user),
                UserName = user.UserName  ?? string.Empty,
                Email = user.Email  ?? string.Empty,
                UserId = user.Id,
                ProfileImageUrl = user.ProfileImageUrl  ?? string.Empty,
            };

        }

        [Authorize]
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

        [AllowAnonymous]
        [HttpPost("google-login")]
        public async Task<ActionResult<UserResponseDto>> GoogleLogin([FromBody] GoogleLoginDto googleLoginDto)
        {
            try
            {
                // Validate Google ID token
                var payload = await GoogleJsonWebSignature.ValidateAsync(googleLoginDto.IdToken);

                // Check if user already exists by normalized email
                var normalizedEmail = _userManager.NormalizeEmail(payload.Email);
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

                if (user == null)
                {
                    // New user - create it

                    string[] images = { "parrot-looks.jpg", "parrot-looks2.jpg", "parrot-looks3.jpg", "parrot-looks4.jpg", "parrot-looks5.jpg" };
                    Random random = new Random();
                    int randomIndex = random.Next(0, images.Length);
                    string selectedImage = images[randomIndex];
                    user = new AppUser
                    {
                        Email = payload.Email,
                        UserName = payload.Email,
                        EmailConfirmed = true,
                        Confirmed = true,
                        NormalizedEmail = normalizedEmail,
                        NormalizedUserName = _userManager.NormalizeName(payload.Email),
                        ProfileImageUrl = selectedImage,
                        BackgroundImageUrl = "amazon.jpeg",
                        EmailVisible = true,
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        return BadRequest("Failed to create user from Google login");
                    }
                }

                // Generate your JWT token for this user
                return CreateUserObject(user);
            }
            catch (Exception ex)
            {
                return BadRequest($"Invalid Google token: {ex.Message}");
            }
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
            };
        }


    }
}


