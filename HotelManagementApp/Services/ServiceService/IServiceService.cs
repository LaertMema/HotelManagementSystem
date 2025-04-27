namespace HotelManagementApp.Services.ServiceService
{
    using HotelManagementApp.Models;
    using HotelManagementApp.Models.DTOs.Service;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IServiceService
    {
        // CRUD operations
        Task<IEnumerable<ServiceDto>> GetAllServicesAsync();
        Task<ServiceDto> GetServiceByIdAsync(int id);
        Task<ServiceDto> CreateServiceAsync(CreateServiceDto serviceDto);
        Task<ServiceDto> UpdateServiceAsync(int id, UpdateServiceDto serviceDto);
        Task<bool> DeleteServiceAsync(int id);

        // Specialized operations
        Task<IEnumerable<ServiceDto>> GetServicesByTypeAsync(string serviceType);
        Task<IEnumerable<ServiceDto>> GetActiveServicesAsync();
        Task<Dictionary<string, int>> GetServiceStatsByTypeAsync();
    }
}

