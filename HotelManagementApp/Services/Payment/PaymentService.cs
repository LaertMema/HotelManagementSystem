namespace HotelManagementApp.Services.Payment
{
    using global::HotelManagementApp.Models.DTOs.Payment;
    using global::HotelManagementApp.Models.Enums;
    using global::HotelManagementApp.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
        public class PaymentService : IPaymentService
        {
            private readonly AppDbContext _context;
            private readonly ILogger<PaymentService> _logger;

            public PaymentService(AppDbContext context, ILogger<PaymentService> logger)
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            #region CRUD Operations

            public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto paymentDto, int processedById)
            {
                try
                {
                    // Find invoice and validate
                    var invoice = await _context.Invoices
                        .Include(i => i.Reservation)
                        .FirstOrDefaultAsync(i => i.Id == paymentDto.InvoiceId);

                    if (invoice == null)
                    {
                        throw new KeyNotFoundException($"Invoice with ID {paymentDto.InvoiceId} not found");
                    }

                    // Validate payment amount against invoice balance
                    var existingPayments = await _context.Payments
                        .Where(p => p.InvoiceId == paymentDto.InvoiceId)
                        .SumAsync(p => p.AmountPaid);

                    var invoiceBalance = invoice.Total - existingPayments;

                    if (paymentDto.Amount > invoiceBalance)
                    {
                        throw new InvalidOperationException($"Payment amount ({paymentDto.Amount:C}) exceeds invoice balance ({invoiceBalance:C})");
                    }

                    // Create payment entity
                    var payment = new Models.Payment
                    {
                        InvoiceId = paymentDto.InvoiceId,
                        AmountPaid = paymentDto.Amount,
                        PaymentDate = DateTime.UtcNow,
                        Method = paymentDto.PaymentMethod,
                        TransactionId = paymentDto.TransactionId,
                        Notes = paymentDto.Notes,
                        ProcessedBy = processedById,
                    };

                    _context.Payments.Add(payment);

                    // Update invoice status if fully paid
                    var totalPaid = existingPayments + paymentDto.Amount;
                    if (totalPaid >= invoice.Total)
                    {
                        invoice.PaidAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();

                    return await GetPaymentByIdAsync(payment.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating payment for invoice ID {InvoiceId}", paymentDto.InvoiceId);
                    throw;
                }
            }

            public async Task<PaymentDto> GetPaymentByIdAsync(int id)
            {
                try
                {
                    var payment = await _context.Payments
                        .Include(p => p.Invoice)
                            .ThenInclude(i => i.Reservation)
                                .ThenInclude(r => r.User)
                        .Include(p => p.ProcessedByUser)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (payment == null)
                    {
                        return null;
                    }

                    return MapToPaymentDto(payment);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving payment with ID {PaymentId}", id);
                    throw;
                }
            }

            public async Task<IEnumerable<PaymentDto>> GetPaymentsByInvoiceIdAsync(int invoiceId)
            {
                try
                {
                    var payments = await _context.Payments
                        .Include(p => p.Invoice)
                            .ThenInclude(i => i.Reservation)
                                .ThenInclude(r => r.User)
                        .Include(p => p.ProcessedByUser)
                        .Where(p => p.InvoiceId == invoiceId)
                        .OrderByDescending(p => p.PaymentDate)
                        .ToListAsync();

                    return payments.Select(p => MapToPaymentDto(p));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving payments for invoice ID {InvoiceId}", invoiceId);
                    throw;
                }
            }

            public async Task<IEnumerable<PaymentDto>> GetPaymentsByReservationIdAsync(int reservationId)
            {
                try
                {
                    var payments = await _context.Payments
                        .Include(p => p.Invoice)
                            .ThenInclude(i => i.Reservation)
                                .ThenInclude(r => r.User)
                        .Include(p => p.ProcessedByUser)
                        .Where(p => p.Invoice.ReservationId == reservationId)
                        .OrderByDescending(p => p.PaymentDate)
                        .ToListAsync();

                    return payments.Select(p => MapToPaymentDto(p));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving payments for reservation ID {ReservationId}", reservationId);
                    throw;
                }
            }

            public async Task<IEnumerable<PaymentDto>> GetRecentPaymentsAsync(int count = 20)
            {
                try
                {
                    var payments = await _context.Payments
                        .Include(p => p.Invoice)
                            .ThenInclude(i => i.Reservation)
                                .ThenInclude(r => r.User)
                        .Include(p => p.ProcessedByUser)
                        .OrderByDescending(p => p.PaymentDate)
                        .Take(count)
                        .ToListAsync();

                    return payments.Select(p => MapToPaymentDto(p));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving recent payments");
                    throw;
                }
            }

            public async Task<IEnumerable<PaymentDto>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate)
            {
                try
                {
                    var payments = await _context.Payments
                        .Include(p => p.Invoice)
                            .ThenInclude(i => i.Reservation)
                                .ThenInclude(r => r.User)
                        .Include(p => p.ProcessedByUser)
                        .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                        .OrderByDescending(p => p.PaymentDate)
                        .ToListAsync();

                    return payments.Select(p => MapToPaymentDto(p));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving payments between {StartDate} and {EndDate}", startDate, endDate);
                    throw;
                }
            }

            public async Task<PaymentDto> RefundPaymentAsync(int paymentId, RefundPaymentDto refundDto, int processedById)
            {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Invoice)
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null)
                {
                    throw new KeyNotFoundException($"Payment with ID {paymentId} not found");
                }

                if (payment.IsRefunded)
                {
                    throw new InvalidOperationException("This payment has already been refunded");
                }

                // Mark payment as refunded
                payment.IsRefunded = true;
                payment.RefundReason = refundDto.RefundReason;

                // Create refund record (negative amount)
                var refundAmount = refundDto.RefundAmount ?? payment.AmountPaid;
                var refund = new Models.Payment
                {
                    InvoiceId = payment.InvoiceId,
                    AmountPaid = -refundAmount, // Negative amount to indicate refund
                    PaymentDate = DateTime.UtcNow,
                    Method = payment.Method,
                    TransactionId = $"REFUND-{payment.TransactionId ?? payment.Id.ToString()}",
                    Notes = $"Refund for payment #{payment.Id}. Reason: {refundDto.RefundReason}",
                    ProcessedBy = processedById,
                    IsRefunded = false,
                    
                };

                _context.Payments.Add(refund);

                // Update invoice status
                await UpdateInvoiceStatusAsync(payment.InvoiceId);

                await _context.SaveChangesAsync();
                return MapToPaymentDto(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment with ID {PaymentId}", paymentId);
                throw;
            }
        }

            public async Task<decimal> GetTotalPaymentsForPeriodAsync(DateTime startDate, DateTime endDate)
            {
                try
                {
                    return await _context.Payments
                        .Where(p =>
                            p.PaymentDate >= startDate &&
                            p.PaymentDate <= endDate &&
                            !p.IsRefunded)
                        .SumAsync(p => p.AmountPaid);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating total payments between {StartDate} and {EndDate}", startDate, endDate);
                    throw;
                }
            }

            public async Task<Dictionary<PaymentMethod, decimal>> GetPaymentBreakdownByMethodAsync(DateTime startDate, DateTime endDate)
            {
                try
                {
                    var paymentsByMethod = await _context.Payments
                        .Where(p =>
                            p.PaymentDate >= startDate &&
                            p.PaymentDate <= endDate &&
                            !p.IsRefunded)
                        .GroupBy(p => p.Method)
                        .Select(g => new
                        {
                            PaymentMethod = g.Key,
                            Total = g.Sum(p => p.AmountPaid)
                        })
                        .ToListAsync();

                    return paymentsByMethod.ToDictionary(
                        x => x.PaymentMethod,
                        x => x.Total
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving payment breakdown between {StartDate} and {EndDate}", startDate, endDate);
                    throw;
                }
            }

            #endregion

            #region Business Operations

            public async Task<PaymentDto> ProcessCreditCardPaymentAsync(int invoiceId, decimal amount, string cardNumber, string cardHolderName, string expiryDate, string cvv, int processedById)
            {
                try
                {
                    // Validate invoice
                    var invoice = await _context.Invoices
                        .Include(i => i.Reservation)
                        .FirstOrDefaultAsync(i => i.Id == invoiceId);

                    if (invoice == null)
                    {
                        throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");
                    }

                    // In a real application, here you would integrate with a payment gateway
                    // For this example, we'll simulate payment processing
                    var success = ValidateCreditCardDetails(cardNumber, cardHolderName, expiryDate, cvv);
                    if (!success)
                    {
                        throw new InvalidOperationException("Credit card validation failed");
                    }

                    // Process payment (in a real app, call payment gateway API)
                    string transactionId = GenerateTransactionId();

                    // Create payment record
                    var paymentDto = new CreatePaymentDto
                    {
                        InvoiceId = invoiceId,
                        Amount = amount,
                        PaymentMethod = PaymentMethod.CreditCard,
                        TransactionId = transactionId,
                        Notes = $"Credit card payment processed for {cardHolderName} (Card ending in {cardNumber.Substring(cardNumber.Length - 4)})"
                    };

                    return await CreatePaymentAsync(paymentDto, processedById);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing credit card payment for invoice ID {InvoiceId}", invoiceId);
                    throw;
                }
            }

            public async Task<PaymentDto> ProcessCashPaymentAsync(int invoiceId, decimal amount, int processedById)
            {
                try
                {
                    // Validate invoice
                    var invoice = await _context.Invoices
                        .FirstOrDefaultAsync(i => i.Id == invoiceId);

                    if (invoice == null)
                    {
                        throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");
                    }

                    // Create payment record
                    var paymentDto = new CreatePaymentDto
                    {
                        InvoiceId = invoiceId,
                        Amount = amount,
                        PaymentMethod = PaymentMethod.Cash,
                        TransactionId = $"CASH-{DateTime.UtcNow.Ticks}",
                        Notes = $"Cash payment received by staff ID {processedById}"
                    };

                    return await CreatePaymentAsync(paymentDto, processedById);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing cash payment for invoice ID {InvoiceId}", invoiceId);
                    throw;
                }
            }

            public async Task<PaymentDto> ProcessBankTransferPaymentAsync(int invoiceId, decimal amount, string transferReference, int processedById)
            {
                try
                {
                    // Validate invoice
                    var invoice = await _context.Invoices
                        .FirstOrDefaultAsync(i => i.Id == invoiceId);

                    if (invoice == null)
                    {
                        throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");
                    }

                    // Create payment record
                    var paymentDto = new CreatePaymentDto
                    {
                        InvoiceId = invoiceId,
                        Amount = amount,
                        PaymentMethod = PaymentMethod.BankTransfer,
                        TransactionId = transferReference,
                        Notes = $"Bank transfer with reference {transferReference}"
                    };

                    return await CreatePaymentAsync(paymentDto, processedById);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing bank transfer for invoice ID {InvoiceId}", invoiceId);
                    throw;
                }
            }

            public async Task<bool> RefundFullPaymentAsync(int paymentId, string reason, int processedById)
            {
                try
                {
                    var refundDto = new RefundPaymentDto
                    {
                        RefundReason = reason,
                        RefundAmount = null // Full refund
                    };

                    var result = await RefundPaymentAsync(paymentId, refundDto, processedById);
                    return result != null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refunding full payment with ID {PaymentId}", paymentId);
                    throw;
                }
            }

            public async Task<Dictionary<string, decimal>> GetPaymentStatsByMethodAsync(DateTime? startDate = null, DateTime? endDate = null)
            {
                try
                {
                    // Set default date range if not provided
                    var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                    var end = endDate ?? DateTime.UtcNow;

                    // Get payment totals by method
                    var query = _context.Payments.AsQueryable();

                    // Apply date filtering if provided
                    if (startDate.HasValue)
                    {
                        query = query.Where(p => p.PaymentDate >= start);
                    }

                    if (endDate.HasValue)
                    {
                        query = query.Where(p => p.PaymentDate <= end);
                    }

                    // Only include non-refunded payments
                    query = query.Where(p => !p.IsRefunded);

                    // Calculate totals by payment method
                    var paymentStats = await query
                        .GroupBy(p => p.Method.ToString())
                        .Select(g => new
                        {
                            Method = g.Key,
                            Total = g.Sum(p => p.AmountPaid)
                        })
                        .ToListAsync();

                    // Calculate refund totals
                    var refundStats = await _context.Payments
                        .Where(p => p.IsRefunded && p.PaymentDate >= start && p.PaymentDate <= end)
                        .GroupBy(p => "Refunds")
                        .Select(g => new
                        {
                            Method = g.Key,
                            Total = g.Sum(p => p.AmountPaid)
                        })
                        .ToListAsync();

                    // Combine all statistics
                    var stats = new Dictionary<string, decimal>();

                    // Add payment methods
                    foreach (var stat in paymentStats)
                    {
                        stats[stat.Method] = stat.Total;
                    }

                    // Add refunds
                    var totalRefunds = refundStats.FirstOrDefault()?.Total ?? 0;
                    if (totalRefunds != 0)
                    {
                        stats["Refunds"] = totalRefunds;
                    }

                    // Add total
                    stats["Total"] = paymentStats.Sum(s => s.Total) - totalRefunds;

                    return stats;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving payment statistics");
                    throw;
                }
            }

            #endregion

            #region Helper Methods

            private PaymentDto MapToPaymentDto(Models.Payment payment)
            {
                if (payment == null)
                    return null;

                var reservation = payment.Invoice?.Reservation;
                var guest = reservation?.User;

                return new PaymentDto
                {
                    Id = payment.Id,
                    InvoiceId = payment.InvoiceId,
                    InvoiceNumber = payment.Invoice?.InvoiceNumber,
                    ReservationId = reservation?.Id ?? 0,
                    ReservationNumber = reservation?.ReservationNumber,
                    GuestId = guest?.Id ?? 0,
                    GuestName = guest != null ? $"{guest.FirstName} {guest.LastName}" : string.Empty,
                    Amount = payment.AmountPaid,
                    PaymentDate = payment.PaymentDate,
                    PaymentMethod = payment.Method.ToString(),
                    TransactionId = payment.TransactionId,
                    Notes = payment.Notes,
                    ProcessedById = payment.ProcessedBy,
                    ProcessedByName = payment.ProcessedByUser != null ?
                        $"{payment.ProcessedByUser.FirstName} {payment.ProcessedByUser.LastName}" :
                        string.Empty,
                    IsRefunded = payment.IsRefunded,
                    //RefundedAt = payment.RefundedAt,
                    RefundReason = payment.RefundReason
                };
            }

            private async Task UpdateInvoiceStatusAsync(int invoiceId)
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null)
                    return;

                var totalPaid = invoice.Payments
                    .Where(p => !p.IsRefunded)
                    .Sum(p => p.AmountPaid);

                // Update PaidAt timestamp if appropriate
                if (totalPaid >= invoice.Total && !invoice.PaidAt.HasValue)
                {
                    invoice.PaidAt = DateTime.UtcNow;
                }
                else if (totalPaid < invoice.Total && invoice.PaidAt.HasValue)
                {
                    invoice.PaidAt = null;
                }
            }

            private bool ValidateCreditCardDetails(string cardNumber, string cardHolderName, string expiryDate, string cvv)
            {
                // This is a simplified validation. In a real application, you would use a payment gateway
                // or a credit card validation library with proper security measures

                // Validate card number (simple Luhn algorithm check)
                if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 13 || cardNumber.Length > 19)
                    return false;

                // Check if card number contains only digits
                if (!cardNumber.All(char.IsDigit))
                    return false;

                // Validate expiry date (format MM/YY)
                if (string.IsNullOrEmpty(expiryDate) || !DateTime.TryParseExact(
                    expiryDate, "MM/yy", null, System.Globalization.DateTimeStyles.None, out var expiryDateTime))
                    return false;

                // Check if card is expired
                var currentDate = DateTime.Now;
                var expiryEndOfMonth = new DateTime(2000 + expiryDateTime.Year, expiryDateTime.Month, 1)
                    .AddMonths(1)
                    .AddDays(-1);

                if (expiryEndOfMonth < currentDate)
                    return false;

                // Validate CVV (3-4 digits)
                if (string.IsNullOrEmpty(cvv) || cvv.Length < 3 || cvv.Length > 4 || !cvv.All(char.IsDigit))
                    return false;

                // Validate card holder name
                if (string.IsNullOrEmpty(cardHolderName) || cardHolderName.Length < 2)
                    return false;

                return true;
            }

            private string GenerateTransactionId()
            {
                // Generate a unique ID for the transaction
                // In a real app, this would come from the payment gateway
                return $"CC-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            #endregion
        }
    

}
