using HotelManagementApp.Models.Enums;

namespace HotelManagementApp.Models
{
    public class Reservation
    {
        public int Id { get; set; }
        public string ReservationNumber { get; set; }
        public int UserId { get; set; }
        public ApplicationUser User { get; set; }
        public DateTime ReservationDate { get; set; } // Track when the reservation was made
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public ReservationStatus Status { get; set; } // Enum instead of string
        public decimal TotalPrice { get; set; }
        public PaymentMethod PaymentMethod { get; set; } // Use Enum
        public PaymentStatus PaymentStatus { get; set; } // Use Enum
        public int NumberOfGuests { get; set; }
        public string SpecialRequests { get; set; }

        // Room Information
        public int? RoomId { get; set; } // Nullable for unassigned rooms
        public Room Room { get; set; }

        public int RoomTypeId { get; set; }
        public RoomType RoomType { get; set; }

        // Tracking user actions
        public int? CreatedBy { get; set; }
        public DateTime? CheckedInTime { get; set; }
        public DateTime? CheckedOutTime { get; set; }
        public int? CheckedInBy { get; set; }
        public int? CheckedOutBy { get; set; }
        public ApplicationUser CreatedByUser { get; set; }
        public ApplicationUser CheckedInByUser { get; set; }
        public ApplicationUser CheckedOutByUser { get; set; }

        // Cancellation Tracking
        public DateTime? CancelledAt { get; set; }
        public string CancellationReason { get; set; }

        // Relationships
        public ICollection<ServiceOrder> ServiceOrders { get; set; }
        public ICollection<Payment> Payments { get; set; }//?? Mund te jete shtese
        public ICollection<Invoice> Invoices { get; set; }
        public ICollection<Feedback>? Feedback { get; set; }
    }

}
