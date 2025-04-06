namespace HotelManagementApp.Models.DTOs.ServiceOrder
{
    using global::HotelManagementApp.Models.Enums;
    using System;
    using System.ComponentModel.DataAnnotations;
        public class CreateServiceOrderDto
        {
            [Required]
            public int ReservationId { get; set; }

            [Required]
            public int ServiceId { get; set; }

            [Required]
            [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
            public int Quantity { get; set; }

            [StringLength(500, ErrorMessage = "Special instructions must be at most 500 characters")]
            public string SpecialInstructions { get; set; }

            // Optional scheduled time for services that need scheduling
            public DateTime? ScheduledTime { get; set; }

            // For room service orders - specify room delivery location
            public string DeliveryLocation { get; set; }
        }

        public class UpdateServiceOrderDto
        {
            [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
            public int? Quantity { get; set; }

            public ServiceOrderStatus? Status { get; set; }

            [StringLength(500, ErrorMessage = "Special instructions must be at most 500 characters")]
            public string SpecialInstructions { get; set; }

            // When order was completed
            public DateTime? CompletedAt { get; set; }

            // Staff member who completed the order
            public int? CompletedById { get; set; }

            // Optional scheduled time update
            public DateTime? ScheduledTime { get; set; }

            // For room service orders - specify room delivery location
            public string DeliveryLocation { get; set; }

            // Notes from staff who completed the order
            [StringLength(500, ErrorMessage = "Completion notes must be at most 500 characters")]
            public string CompletionNotes { get; set; }
        }

        public class ServiceOrderDto
        {
            public int Id { get; set; }
            public int ReservationId { get; set; }
            public string ReservationNumber { get; set; }
            public int ServiceId { get; set; }
            public string ServiceName { get; set; }
            public string ServiceDescription { get; set; }
            public DateTime OrderDateTime { get; set; }
            public int Quantity { get; set; }
            public decimal PricePerUnit { get; set; }
            public decimal TotalPrice { get; set; }
            public string Status { get; set; }
            public string SpecialInstructions { get; set; }
            public string DeliveryLocation { get; set; }
            public DateTime? ScheduledTime { get; set; }

            // Completion information
            public int? CompletedById { get; set; }
            public string CompletedByName { get; set; }
            public DateTime? CompletedAt { get; set; }
            public string CompletionNotes { get; set; }

            // Guest information
            public int GuestId { get; set; }
            public string GuestName { get; set; }
            public string RoomNumber { get; set; }

            // Elapsed time since order placed
            public TimeSpan ElapsedTime { get; set; }

            // Category of service (for filtering)
            public string ServiceCategory { get; set; }
        }

        public class ServiceOrderSummaryDto
        {
            public int Id { get; set; }
            public string ServiceName { get; set; }
            public string ReservationNumber { get; set; }
            public string RoomNumber { get; set; }
            public string GuestName { get; set; }
            public DateTime OrderDateTime { get; set; }
            public string Status { get; set; }
            public DateTime? ScheduledTime { get; set; }
            public decimal TotalPrice { get; set; }
            public TimeSpan ElapsedTime { get; set; }
        }

        public class ServiceOrderStatisticsDto
        {
            public int TotalOrders { get; set; }
            public int PendingOrders { get; set; }
            public int InProgressOrders { get; set; }
            public int CompletedOrders { get; set; }
            public int CancelledOrders { get; set; }
            public decimal TotalRevenue { get; set; }
            public Dictionary<string, int> OrdersByServiceType { get; set; }
            public Dictionary<string, decimal> RevenueByServiceType { get; set; }
            public double AverageCompletionTime { get; set; } // In minutes
        }
    

}
