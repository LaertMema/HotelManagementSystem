using HotelManagementApp.Models.Enums;

namespace HotelManagementApp.Models
{
    public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; }
    public int ReservationId { get; set; }
    public Reservation Reservation { get; set; }
    public decimal Amount { get; set; } // Base amount before tax
    public decimal Tax { get; set; }
    public decimal Total { get; set; } // Final amount including tax
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string Notes { get; set; }
    public DateTime? DueDate { get; set; }

        public Boolean IsPaid { get; set; } = false; // Indicates if the invoice is paid

        // Navigation property for multiple payments
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        // Payment Status: Dynamically determined based on payments
        public PaymentStatus Status
        {
            get
            {
                decimal totalPaid = Payments.Sum(p => p.AmountPaid);
                if (totalPaid >= Total) return PaymentStatus.Paid;
                if (totalPaid > 0) return PaymentStatus.PartiallyPaid;
                return PaymentStatus.Pending;
            }
        }
    }

}
