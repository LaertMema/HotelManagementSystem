namespace HotelManagementApp.Services.UserManagement
{
    using global::HotelManagementApp.Models;
    using global::HotelManagementApp.Models.DTOs;
    using global::HotelManagementApp.Models.Enums;
    using HotelManagementApp.Models.DTOs.Room;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<UserService> _logger;

        public UserService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ILogger<UserService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region CRUD Operations

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            try
            {
                var users = await _context.Users
                    .AsNoTracking()
                    .ToListAsync();

                var userRoles = await GetUserRolesAsync(users.Select(u => u.Id).ToList());
                return users.ToUserDtos(userRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                throw;
            }
        }

        public async Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string roleName)
        {
            try
            {
                // First find role
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role == null)
                {
                    throw new KeyNotFoundException($"Role '{roleName}' not found");
                }

                // Get users in role
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);

                var userIds = usersInRole.Select(u => u.Id).ToList();
                var userRoles = new Dictionary<int, string>();

                foreach (var userId in userIds)
                {
                    userRoles[userId] = roleName;
                }

                return usersInRole.ToUserDtos(userRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users by role {RoleName}", roleName);
                throw;
            }
        }

        public async Task<UserDto> GetUserByIdAsync(int id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return null;
                }

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.ToRoleString();

                return user.ToUserDto(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID {UserId}", id);
                throw;
            }
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            try
            {
                // Check if username or email already exists
                var existingUser = await _userManager.FindByNameAsync(createUserDto.Username) ??
                                  await _userManager.FindByEmailAsync(createUserDto.Email);

                if (existingUser != null)
                {
                    throw new InvalidOperationException("Username or email already exists");
                }

                // Create the user
                var user = new ApplicationUser
                {
                    UserName = createUserDto.Username,
                    Email = createUserDto.Email,
                    FirstName = createUserDto.FirstName,
                    LastName = createUserDto.LastName,
                    PhoneNumber = createUserDto.PhoneNumber,
                    IsActive = true,
                    AccountStatus = AccountStatus.Active,
                    PasswordResetRequired = false,
                    RegistrationDate = DateTime.UtcNow,
                    Created = DateTime.UtcNow
                };

                // Additional properties if provided
                if (!string.IsNullOrEmpty(createUserDto.Address))
                    user.Address = createUserDto.Address;

                if (!string.IsNullOrEmpty(createUserDto.City))
                    user.City = createUserDto.City;

                if (!string.IsNullOrEmpty(createUserDto.State))
                    user.State = createUserDto.State;

                if (!string.IsNullOrEmpty(createUserDto.Country))
                    user.Country = createUserDto.Country;

                if (!string.IsNullOrEmpty(createUserDto.PostalCode))
                    user.PostalCode = createUserDto.PostalCode;

                // Create the user with password
                var result = await _userManager.CreateAsync(user, createUserDto.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create user: {errors}");
                }

                // Add user to role
                var roleExists = await _roleManager.RoleExistsAsync(createUserDto.Role);
                if (!roleExists)
                {
                    throw new KeyNotFoundException($"Role '{createUserDto.Role}' not found");
                }

                await _userManager.AddToRoleAsync(user, createUserDto.Role);

                return user.ToUserDto(createUserDto.Role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Username}", createUserDto.Username);
                throw;
            }
        }

        public async Task<UserDto> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {id} not found");
                }

                // Update properties if provided
                if (!string.IsNullOrEmpty(updateUserDto.FirstName))
                    user.FirstName = updateUserDto.FirstName;

                if (!string.IsNullOrEmpty(updateUserDto.LastName))
                    user.LastName = updateUserDto.LastName;

                if (!string.IsNullOrEmpty(updateUserDto.Email))
                {
                    // Check if email is already used by another user
                    var userWithEmail = await _userManager.FindByEmailAsync(updateUserDto.Email);
                    if (userWithEmail != null && userWithEmail.Id != id)
                    {
                        throw new InvalidOperationException("Email is already in use by another user");
                    }

                    user.Email = updateUserDto.Email;
                }

                if (!string.IsNullOrEmpty(updateUserDto.PhoneNumber))
                    user.PhoneNumber = updateUserDto.PhoneNumber;

                if (!string.IsNullOrEmpty(updateUserDto.Address))
                    user.Address = updateUserDto.Address;

                if (!string.IsNullOrEmpty(updateUserDto.City))
                    user.City = updateUserDto.City;

                if (!string.IsNullOrEmpty(updateUserDto.State))
                    user.State = updateUserDto.State;

                if (!string.IsNullOrEmpty(updateUserDto.Country))
                    user.Country = updateUserDto.Country;

                if (!string.IsNullOrEmpty(updateUserDto.PostalCode))
                    user.PostalCode = updateUserDto.PostalCode;

                // Save user changes
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to update user: {errors}");
                }

                // Update password if provided
                if (!string.IsNullOrEmpty(updateUserDto.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, updateUserDto.Password);

                    if (!passwordResult.Succeeded)
                    {
                        var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to update password: {errors}");
                    }
                }

                // Get current role for return
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.ToRoleString();

                return user.ToUserDto(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {UserId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return false;
                }

                // Check if user has related data before deleting
                var hasReservations = await _context.Reservations.AnyAsync(r => r.UserId == id);
                var hasCompletedOrders = await _context.ServiceOrders.AnyAsync(so => so.CompletedById == id);
                var hasAssignedTasks = await _context.CleaningTasks.AnyAsync(ct => ct.AssignedToId == id);
                var hasReportedRequests = await _context.MaintenanceRequests.AnyAsync(mr => mr.ReportedBy == id);
                var hasAssignedRequests = await _context.MaintenanceRequests.AnyAsync(mr => mr.AssignedTo == id);

                if (hasReservations || hasCompletedOrders || hasAssignedTasks || hasReportedRequests || hasAssignedRequests)
                {
                    // Instead of hard deleting, deactivate the user
                    user.IsActive = false;
                    user.AccountStatus = AccountStatus.Inactive;
                    await _userManager.UpdateAsync(user);
                    return true;
                }

                // If no related data, proceed with deletion
                var result = await _userManager.DeleteAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {UserId}", id);
                throw;
            }
        }

        #endregion

        #region Status Management

        public async Task<bool> ActivateUserAsync(int id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {id} not found");
                }

                user.IsActive = true;
                user.AccountStatus = AccountStatus.Active;

                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user with ID {UserId}", id);
                throw;
            }
        }

        public async Task<bool> DeactivateUserAsync(int id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {id} not found");
                }

                user.IsActive = false;
                user.AccountStatus = AccountStatus.Inactive;

                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user with ID {UserId}", id);
                throw;
            }
        }

        #endregion

        #region Role Management

        public async Task<bool> ChangeUserRoleAsync(int id, int roleId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {id} not found");
                }

                var role = await _roleManager.FindByIdAsync(roleId.ToString());
                if (role == null)
                {
                    throw new KeyNotFoundException($"Role with ID {roleId} not found");
                }

                // Get current roles and remove
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                // Add new role
                var result = await _userManager.AddToRoleAsync(user, role.Name);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing role for user with ID {UserId} to role ID {RoleId}", id, roleId);
                throw;
            }
        }

        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            try
            {
                var roles = await _roleManager.Roles.ToListAsync();
                return roles.Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all roles");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private async Task<Dictionary<int, string>> GetUserRolesAsync(List<int> userIds)
        {
            var result = new Dictionary<int, string>();

            foreach (var userId in userIds)
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    result[userId] = roles.ToRoleString();
                }
            }

            return result;
        }

        #endregion
    }
}

