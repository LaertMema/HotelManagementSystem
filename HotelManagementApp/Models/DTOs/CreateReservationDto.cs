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
        [FutureDate(ErrorMessage = "Check-in date must be in the future")]
        public DateTime CheckInDate { get; set; }

        [Required]
        [DateGreaterThan("CheckInDate", ErrorMessage = "Check-out date must be after check-in date")]
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
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            return value is DateTime date && date.Date >= DateTime.Today;
        }
    }
    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public DateGreaterThanAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var compareValue = validationContext.ObjectType.GetProperty(_comparisonProperty)?.GetValue(validationContext.ObjectInstance);

            if (value is DateTime endDate && compareValue is DateTime startDate && endDate <= startDate)
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
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
        [Required]
         public int RoomId { get; set; }
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
