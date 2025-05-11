namespace HotelManagementApp.Models.DTOs
{
    using global::HotelManagementApp.Models.Enums;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
        public class CreateInvoiceDto
        {
            [Required]
            public int ReservationId { get; set; }

            [Required]
            [Range(0, 100000, ErrorMessage = "Amount must be between 0 and 100,000")]
            public decimal Amount { get; set; }

            [Range(0, 30, ErrorMessage = "Tax percentage must be between 0 and 30")]
            public decimal TaxPercentage { get; set; } = 10; // Default 10% tax

            [StringLength(500, ErrorMessage = "Notes must be at most 500 characters")]
            public string Notes { get; set; }

            public DateTime? DueDate { get; set; }

            public List<InvoiceItemDto> Items { get; set; } = new List<InvoiceItemDto>();
        }

        public class UpdateInvoiceDto
        {
            [Range(0, 100000, ErrorMessage = "Amount must be between 0 and 100,000")]
            public decimal? Amount { get; set; }

            [Range(0, 30, ErrorMessage = "Tax percentage must be between 0 and 30")]
            public decimal? TaxPercentage { get; set; }

            public PaymentStatus? Status { get; set; }

            [StringLength(500, ErrorMessage = "Notes must be at most 500 characters")]
            public string Notes { get; set; }

            public DateTime? DueDate { get; set; }

            public DateTime? PaidAt { get; set; }

            public List<InvoiceItemDto> Items { get; set; }
        }

        public class InvoiceItemDto
        {
            public int? Id { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "Description must be at most 100 characters")]
            public string Description { get; set; }

            [Required]
            [Range(0, 100000, ErrorMessage = "Amount must be between 0 and 100,000")]
            public decimal Amount { get; set; }

            [Required]
            [Range(0, 100, ErrorMessage = "Quantity must be between 0 and 100")]
            public int Quantity { get; set; } = 1;

            public int? ServiceOrderId { get; set; }

            public ItemType ItemType { get; set; } = ItemType.Service;

            public decimal TotalAmount => Amount * Quantity;
        }

        public class InvoiceDto
        {
            public int Id { get; set; }
            public string InvoiceNumber { get; set; }
            public int ReservationId { get; set; }
            public string ReservationNumber { get; set; }
             public bool IsPaid { get; set; }


        // Guest information
        public int GuestId { get; set; }
            public string GuestName { get; set; }
            public string GuestEmail { get; set; }

            // Amount information
            public decimal Subtotal { get; set; }
            public decimal TaxAmount { get; set; }
            public decimal TaxPercentage { get; set; }
            public decimal Total { get; set; }
            public decimal AmountPaid { get; set; }
            public decimal Balance => Total - AmountPaid;

            // Dates
            public DateTime CreatedAt { get; set; }
            public DateTime? DueDate { get; set; }
            public DateTime? PaidAt { get; set; }

            // Status
            public string Status { get; set; }
            public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.Today && Balance > 0;

            // Notes
            public string Notes { get; set; }

            // Related items
            public List<InvoiceItemDto> Items { get; set; }
            public List<PaymentDto> Payments { get; set; }

            // Stay information
            public DateTime CheckInDate { get; set; }
            public DateTime CheckOutDate { get; set; }
            public string RoomNumber { get; set; }
            public string RoomTypeName { get; set; }
            public int NumberOfNights { get; set; }
        }

        public class InvoiceSummaryDto
        {
            public int Id { get; set; }
            public string InvoiceNumber { get; set; }
            public string GuestName { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal Total { get; set; }
            public decimal AmountPaid { get; set; }
            public decimal Balance { get; set; }
            public string Status { get; set; }
            public bool IsOverdue { get; set; }
            public DateTime? DueDate { get; set; }
        }

        public class PaymentDto
        {
            public int Id { get; set; }
            public decimal Amount { get; set; }
            public DateTime PaymentDate { get; set; }
            public string PaymentMethod { get; set; }
            public string TransactionId { get; set; }
            public string ProcessedByName { get; set; }
            public string Notes { get; set; }
        }

        public enum ItemType
        {
            Accommodation,
            Service,
            FoodAndBeverage,
            Tax,
            Discount,
            Other
        }
    

}
