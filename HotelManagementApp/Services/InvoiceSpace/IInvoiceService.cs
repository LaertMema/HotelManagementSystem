namespace HotelManagementApp.Services.InvoiceSpace
{
    using HotelManagementApp.Models;
    using HotelManagementApp.Models.DTOs;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IInvoiceService
    {
        // CRUD operations
        Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync();
        Task<InvoiceDto> GetInvoiceByIdAsync(int id);
        Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createInvoiceDto);
        Task<InvoiceDto> UpdateInvoiceAsync(int id, UpdateInvoiceDto updateInvoiceDto);
        Task<bool> DeleteInvoiceAsync(int id);

        // Specialized queries
        Task<IEnumerable<InvoiceDto>> GetInvoicesByReservationAsync(int reservationId);
        Task<IEnumerable<InvoiceDto>> GetInvoicesByUserAsync(int userId);
        Task<IEnumerable<InvoiceDto>> GetInvoicesByStatusAsync(string status);
        Task<IEnumerable<InvoiceDto>> GetInvoicesByDateRangeAsync(DateTime start, DateTime end);
        Task<InvoiceDto> GetInvoiceByNumberAsync(string invoiceNumber);
        Task<IEnumerable<InvoiceDto>> GetUnpaidInvoicesAsync();
        Task<IEnumerable<InvoiceDto>> GetOverdueInvoicesAsync();

        // Business operations
        Task<string> GenerateInvoiceNumberAsync();
        Task<InvoiceDto> GenerateInvoiceFromReservationAsync(int reservationId);
        Task<bool> FinalizeInvoiceAsync(int id);
        Task<bool> SendInvoiceAsync(int id, string email);
        Task<InvoiceStatisticsDto> GetInvoiceStatisticsAsync();

        // Helper methods
        Task<Reservation> GetReservationByIdAsync(int reservationId);
        Task<decimal> CalculateTaxAsync(decimal amount, decimal taxRate = 0.1m);
    }
}


