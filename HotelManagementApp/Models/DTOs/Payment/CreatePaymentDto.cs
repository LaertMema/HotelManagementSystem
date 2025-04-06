namespace HotelManagementApp.Models.DTOs.Payment
{
    using global::HotelManagementApp.Models.Enums;
    using System;
    using System.ComponentModel.DataAnnotations;
        public class CreatePaymentDto
        {
            [Required]
            public int InvoiceId { get; set; }

            [Required]
            [Range(0.01, 100000, ErrorMessage = "Amount must be between 0.01 and 100,000")]
            public decimal Amount { get; set; }

            [Required]
            public PaymentMethod PaymentMethod { get; set; }

            [StringLength(100)]
            public string TransactionId { get; set; }

            [StringLength(500)]
            public string Notes { get; set; }
        }

        public class PaymentDto
        {
            public int Id { get; set; }
            public int InvoiceId { get; set; }
            public string InvoiceNumber { get; set; }
            public int ReservationId { get; set; }
            public string ReservationNumber { get; set; }
            public int GuestId { get; set; }
            public string GuestName { get; set; }
            public decimal Amount { get; set; }
            public DateTime PaymentDate { get; set; }
            public string PaymentMethod { get; set; }
        public string TransactionId { get; set; }
        public string Notes { get; set; }
        public int? ProcessedById { get; set; }
            public string ProcessedByName { get; set; }
            public bool IsRefunded { get; set; }
            //public DateTime? RefundedAt { get; set; }
            public string RefundReason { get; set; }
        }

        public class PaymentSummaryDto
        {
            public int Id { get; set; }
            public string InvoiceNumber { get; set; }
            public string GuestName { get; set; }
            public decimal Amount { get; set; }
            public DateTime PaymentDate { get; set; }
            public string PaymentMethod { get; set; }
            public string TransactionId { get; set; }
        }

        public class RefundPaymentDto
        {
            [Required]
            [StringLength(500)]
            public string RefundReason { get; set; }

            [Range(0.01, 100000, ErrorMessage = "Amount must be between 0.01 and 100,000")]
            public decimal? RefundAmount { get; set; }  // If null, refund full amount
        }
    

}
