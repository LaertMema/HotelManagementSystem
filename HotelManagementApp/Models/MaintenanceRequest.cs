using HotelManagementApp.Models.Enums;

namespace HotelManagementApp.Models
{
    public class MaintenanceRequest
    {
        public int Id { get; set; }
        public DateTime ReportDate { get; set; }
        public string IssueDescription { get; set; }
        public MaintenanceRequestStatus Status { get; set; }
        public MaintenanceRequestPriority Priority { get; set; }
        public int? ReportedBy { get; set; }
        public int? AssignedTo { get; set; }
        public int? RoomId { get; set; }

        //public int? ReservationId { get; set; }
        //public string Location { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ResolutionNotes { get; set; }

        public ApplicationUser ReportedByUser { get; set; }
        public ApplicationUser AssignedToUser { get; set; }
        public Room Room { get; set; }
    }
}
