namespace HotelManagementApp.Services.ServiceService
{
    using HotelManagementApp.Models;
    using HotelManagementApp.Models.DTOs.Service;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class ServiceService : IServiceService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ServiceService> _logger;

        public ServiceService(AppDbContext context, ILogger<ServiceService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region CRUD Operations

        public async Task<IEnumerable<ServiceDto>> GetAllServicesAsync()
        {
            try
            {
                var services = await _context.Services
                    .Include(s => s.ServiceOrders)
                    .AsNoTracking()
                    .ToListAsync();

                return services.Select(s => MapToServiceDto(s));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all services");
                throw;
            }
        }

        public async Task<ServiceDto> GetServiceByIdAsync(int id)
        {
            try
            {
                var service = await _context.Services
                    .Include(s => s.ServiceOrders)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (service == null)
                {
                    return null;
                }

                return MapToServiceDto(service);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service with ID {ServiceId}", id);
                throw;
            }
        }

        public async Task<ServiceDto> CreateServiceAsync(CreateServiceDto serviceDto)
        {
            try
            {
                // Create new service
                var service = new Service
                {
                    ServiceName = serviceDto.ServiceName,
                    Description = serviceDto.Description,
                    Price = serviceDto.Price,
                    ServiceType = serviceDto.ServiceType,
                    IsActive = serviceDto.IsActive
                };

                _context.Services.Add(service);
                await _context.SaveChangesAsync();

                return await GetServiceByIdAsync(service.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service {ServiceName}", serviceDto.ServiceName);
                throw;
            }
        }

        public async Task<ServiceDto> UpdateServiceAsync(int id, UpdateServiceDto serviceDto)
        {
            try
            {
                var service = await _context.Services.FindAsync(id);
                if (service == null)
                {
                    throw new KeyNotFoundException($"Service with ID {id} not found");
                }

                // Update properties if provided
                if (!string.IsNullOrEmpty(serviceDto.ServiceName))
                {
                    service.ServiceName = serviceDto.ServiceName;
                }

                if (!string.IsNullOrEmpty(serviceDto.Description))
                {
                    service.Description = serviceDto.Description;
                }

                if (serviceDto.Price.HasValue)
                {
                    service.Price = serviceDto.Price.Value;
                }

                if (!string.IsNullOrEmpty(serviceDto.ServiceType))
                {
                    service.ServiceType = serviceDto.ServiceType;
                }

                if (serviceDto.IsActive.HasValue)
                {
                    service.IsActive = serviceDto.IsActive.Value;
                }

                await _context.SaveChangesAsync();
                return await GetServiceByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service with ID {ServiceId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteServiceAsync(int id)
        {
            try
            {
                var service = await _context.Services
                    .Include(s => s.ServiceOrders)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (service == null)
                {
                    return false;
                }

                // Check if service is used in any orders
                if (service.ServiceOrders != null && service.ServiceOrders.Any())
                {
                    // Consider soft deletion by marking as inactive instead
                    service.IsActive = false;
                    await _context.SaveChangesAsync();
                    return true;
                }

                // Hard delete if no orders reference this service
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service with ID {ServiceId}", id);
                throw;
            }
        }

        #endregion

        #region Specialized Operations

        public async Task<IEnumerable<ServiceDto>> GetServicesByTypeAsync(string serviceType)
        {
            try
            {
                var services = await _context.Services
                    .Include(s => s.ServiceOrders)
                    .Where(s => s.ServiceType == serviceType)
                    .AsNoTracking()
                    .ToListAsync();

                return services.Select(s => MapToServiceDto(s));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving services by type {ServiceType}", serviceType);
                throw;
            }
        }

        public async Task<IEnumerable<ServiceDto>> GetActiveServicesAsync()
        {
            try
            {
                var services = await _context.Services
                    .Include(s => s.ServiceOrders)
                    .Where(s => s.IsActive)
                    .AsNoTracking()
                    .ToListAsync();

                return services.Select(s => MapToServiceDto(s));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active services");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetServiceStatsByTypeAsync()
        {
            try
            {
                return await _context.Services
                    .GroupBy(s => s.ServiceType)
                    .Select(g => new { ServiceType = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.ServiceType, g => g.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service statistics by type");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private ServiceDto MapToServiceDto(Service service)
        {
            if (service == null)
                return null;

            // Calculate statistics if service orders are loaded
            int totalOrders = 0;
            decimal totalRevenue = 0;
            double averageRating = 0;

            if (service.ServiceOrders != null)
            {
                totalOrders = service.ServiceOrders.Count;
                totalRevenue = service.ServiceOrders.Sum(so => so.Quantity * service.Price);

                // Could calculate average rating if we had ratings for service orders
                // For now, leaving as 0
            }

            return new ServiceDto
            {
                Id = service.Id,
                ServiceName = service.ServiceName,
                Description = service.Description,
                Price = service.Price,
                ServiceType = service.ServiceType,
                IsActive = service.IsActive,

                // Statistics
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                AverageRating = averageRating
            };
        }

        private ServiceSummaryDto MapToServiceSummaryDto(Service service)
        {
            if (service == null)
                return null;

            return new ServiceSummaryDto
            {
                Id = service.Id,
                ServiceName = service.ServiceName,
                Price = service.Price,
                ServiceType = service.ServiceType,
                IsActive = service.IsActive
            };
        }

        #endregion
    }
}
