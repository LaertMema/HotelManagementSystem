namespace HotelManagementApp.Models.DTOs
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using HotelManagementApp.Models.Enums;

    // Request DTOs
    public class CreateUserDto
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(30)]
        public string Username { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(50)]
        public string City { get; set; }

        [StringLength(50)]
        public string State { get; set; }

        [StringLength(50)]
        public string Country { get; set; }

        [StringLength(20)]
        public string PostalCode { get; set; }

        [Required]
        public string Role { get; set; }
    }

    public class UpdateUserDto
    {
        [StringLength(50)]
        public string FirstName { get; set; }

        [StringLength(50)]
        public string LastName { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(50)]
        public string City { get; set; }

        [StringLength(50)]
        public string State { get; set; }

        [StringLength(50)]
        public string Country { get; set; }

        [StringLength(20)]
        public string PostalCode { get; set; }

        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public class UserListItemDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
        public DateTime? LastLogin { get; set; }
    }

    public class UserDetailDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public AccountStatus AccountStatus { get; set; }
        public DateTime Created { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool PasswordResetRequired { get; set; }

        // Statistics
        public int ReservationsCount { get; set; }
        public int ServiceOrdersCompletedCount { get; set; }
        public int CleaningTasksAssignedCount { get; set; }
        public int MaintenanceRequestsReportedCount { get; set; }
        public int MaintenanceRequestsAssignedCount { get; set; }
    }

    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class UserStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public Dictionary<string, int> UsersByRole { get; set; }
        public int NewUsersLast30Days { get; set; }
        public List<UserActivityDto> MostActiveUsers { get; set; }
    }

    public class UserActivityDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public int ActivityCount { get; set; }
    }
}

