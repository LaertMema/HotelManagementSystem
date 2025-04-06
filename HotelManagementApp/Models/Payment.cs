using HotelManagementApp.Models.Enums;

namespace HotelManagementApp.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public PaymentMethod Method { get; set; }
        public int InvoiceId { get; set; } // Payments are tied to an invoice
        public Invoice Invoice { get; set; } // Navigation property
        public bool IsRefunded { get; set; } = false;
        public string? RefundReason { get; set; } // Nullable for non-refunded payments
        public int? ProcessedBy { get; set; } // Nullable for automatic transactions
        public ApplicationUser ProcessedByUser { get; set; }

        public string? TransactionId { get; set; } // Optional for tracking payment transactions
        public string? Notes { get; set; } // Optional notes for the payment
    }
}
