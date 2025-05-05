using HotelManagementApp.Models;
using HotelManagementApp.Services.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HotelManagementApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;

        public AuthController(IAuthenticationService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest("Username and password are required");
            }

            var user = await _authService.LoginAsync(model.Username, model.Password);
            if (user == null)
            {
                return Unauthorized("Invalid username or password");
            }

            var token = await _authService.GenerateJwtTokenAsync(user);
            return Ok(new { Token = token });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return Ok(new { Message = "Logged out successfully" });
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user == null)
            {
                return NotFound("User not found or not authenticated");
            }

            return Ok(user);
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            if (model == null || model.UserId <= 0 ||
                string.IsNullOrEmpty(model.CurrentPassword) ||
                string.IsNullOrEmpty(model.NewPassword))
            {
                return BadRequest("Valid user ID, current password, and new password are required");
            }

            var result = await _authService.ChangePasswordAsync(model.UserId, model.CurrentPassword, model.NewPassword);
            if (!result)
            {
                return BadRequest("Failed to change password. Please check your current password.");
            }

            return Ok(new { Message = "Password changed successfully" });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email))
            {
                return BadRequest("Email address is required");
            }

            var result = await _authService.ResetPasswordAsync(model.Email);
            if (!result)
            {
                return BadRequest("Failed to reset password. Please check if the email address is correct.");
            }

            return Ok(new { Message = "Password reset instructions have been sent to your email" });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (model == null ||
                string.IsNullOrEmpty(model.Username) ||
                string.IsNullOrEmpty(model.Email) ||
                string.IsNullOrEmpty(model.Password) ||
                string.IsNullOrEmpty(model.FirstName) ||
                string.IsNullOrEmpty(model.LastName))
            {
                return BadRequest("All fields are required for registration");
            }

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var result = await _authService.RegisterUserAsync(user, model.Password);
            if (!result)
            {
                return BadRequest("Registration failed. The username or email may already be in use.");
            }

            return Ok(new { Message = "Registration successful" });
        }

        [HttpPost("validate-token")]
        public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Token))
            {
                return BadRequest("Token is required");
            }

            var result = await _authService.ValidateTokenAsync(model.Token);
            if (!result)
            {
                return BadRequest("Invalid or expired token");
            }

            return Ok(new { Message = "Token is valid" });
        }
    }

    public class LoginModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class ChangePasswordModel
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }
    }

    public class ResetPasswordModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class RegisterModel
    {
        [Required]
        [MinLength(3)]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [MinLength(2)]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MinLength(2)]
        [MaxLength(50)]
        public string LastName { get; set; }

        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string Password { get; set; }
    }

    public class ValidateTokenModel
    {
        [Required]
        public string Token { get; set; }
    }
}

