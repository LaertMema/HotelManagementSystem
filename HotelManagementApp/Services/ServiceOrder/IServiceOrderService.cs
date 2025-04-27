namespace HotelManagementApp.Services.ServiceOrder
{
    using global::HotelManagementApp.Models.DTOs.ServiceOrder;
    using global::HotelManagementApp.Models.Enums;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

        public interface IServiceOrderService
        {
            // CRUD operations
            Task<IEnumerable<ServiceOrderDto>> GetAllServiceOrdersAsync();
            Task<ServiceOrderDto> GetServiceOrderByIdAsync(int id);
            Task<ServiceOrderDto> CreateServiceOrderAsync(CreateServiceOrderDto serviceOrder);
            Task<ServiceOrderDto> UpdateServiceOrderAsync(int id, UpdateServiceOrderDto serviceOrder);
            Task<bool> DeleteServiceOrderAsync(int id);

            // Specialized operations
            Task<IEnumerable<ServiceOrderDto>> GetServiceOrdersByReservationAsync(int reservationId);
            Task<IEnumerable<ServiceOrderDto>> GetServiceOrdersByStatusAsync(ServiceOrderStatus status);
            Task<IEnumerable<ServiceOrderDto>> GetServiceOrdersByDateRangeAsync(DateTime start, DateTime end);

            // Business operations
            Task<bool> CompleteServiceOrderAsync(int id, string notes, int completedById);
            Task<bool> CancelServiceOrderAsync(int id, string reason);
            Task<ServiceOrderStatisticsDto> GetServiceOrderStatsAsync();
            Task<Dictionary<string, int>> GetServiceOrderStatsByTypeAsync();
        }
    

}
