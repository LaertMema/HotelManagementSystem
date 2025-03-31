using HotelManagementApp.Models;

namespace HotelManagementApp.Services.Authentication
{
    public interface IAuthenticationService
    {
        Task<ApplicationUser> LoginAsync(string username, string password);
        Task<bool> LogoutAsync();
        Task<ApplicationUser> GetCurrentUserAsync();
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> ResetPasswordAsync(string email);
        Task<bool> RegisterUserAsync(ApplicationUser user, string password);
        Task<string> GenerateJwtTokenAsync(ApplicationUser user);
        Task<bool> ValidateTokenAsync(string token);
    }
}
