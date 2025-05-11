namespace HotelManagementApp.Models
{
    public class Feedback
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public ApplicationUser User { get; set; }
        public int? ReservationId { get; set; }
        public Reservation Reservation { get; set; }
        public string GuestName { get; set; }
        public string GuestEmail { get; set; }
        public int Rating { get; set; } // 1-5 star rating
        public string Subject { get; set; }
        public string Comments { get; set; }
        public string Category { get; set; } // Room, Service, Cleanliness, Food, etc.
        public bool IsResolved { get; set; }
        public string? ResolutionNotes { get; set; }
        public int? ResolvedById { get; set; }
        public ApplicationUser ? ResolvedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
