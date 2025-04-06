namespace HotelManagementApp.Models.DTOs
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using HotelManagementApp.Models.Enums;


    // Request DTOs
    public class CreateReservationDto
        {
            [Required]
            public DateTime CheckInDate { get; set; }

            [Required]
            public DateTime CheckOutDate { get; set; }

            [Required]
            public int RoomTypeId { get; set; }

            [Required]
            [Range(1, 10, ErrorMessage = "Number of guests must be between 1 and 10")]
            public int NumberOfGuests { get; set; }

            [StringLength(500)]
            public string SpecialRequests { get; set; }

            public PaymentMethod PaymentMethod { get; set; }
        }

        public class UpdateReservationDto
        {
            public DateTime? CheckInDate { get; set; }

            public DateTime? CheckOutDate { get; set; }

            public int? RoomTypeId { get; set; }

            [Range(1, 10, ErrorMessage = "Number of guests must be between 1 and 10")]
            public int? NumberOfGuests { get; set; }

            [StringLength(500)]
            public string SpecialRequests { get; set; }

            public PaymentMethod? PaymentMethod { get; set; }
        }

        public class AssignRoomDto
        {
            [Required]
            public int RoomId { get; set; }
        }

        public class CheckInDto
        {
            [StringLength(500)]
            public string Notes { get; set; }
        }

        public class CheckOutDto
        {
            [StringLength(500)]
            public string Notes { get; set; }
        }

        public class CancelReservationDto
        {
            [Required]
            [StringLength(500)]
            public string CancellationReason { get; set; }
        }

        // Response DTOs
        public class ReservationDto
        {
            public int Id { get; set; }
            public string ReservationNumber { get; set; }
            public int UserId { get; set; }
            public string UserName { get; set; }
            public string UserEmail { get; set; }
            public DateTime ReservationDate { get; set; }
            public DateTime CheckInDate { get; set; }
            public DateTime CheckOutDate { get; set; }
            public string Status { get; set; }
            public decimal TotalPrice { get; set; }
            public string PaymentMethod { get; set; }
            public string PaymentStatus { get; set; }
            public int NumberOfGuests { get; set; }
            public string SpecialRequests { get; set; }

            // Room Information
            public int? RoomId { get; set; }
            public string RoomNumber { get; set; }
            public int RoomTypeId { get; set; }
            public string RoomTypeName { get; set; }

            // Tracking user actions
            public DateTime? CheckedInTime { get; set; }
            public DateTime? CheckedOutTime { get; set; }
            public string CheckedInByUserName { get; set; }
            public string CheckedOutByUserName { get; set; }

            // Cancellation Tracking
            public DateTime? CancelledAt { get; set; }
            public string CancellationReason { get; set; }

            // Related items counts
            public int ServiceOrdersCount { get; set; }
            public int InvoicesCount { get; set; }
            public int FeedbackCount { get; set; }
            public bool HasUnpaidInvoices { get; set; }
        }

        public class ReservationSummaryDto
        {
            public int Id { get; set; }
            public string ReservationNumber { get; set; }
            public string GuestName { get; set; }
            public DateTime CheckInDate { get; set; }
            public DateTime CheckOutDate { get; set; }
            public string Status { get; set; }
            public string RoomTypeName { get; set; }
            public string RoomNumber { get; set; }
            public decimal TotalPrice { get; set; }
            public string PaymentStatus { get; set; }
        }

        public class ReservationStatisticsDto
        {
            public int TotalReservations { get; set; }
            public int ActiveReservations { get; set; }
            public int UpcomingReservations { get; set; }
            public int CompletedReservations { get; set; }
            public int CancelledReservations { get; set; }
            public decimal TotalRevenue { get; set; }
            public decimal OutstandingPayments { get; set; }
            public Dictionary<string, int> ReservationsByRoomType { get; set; }
            public Dictionary<string, int> ReservationsByStatus { get; set; }
            public double OccupancyRate { get; set; } // Current occupancy as percentage
        }
    
}
