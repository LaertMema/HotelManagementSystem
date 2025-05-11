namespace HotelManagementApp.Services.InvoiceSpace
{
    using HotelManagementApp.Models;
    using HotelManagementApp.Models.DTOs;
    using HotelManagementApp.Models.Enums;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;

    public class InvoiceService : IInvoiceService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<InvoiceService> _logger;

        public InvoiceService(AppDbContext context, ILogger<InvoiceService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region CRUD Operations

        public async Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync()
        {
            try
            {
                var invoices = await _context.Invoices
                    .Include(i => i.Reservation)
                        .ThenInclude(r => r.User)
                    .Include(i => i.Reservation.RoomType)
                    .Include(i => i.Reservation.Room)
                    .Include(i => i.Payments)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return invoices.Select(i => MapToInvoiceDto(i));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all invoices");
                throw;
            }
        }

        public async Task<InvoiceDto> GetInvoiceByIdAsync(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Reservation)
                        .ThenInclude(r => r.User)
                    .Include(i => i.Reservation.RoomType)
                    .Include(i => i.Reservation.Room)
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                    return null;

                return MapToInvoiceDto(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice with ID {InvoiceId}", id);
                throw;
            }
        }

        public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createInvoiceDto)
        {
            try
            {
                // Validate reservation exists
                var reservation = await _context.Reservations
                    .Include(r => r.User)
                    .Include(r => r.RoomType)
                    .Include(r => r.Room)
                    .FirstOrDefaultAsync(r => r.Id == createInvoiceDto.ReservationId);

                if (reservation == null)
                {
                    throw new KeyNotFoundException($"Reservation with ID {createInvoiceDto.ReservationId} not found");
                }

                // Generate invoice number
                var invoiceNumber = await GenerateInvoiceNumberAsync();

                // Calculate tax amount
                var taxAmount = Math.Round(createInvoiceDto.Amount * (createInvoiceDto.TaxPercentage / 100), 2);
                var totalAmount = createInvoiceDto.Amount + taxAmount;

                // Create invoice entity
                var invoice = new Invoice
                {
                    InvoiceNumber = invoiceNumber,
                    ReservationId = createInvoiceDto.ReservationId,
                    Amount = createInvoiceDto.Amount,
                    Tax = taxAmount,
                    Total = totalAmount,
                    CreatedAt = DateTime.UtcNow,
                    PaidAt = null,
                    IsPaid = false, // Initialize as unpaid
                    //Notes = createInvoiceDto.Notes,
                    //DueDate = createInvoiceDto.DueDate, // Keep DueDate for reference
                    Payments = new List<Payment>()
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                // Return the created invoice
                return await GetInvoiceByIdAsync(invoice.Id);
            }
            catch (Exception ex) when (!(ex is KeyNotFoundException))
            {
                _logger.LogError(ex, "Error creating invoice for reservation ID {ReservationId}", createInvoiceDto.ReservationId);
                throw;
            }
        }

        public async Task<InvoiceDto> UpdateInvoiceAsync(int id, UpdateInvoiceDto updateInvoiceDto)
        {
            try
            {
                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                {
                    throw new KeyNotFoundException($"Invoice with ID {id} not found");
                }

                // Check if invoice is already paid
                if (invoice.IsPaid)
                {
                    throw new InvalidOperationException("Cannot update a paid invoice");
                }

                // Update invoice properties if provided in DTO
                if (updateInvoiceDto.Amount.HasValue)
                {
                    invoice.Amount = updateInvoiceDto.Amount.Value;

                    // Recalculate tax if tax percentage is also provided
                    if (updateInvoiceDto.TaxPercentage.HasValue)
                    {
                        invoice.Tax = Math.Round(invoice.Amount * (updateInvoiceDto.TaxPercentage.Value / 100), 2);
                    }

                    // Always recalculate total when amount changes
                    invoice.Total = invoice.Amount + invoice.Tax;
                }
                else if (updateInvoiceDto.TaxPercentage.HasValue)
                {
                    // Only tax percentage changed
                    invoice.Tax = Math.Round(invoice.Amount * (updateInvoiceDto.TaxPercentage.Value / 100), 2);
                    invoice.Total = invoice.Amount + invoice.Tax;
                }

                //// Update other properties
                //if (updateInvoiceDto.Notes != null)
                //{
                //    invoice.Notes = updateInvoiceDto.Notes;
                //}

                //if (updateInvoiceDto.DueDate.HasValue)
                //{
                //    invoice.DueDate = updateInvoiceDto.DueDate;
                //}

                // Update paid status if provided
                if (updateInvoiceDto.PaidAt.HasValue)
                {
                    invoice.PaidAt = updateInvoiceDto.PaidAt;
                    invoice.IsPaid = true; // Set IsPaid to true if PaidAt is provided
                }

                await _context.SaveChangesAsync();

                return await GetInvoiceByIdAsync(invoice.Id);
            }
            catch (Exception ex) when (!(ex is KeyNotFoundException) && !(ex is InvalidOperationException))
            {
                _logger.LogError(ex, "Error updating invoice with ID {InvoiceId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                {
                    return false;
                }

                // Check if invoice has payments
                if (invoice.Payments.Any())
                {
                    throw new InvalidOperationException("Cannot delete an invoice with associated payments");
                }

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                _logger.LogError(ex, "Error deleting invoice with ID {InvoiceId}", id);
                throw;
            }
        }

        #endregion

        #region Specialized Operations

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByReservationAsync(int reservationId)
        {
            try
            {
                var invoices = await _context.Invoices
                    .Include(i => i.Reservation)
                        .ThenInclude(r => r.User)
                    .Include(i => i.Reservation.RoomType)
                    .Include(i => i.Reservation.Room)
                    .Include(i => i.Payments)
                    .Where(i => i.ReservationId == reservationId)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return invoices.Select(i => MapToInvoiceDto(i));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices for reservation ID {ReservationId}", reservationId);
                throw;
            }
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByUserAsync(int userId)
        {
            try
            {
                var invoices = await _context.Invoices
                    .Include(i => i.Reservation)
                        .ThenInclude(r => r.User)
                    .Include(i => i.Reservation.RoomType)
                    .Include(i => i.Reservation.Room)
                    .Include(i => i.Payments)
                    .Where(i => i.Reservation.UserId == userId)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return invoices.Select(i => MapToInvoiceDto(i));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices for user ID {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByStatusAsync(string status)
        {
            try
            {
                if (!Enum.TryParse<PaymentStatus>(status, true, out var paymentStatus))
                {
                    throw new InvalidOperationException($"Invalid payment status: {status}");
                }

                var invoices = await _context.Invoices
                    .Include(i => i.Reservation)
                        .ThenInclude(r => r.User)
                    .Include(i => i.Reservation.RoomType)
                    .Include(i => i.Reservation.Room)
                    .Include(i => i.Payments)
                    .ToListAsync();

                // Filter by status (which is a calculated property)
                var filteredInvoices = invoices.Where(i => i.Status == paymentStatus).ToList();

                return filteredInvoices.Select(i => MapToInvoiceDto(i));
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                _logger.LogError(ex, "Error retrieving invoices with status {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByDateRangeAsync(DateTime start, DateTime end)
        {
            try
            {
                // Ensure end date includes the whole day
                end = end.Date.AddDays(1).AddTicks(-1);

                var invoices = await _context.Invoices
                    .Include(i => i.Reservation)
                        .ThenInclude(r => r.User)
                    .Include(i => i.Reservation.RoomType)
                    .Include(i => i.Reservation.Room)
                    .Include(i => i.Payments)
                    .Where(i => i.CreatedAt >= start && i.CreatedAt <= end)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return invoices.Select(i => MapToInvoiceDto(i));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices between {StartDate} and {EndDate}", start, end);
                throw;
            }
        }

        public async Task<InvoiceDto> GetInvoiceByNumberAsync(string invoiceNumber)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Reservation)
                        .ThenInclude(r => r.User)
                    .Include(i => i.Reservation.RoomType)
                    .Include(i => i.Reservation.Room)
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);

                if (invoice == null)
                    return null;

                return MapToInvoiceDto(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice with number {InvoiceNumber}", invoiceNumber);
                throw;
            }
        }

        public async Task<string> GenerateInvoiceNumberAsync()
        {
            try
            {
                // Format: INV-YYYYMMDD-XXXX where XXXX is a sequential number
                var today = DateTime.UtcNow.ToString("yyyyMMdd");
                var prefix = $"INV-{today}-";

                // Find the highest invoice number with today's prefix
                var lastInvoice = await _context.Invoices
                    .Where(i => i.InvoiceNumber.StartsWith(prefix))
                    .OrderByDescending(i => i.InvoiceNumber)
                    .FirstOrDefaultAsync();

                int sequentialNumber = 1;

                if (lastInvoice != null)
                {
                    // Extract the sequential part and increment
                    var lastSequentialPart = lastInvoice.InvoiceNumber.Substring(prefix.Length);
                    if (int.TryParse(lastSequentialPart, out int lastSequential))
                    {
                        sequentialNumber = lastSequential + 1;
                    }
                }

                return $"{prefix}{sequentialNumber:D4}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice number");
                throw;
            }
        }

        public async Task<IEnumerable<InvoiceDto>> GetUnpaidInvoicesAsync()
        {
            try
            {
                var invoices = await _context.Invoices
                    .Include(i => i.Reservation)
                        .ThenInclude(r => r.User)
                    .Include(i => i.Reservation.RoomType)
                    .Include(i => i.Reservation.Room)
                    .Include(i => i.Payments)
                    .Where(i => !i.IsPaid) // Use IsPaid instead of checking PaidAt
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return invoices.Select(i => MapToInvoiceDto(i));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unpaid invoices");
                throw;
            }
        }

        public async Task<IEnumerable<InvoiceDto>> GetOverdueInvoicesAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                var invoices = await _context.Invoices
                    .Include(i => i.Reservation)
                        .ThenInclude(r => r.User)
                    .Include(i => i.Reservation.RoomType)
                    .Include(i => i.Reservation.Room)
                    .Include(i => i.Payments)
                    .Where(i => i.DueDate.HasValue && i.DueDate.Value < today && !i.IsPaid) // Use IsPaid instead of checking PaidAt
                    .OrderBy(i => i.DueDate)
                    .ToListAsync();

                return invoices.Select(i => MapToInvoiceDto(i));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overdue invoices");
                throw;
            }
        }

        #endregion

        #region Business Operations

        public async Task<InvoiceDto> GenerateInvoiceFromReservationAsync(int reservationId)
        {
            try
            {
                var reservation = await _context.Reservations
                    .Include(r => r.User)
                    .Include(r => r.RoomType)
                    .Include(r => r.Room)
                    .FirstOrDefaultAsync(r => r.Id == reservationId);

                if (reservation == null)
                {
                    throw new KeyNotFoundException($"Reservation with ID {reservationId} not found");
                }

                // Calculate number of nights
                var nights = (int)(reservation.CheckOutDate - reservation.CheckInDate).TotalDays;

                // Calculate room rate (assuming there's a base rate in RoomType)
                var roomRate = reservation.RoomType?.BasePrice ?? 100; // Default rate if not found

                // Calculate base amount
                var amount = roomRate * nights;

                // Create invoice DTO
                var createInvoiceDto = new CreateInvoiceDto
                {
                    ReservationId = reservationId,
                    Amount = amount,
                    TaxPercentage = 10, // Default tax percentage
                    DueDate = reservation.CheckOutDate,
                    Notes = $"Accommodation charges for reservation {reservation.ReservationNumber}"
                };

                // Call create invoice method
                return await CreateInvoiceAsync(createInvoiceDto);
            }
            catch (Exception ex) when (!(ex is KeyNotFoundException))
            {
                _logger.LogError(ex, "Error generating invoice for reservation ID {ReservationId}", reservationId);
                throw;
            }
        }

        public async Task<bool> FinalizeInvoiceAsync(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                {
                    return false;
                }

                //// Set due date if not already set
                //if (!invoice.DueDate.HasValue)
                //{
                //    invoice.DueDate = DateTime.UtcNow.AddDays(30); // Default 30 days payment term
                //}

                // Check if invoice is fully paid
                var payments = await _context.Payments
                    .Where(p => p.InvoiceId == id && !p.IsRefunded)
                    .ToListAsync();

                decimal totalPaid = payments.Sum(p => p.AmountPaid);
                if (totalPaid >= invoice.Total)
                {
                    invoice.IsPaid = true;
                    invoice.PaidAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing invoice with ID {InvoiceId}", id);
                throw;
            }
        }

        public async Task<bool> SendInvoiceAsync(int id, string email)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Reservation)
                        .ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                {
                    return false;
                }

                // In a real application, you would:
                // 1. Generate PDF invoice
                // 2. Send email with the PDF as attachment

                // For this example, we just log it
                _logger.LogInformation("Invoice {InvoiceNumber} sent to {Email}", invoice.InvoiceNumber, email);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invoice with ID {InvoiceId}", id);
                throw;
            }
        }

        public async Task<InvoiceStatisticsDto> GetInvoiceStatisticsAsync()
        {
            try
            {
                // Get all invoices for stats calculation
                var allInvoices = await _context.Invoices
                    .Include(i => i.Payments)
                    .ToListAsync();

                var totalInvoiceCount = allInvoices.Count;
                var paidInvoiceCount = allInvoices.Count(i => i.IsPaid);
                var pendingInvoiceCount = allInvoices.Count(i => !i.IsPaid && i.Status == PaymentStatus.Pending);
                var partiallyPaidInvoiceCount = allInvoices.Count(i => !i.IsPaid && i.Status == PaymentStatus.PartiallyPaid);

                var totalAmount = allInvoices.Sum(i => i.Total);
                var paidAmount = allInvoices.Where(i => i.IsPaid).Sum(i => i.Total);
                var pendingAmount = totalAmount - allInvoices.SelectMany(i => i.Payments)
                    .Where(p => !p.IsRefunded)
                    .Sum(p => p.AmountPaid);

                // Calculate overdue invoices
                var today = DateTime.UtcNow.Date;
                var overdueInvoices = allInvoices.Where(i =>
                    !i.IsPaid).ToList();

                var overdueInvoiceCount = overdueInvoices.Count;
                var overdueAmount = overdueInvoices.Sum(i =>
                    i.Total - i.Payments
                        .Where(p => !p.IsRefunded)
                        .Sum(p => p.AmountPaid));

                // Last 30 days stats
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                var last30DaysInvoices = allInvoices.Where(i => i.CreatedAt >= thirtyDaysAgo).ToList();
                var last30DaysTotal = last30DaysInvoices.Sum(i => i.Total);
                var last30DaysPaid = last30DaysInvoices.SelectMany(i => i.Payments)
                    .Where(p => p.PaymentDate >= thirtyDaysAgo && !p.IsRefunded)
                    .Sum(p => p.AmountPaid);

                // Create statistics DTO
                return new InvoiceStatisticsDto
                {
                    TotalInvoiceCount = totalInvoiceCount,
                    PaidInvoiceCount = paidInvoiceCount,
                    PendingInvoiceCount = pendingInvoiceCount,
                    PartiallyPaidInvoiceCount = partiallyPaidInvoiceCount,
                    OverdueInvoiceCount = overdueInvoiceCount,

                    TotalAmount = totalAmount,
                    PaidAmount = paidAmount,
                    PendingAmount = pendingAmount,
                    OverdueAmount = overdueAmount,

                    Last30DaysTotal = last30DaysTotal,
                    Last30DaysPaid = last30DaysPaid
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice statistics");
                throw;
            }
        }

        public async Task<decimal> CalculateTaxAsync(decimal amount, decimal taxRate = 0.1m)
        {
            // Simple tax calculation method
            return await Task.FromResult(Math.Round(amount * taxRate, 2));
        }

        #endregion

        #region Helper Methods

        private InvoiceDto MapToInvoiceDto(Invoice invoice)
        {
            if (invoice == null)
                return null;

            var reservation = invoice.Reservation;
            var user = reservation?.User;

            // Calculate total amount paid through payments
            decimal amountPaid = invoice.Payments?
                .Where(p => !p.IsRefunded)
                .Sum(p => p.AmountPaid) ?? 0;

            // Calculate number of nights for the stay
            int numberOfNights = 0;
            if (reservation != null)
            {
                numberOfNights = (int)(reservation.CheckOutDate - reservation.CheckInDate).TotalDays;
            }

            return new InvoiceDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                ReservationId = invoice.ReservationId,
                ReservationNumber = reservation?.ReservationNumber,

                // Guest information
                GuestId = user?.Id ?? 0,
                GuestName = user != null ? $"{user.FirstName} {user.LastName}" : string.Empty,
                GuestEmail = user?.Email,

                // Amount information
                Subtotal = invoice.Amount,
                TaxAmount = invoice.Tax,
                TaxPercentage = invoice.Amount > 0 ? Math.Round(invoice.Tax / invoice.Amount * 100, 2) : 0,
                Total = invoice.Total,
                AmountPaid = amountPaid,

                // Dates
                CreatedAt = invoice.CreatedAt,
                PaidAt = invoice.PaidAt,

                // Status
                Status = invoice.Status.ToString(),
                IsPaid = invoice.IsPaid, // Add IsPaid property to DTO

                //// Notes
                //Notes = invoice.Notes,

                // Stay information
                CheckInDate = reservation?.CheckInDate ?? DateTime.MinValue,
                CheckOutDate = reservation?.CheckOutDate ?? DateTime.MinValue,
                RoomNumber = reservation?.Room?.RoomNumber,
                RoomTypeName = reservation?.RoomType?.Name,
                NumberOfNights = numberOfNights,

                // Add payments
                Payments = invoice.Payments?.Select(p => new PaymentDto
                {
                    Id = p.Id,
                    Amount = p.AmountPaid,
                    PaymentDate = p.PaymentDate,
                    PaymentMethod = p.Method.ToString(),
                    TransactionId = p.TransactionId,
                    ProcessedByName = p.ProcessedByUser != null ?
                        $"{p.ProcessedByUser.FirstName} {p.ProcessedByUser.LastName}" :
                        string.Empty,
                    Notes = p.Notes,
                    IsRefunded = p.IsRefunded,
                    RefundReason = p.RefundReason
                }).ToList() ?? new List<PaymentDto>()
            };
        }

        public async Task<Reservation> GetReservationByIdAsync(int reservationId)
        {
            return await _context.Reservations
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == reservationId);
        }

        #endregion
    }

    // DTOs for Invoice Service
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
        public bool IsPaid { get; set; } // Added for IsPaid property in Invoice model

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

    public class InvoiceStatisticsDto
    {
        public int TotalInvoiceCount { get; set; }
        public int PaidInvoiceCount { get; set; }
        public int PendingInvoiceCount { get; set; }
        public int PartiallyPaidInvoiceCount { get; set; }
        public int OverdueInvoiceCount { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal OverdueAmount { get; set; }

        public decimal Last30DaysTotal { get; set; }
        public decimal Last30DaysPaid { get; set; }
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
        public bool IsRefunded { get; set; }
        public string RefundReason { get; set; }
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

    // DTO used by the controller
    public class SendInvoiceDto
    {
        public string Email { get; set; }
    }

}


