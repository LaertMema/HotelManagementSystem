using HotelManagementApp.Models.Enums;

namespace HotelManagementApp.Models
{
    public class ServiceOrder
    {
        public int Id { get; set; }
        public int ReservationId { get; set; }
        public int ServiceId { get; set; }
        public DateTime OrderDateTime { get; set; }
        public int Quantity { get; set; }
        public decimal PriceCharged { get; set; } //Cmimi i Service
        public decimal TotalPrice { get; set; } // Totali = PriceCharged * Quantity
        public ServiceOrderStatus Status { get; set; }
        public int? CompletedById { get; set; } // Nullable for pending orders
        public ApplicationUser CompletedBy { get; set; }

        public DateTime? ScheduledTime { get; set; } // Optional scheduled time for services that need scheduling
        public string? DeliveryLocation { get; set; } // For room service orders - specify room delivery location
        public string? SpecialInstructions { get; set; } // Any special instructions from the guest
        public DateTime? CompletedAt { get; set; } // Time when the service was completed
        public string? CompletionNotes { get; set; } // Any additional notes or comments
        public Reservation Reservation { get; set; }

        public Service Service { get; set; }
    }
}
