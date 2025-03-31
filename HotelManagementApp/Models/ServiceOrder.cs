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
        public DateTime? CompletedAt { get; set; } // Time when the service was completed

        public Reservation Reservation { get; set; }
        public Service Service { get; set; }
    }
}
