using HotelManagementApp.Models.Enums;

namespace HotelManagementApp.Models
{
    public class CleaningTask
    {
        public int Id { get; set; }
        public string TaskId { get; set; }
        public int RoomId { get; set; }
        public Room Room { get; set; }
        public int AssignedToId { get; set; }
        public ApplicationUser AssignedTo { get; set; }
        public string Description { get; set; }
        
        public Priority Priority { get; set; } 
        public CleaningRequestStatus Status { get; set; } // Pending, InProgress, Completed
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string CompletionNotes { get; set; }
    }
}
