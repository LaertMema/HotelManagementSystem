namespace HotelManagementApp.Services.UserManagement
{
    using global::HotelManagementApp.Models.DTOs;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IUserService
    {
        // CRUD operations
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string roleName);
        Task<UserDto> GetUserByIdAsync(int id);
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<UserDto> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(int id);

        // Status management
        Task<bool> ActivateUserAsync(int id);
        Task<bool> DeactivateUserAsync(int id);

        // Role management
        Task<bool> ChangeUserRoleAsync(int id, int roleId);
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();
    }
}

