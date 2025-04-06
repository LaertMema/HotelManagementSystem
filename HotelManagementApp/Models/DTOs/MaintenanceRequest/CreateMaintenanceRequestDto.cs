namespace HotelManagementApp.Models.DTOs.MaintenanceRequest
{
    using global::HotelManagementApp.Models.Enums;
    using System;
    using System.ComponentModel.DataAnnotations;
        public class CreateMaintenanceRequestDto
        {
            
            [StringLength(200, ErrorMessage = "Title must be at most 200 characters")]
            public string? Title { get; set; }

            [Required]
            [StringLength(1000, ErrorMessage = "Description must be at most 1000 characters")]
            public string IssueDescription { get; set; }

            public int? RoomId { get; set; }

            public int? ReservationId { get; set; }

            [Required]
            public MaintenanceRequestPriority Priority { get; set; } = MaintenanceRequestPriority.Medium;

            public MaintenanceRequestType RequestType { get; set; } = MaintenanceRequestType.Repair;

            [StringLength(500)]
            public string? Location { get; set; }
        }

        public class UpdateMaintenanceRequestDto
        {
            [StringLength(200, ErrorMessage = "Title must be at most 200 characters")]
            public string Title { get; set; }

            [StringLength(1000, ErrorMessage = "Description must be at most 1000 characters")]
            public string IssueDescription { get; set; }

            public MaintenanceRequestStatus? Status { get; set; }

            public MaintenanceRequestPriority? Priority { get; set; }

            public int? AssignedTo { get; set; }

            [StringLength(1000)]
            public string? ResolutionNotes { get; set; }

            public DateTime? CompletedAt { get; set; }

            [StringLength(500)]
            public string? Location { get; set; }

            public decimal? CostOfRepair { get; set; }

            public bool? RequiresFollowUp { get; set; }
        }

        public class MaintenanceRequestDto
        {
            public int Id { get; set; }
            public string? Title { get; set; }
            public string IssueDescription { get; set; }
            public DateTime ReportDate { get; set; }
            public string Status { get; set; }
            public string Priority { get; set; }
            public string? RequestType { get; set; }
            public string? Location { get; set; }

            // Room information
            public int? RoomId { get; set; }
            public string RoomNumber { get; set; }
            public int? Floor { get; set; }

            // Reservation information if applicable
            public int? ReservationId { get; set; }
            public string ReservationNumber { get; set; }

            // Reporter information
            public int? ReportedBy { get; set; }
            public string ReportedByName { get; set; }

            // Assignment information
            public int? AssignedTo { get; set; }
            public string AssignedToName { get; set; }
            public DateTime? AssignedAt { get; set; }

            // Resolution information
            public string ResolutionNotes { get; set; }
            public DateTime? CompletedAt { get; set; }
            public decimal? CostOfRepair { get; set; }
            public bool? RequiresFollowUp { get; set; }
            public TimeSpan? TimeToResolve { get; set; }
        }

        public class MaintenanceRequestSummaryDto
        {
            public int Id { get; set; }
            public string? Title { get; set; }
            public DateTime ReportDate { get; set; }
            public string Status { get; set; }
            public string Priority { get; set; }
            public string RoomNumber { get; set; }
            public string AssignedToName { get; set; }
        }

        public enum MaintenanceRequestType
        {
            Repair,
            Replacement,
            Installation,
            Inspection,
            Other
        }
    

}
