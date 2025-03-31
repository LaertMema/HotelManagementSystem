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
        //public int? ProcessedBy { get; set; } // Nullable for automatic transactions
        //public ApplicationUser ProcessedByUser { get; set; }
    }
}
