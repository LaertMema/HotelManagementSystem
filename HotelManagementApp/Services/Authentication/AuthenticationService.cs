using HotelManagementApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace HotelManagementApp.Services.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApplicationUser> LoginAsync(string username, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(username, password, false, false);
            if (!result.Succeeded)
            {
                return null;
            }
            //No null check required signInManager will throw exception if user is not found
            var user = await _userManager.FindByNameAsync(username);

            // Update LastLogin time
            if (user != null)
            {
                user.LastLogin = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            return user;
        }

        public async Task<bool> LogoutAsync()
        {
            await _signInManager.SignOutAsync();
            return true;
        }

        public async Task<ApplicationUser> GetCurrentUserAsync()
        {
            try
            {
                // Make sure HttpContext exists
                if (_httpContextAccessor.HttpContext == null)
                {
                    return null;
                }

                // Try to get the user directly from the claims
                var user = _httpContextAccessor.HttpContext.User;
                if (user == null || !user.Identity.IsAuthenticated)
                {
                    return null;
                }

                // Look for the name identifier claim which contains the user ID
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                                  user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                                  user.FindFirst(JwtRegisteredClaimNames.NameId)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return null;
                }

                // Use the ID to find the user
                var appUser = await _userManager.FindByIdAsync(userIdClaim);
                return appUser;
            }
            catch (Exception ex)
            {
                // Log the exception - critical for debugging issues like this
                Console.WriteLine($"Error in GetCurrentUserAsync: {ex.Message}");
                return null;
            }
        }


        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            return result.Succeeded;
        }

        public async Task<bool> ResetPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return false;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // For a real application, you would email the token to the user
            // For testing purposes, we're resetting to a default password
            var result = await _userManager.ResetPasswordAsync(user, token, "Password@123");

            if (result.Succeeded)
            {
                user.PasswordResetRequired = true;
                await _userManager.UpdateAsync(user);
            }

            return result.Succeeded;
        }

        public async Task<bool> RegisterUserAsync(ApplicationUser user, string password)
        {
            // Set default values for new users
            user.IsActive = true;
            user.AccountStatus = Models.Enums.AccountStatus.Active;
            user.RegistrationDate = DateTime.UtcNow;
            user.Created = DateTime.UtcNow;
            user.PasswordResetRequired = false;

            // Create the user with password
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return false;
            }

            // Assign default role (Guest) to new users
            const string defaultRole = "Guest";

            // Check if role exists, create it if it doesn't
            var roleExists = await _roleManager.RoleExistsAsync(defaultRole);
            if (!roleExists)
            {
                // Create the Guest role if it doesn't exist
                await _roleManager.CreateAsync(new ApplicationRole
                {
                    Name = defaultRole,
                    NormalizedName = defaultRole.ToUpper(),
                    Description = "Hotel guest with booking capabilities"
                });
            }

            // Assign the role to the user
            await _userManager.AddToRoleAsync(user, defaultRole);

            return true;
        }

        public async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            // Get user roles for claims
            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
    {
        // Use the user's ID for Sub and NameId claims
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),

        // Add other claims
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.Name, user.UserName)
    };

            // Add role claims
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Set token expiration (1 hour)
            var expiration = DateTime.UtcNow.AddHours(1);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public async Task<bool> ValidateTokenAsync(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // Set to zero for exact expiration time
                }, out SecurityToken validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

