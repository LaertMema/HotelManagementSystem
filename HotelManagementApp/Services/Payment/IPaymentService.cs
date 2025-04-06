namespace HotelManagementApp.Services.Payment
{
    using global::HotelManagementApp.Models.DTOs.Payment;
    using global::HotelManagementApp.Models.Enums;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

        public interface IPaymentService
        {
            Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto paymentDto, int processedById);
            Task<PaymentDto> GetPaymentByIdAsync(int id);
            Task<IEnumerable<PaymentDto>> GetPaymentsByInvoiceIdAsync(int invoiceId);
            Task<IEnumerable<PaymentDto>> GetPaymentsByReservationIdAsync(int reservationId);
            Task<IEnumerable<PaymentDto>> GetRecentPaymentsAsync(int count = 20);
            Task<IEnumerable<PaymentDto>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate);
            Task<PaymentDto> RefundPaymentAsync(int paymentId, RefundPaymentDto refundDto, int processedById);
            Task<decimal> GetTotalPaymentsForPeriodAsync(DateTime startDate, DateTime endDate);
            Task<Dictionary<PaymentMethod, decimal>> GetPaymentBreakdownByMethodAsync(DateTime startDate, DateTime endDate);
        }
    

}
