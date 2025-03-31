using HotelManagementApp.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace HotelManagementApp.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        // Personal Information
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        // Contact Information
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }

        // Security & Account Status
        public bool IsActive { get; set; } = true;
        public bool PasswordResetRequired { get; set; }
        public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }

        // Guest-Specific Fields
        public string IdType { get; set; }
        public string IdNumber { get; set; }

        // Staff-Specific Fields
        public DateTime? HireDate { get; set; }

        // Navigation Properties
        public ICollection<Reservation> Reservations { get; set; }
        public ICollection<ServiceOrder> ServiceOrdersCompleted { get; set; }
        public ICollection<MaintenanceRequest> MaintenanceRequestsReported { get; set; }
        public ICollection<MaintenanceRequest> MaintenanceRequestsAssigned { get; set; }
        public ICollection<Report> Reports { get; set; }

        //public ICollection<ServiceOrder> ServiceOrdersCompleted { get; set; }
        public ICollection<CleaningTask> AssignedCleaningTasks { get; set; }
        public ICollection<Feedback> Feedback { get; set; }
        public ICollection<Feedback> ResolvedFeedback { get; set; }
        //Per receptionist
        public ICollection<Reservation>? CheckedOutReservations { get; set; }
        public ICollection<Reservation>? CheckedInReservations { get; set; }
    }


}
