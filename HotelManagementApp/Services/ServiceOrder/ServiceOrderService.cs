namespace HotelManagementApp.Services.ServiceOrder
{
    using global::HotelManagementApp.Models.DTOs.ServiceOrder;
    using global::HotelManagementApp.Models.Enums;
    using global::HotelManagementApp.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
        public class ServiceOrderService : IServiceOrderService
        {
            private readonly AppDbContext _context;
            private readonly ILogger<ServiceOrderService> _logger;

            public ServiceOrderService(AppDbContext context, ILogger<ServiceOrderService> logger)
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            #region CRUD Operations

            public async Task<IEnumerable<ServiceOrderDto>> GetAllServiceOrdersAsync()
            {
                try
                {
                    var serviceOrders = await _context.ServiceOrders
                        .Include(so => so.Service)
                        .Include(so => so.Reservation)
                            .ThenInclude(r => r.Room)
                        .Include(so => so.Reservation.User)
                        .Include(so => so.CompletedBy)
                        .AsNoTracking()
                        .ToListAsync();

                    return serviceOrders.Select(so => MapToServiceOrderDto(so));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving all service orders");
                    throw;
                }
            }

            public async Task<ServiceOrderDto> GetServiceOrderByIdAsync(int id)
            {
                try
                {
                    var serviceOrder = await _context.ServiceOrders
                        .Include(so => so.Service)
                        .Include(so => so.Reservation)
                            .ThenInclude(r => r.Room)
                        .Include(so => so.Reservation.User)
                        .Include(so => so.CompletedBy)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(so => so.Id == id);

                    if (serviceOrder == null)
                    {
                        return null;
                    }

                    return MapToServiceOrderDto(serviceOrder);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving service order with ID {ServiceOrderId}", id);
                    throw;
                }
            }

            public async Task<ServiceOrderDto> CreateServiceOrderAsync(CreateServiceOrderDto serviceOrderDto)
            {
                try
                {
                    // Validate reservation
                    var reservation = await _context.Reservations
                        .Include(r => r.Room)
                        .FirstOrDefaultAsync(r => r.Id == serviceOrderDto.ReservationId);

                    if (reservation == null)
                    {
                        throw new KeyNotFoundException($"Reservation with ID {serviceOrderDto.ReservationId} not found");
                    }

                    // Validate service
                    var service = await _context.Services.FindAsync(serviceOrderDto.ServiceId);
                    if (service == null)
                    {
                        throw new KeyNotFoundException($"Service with ID {serviceOrderDto.ServiceId} not found");
                    }

                    // Create order
                    var serviceOrder = new ServiceOrder
                    {
                        ReservationId = serviceOrderDto.ReservationId,
                        ServiceId = serviceOrderDto.ServiceId,
                        OrderDateTime = DateTime.UtcNow,
                        Quantity = serviceOrderDto.Quantity,
                        Status = ServiceOrderStatus.Pending,
                        SpecialInstructions = serviceOrderDto.SpecialInstructions,
                        ScheduledTime = serviceOrderDto.ScheduledTime,
                        DeliveryLocation = serviceOrderDto.DeliveryLocation ?? reservation.Room?.RoomNumber
                    };

                    _context.ServiceOrders.Add(serviceOrder);
                    await _context.SaveChangesAsync();

                    return await GetServiceOrderByIdAsync(serviceOrder.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating service order for reservation {ReservationId} and service {ServiceId}",
                        serviceOrderDto.ReservationId, serviceOrderDto.ServiceId);
                    throw;
                }
            }

            public async Task<ServiceOrderDto> UpdateServiceOrderAsync(int id, UpdateServiceOrderDto serviceOrderDto)
            {
                try
                {
                    var serviceOrder = await _context.ServiceOrders.FindAsync(id);
                    if (serviceOrder == null)
                    {
                        throw new KeyNotFoundException($"Service order with ID {id} not found");
                    }

                    // Update properties if provided
                    if (serviceOrderDto.Quantity.HasValue)
                    {
                        serviceOrder.Quantity = serviceOrderDto.Quantity.Value;
                    }

                    if (serviceOrderDto.Status.HasValue)
                    {
                        serviceOrder.Status = serviceOrderDto.Status.Value;

                        // If changing to completed and not already completed
                        if (serviceOrderDto.Status == ServiceOrderStatus.Completed && serviceOrder.CompletedAt == null)
                        {
                            serviceOrder.CompletedAt = DateTime.UtcNow;
                        }
                    }

                    if (!string.IsNullOrEmpty(serviceOrderDto.SpecialInstructions))
                    {
                        serviceOrder.SpecialInstructions = serviceOrderDto.SpecialInstructions;
                    }

                    if (serviceOrderDto.CompletedById.HasValue)
                    {
                        // Validate employee
                        var employee = await _context.Users.FindAsync(serviceOrderDto.CompletedById.Value);
                        if (employee == null)
                        {
                            throw new KeyNotFoundException($"Employee with ID {serviceOrderDto.CompletedById.Value} not found");
                        }

                        serviceOrder.CompletedById = serviceOrderDto.CompletedById;
                    }

                    if (serviceOrderDto.CompletedAt.HasValue)
                    {
                        serviceOrder.CompletedAt = serviceOrderDto.CompletedAt;
                    }

                    //if (serviceOrderDto.ScheduledTime.HasValue)
                    //{
                    //    serviceOrder.ScheduledTime = serviceOrderDto.ScheduledTime;
                    //}

                    //if (!string.IsNullOrEmpty(serviceOrderDto.DeliveryLocation))
                    //{
                    //    serviceOrder.DeliveryLocation = serviceOrderDto.DeliveryLocation;
                    //}

                    await _context.SaveChangesAsync();
                    return await GetServiceOrderByIdAsync(id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating service order with ID {ServiceOrderId}", id);
                    throw;
                }
            }

            public async Task<bool> DeleteServiceOrderAsync(int id)
            {
                try
                {
                    var serviceOrder = await _context.ServiceOrders.FindAsync(id);
                    if (serviceOrder == null)
                    {
                        return false;
                    }

                    // Don't allow deletion of completed orders
                    if (serviceOrder.Status == ServiceOrderStatus.Completed)
                    {
                        throw new InvalidOperationException("Cannot delete a completed service order");
                    }

                    _context.ServiceOrders.Remove(serviceOrder);
                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting service order with ID {ServiceOrderId}", id);
                    throw;
                }
            }

            #endregion

            #region Specialized Operations

            public async Task<IEnumerable<ServiceOrderDto>> GetServiceOrdersByReservationAsync(int reservationId)
            {
                try
                {
                    var serviceOrders = await _context.ServiceOrders
                        .Include(so => so.Service)
                        .Include(so => so.Reservation)
                            .ThenInclude(r => r.Room)
                        .Include(so => so.Reservation.User)
                        .Include(so => so.CompletedBy)
                        .Where(so => so.ReservationId == reservationId)
                        .AsNoTracking()
                        .ToListAsync();

                    return serviceOrders.Select(so => MapToServiceOrderDto(so));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving service orders for reservation {ReservationId}", reservationId);
                    throw;
                }
            }

            public async Task<IEnumerable<ServiceOrderDto>> GetServiceOrdersByStatusAsync(ServiceOrderStatus status)
            {
                try
                {
                    var serviceOrders = await _context.ServiceOrders
                        .Include(so => so.Service)
                        .Include(so => so.Reservation)
                            .ThenInclude(r => r.Room)
                        .Include(so => so.Reservation.User)
                        .Include(so => so.CompletedBy)
                        .Where(so => so.Status == status)
                        .AsNoTracking()
                        .ToListAsync();

                    return serviceOrders.Select(so => MapToServiceOrderDto(so));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving service orders with status {Status}", status);
                    throw;
                }
            }

            public async Task<IEnumerable<ServiceOrderDto>> GetServiceOrdersByDateRangeAsync(DateTime start, DateTime end)
            {
                try
                {
                    var serviceOrders = await _context.ServiceOrders
                        .Include(so => so.Service)
                        .Include(so => so.Reservation)
                            .ThenInclude(r => r.Room)
                        .Include(so => so.Reservation.User)
                        .Include(so => so.CompletedBy)
                        .Where(so => so.OrderDateTime >= start && so.OrderDateTime <= end)
                        .AsNoTracking()
                        .ToListAsync();

                    return serviceOrders.Select(so => MapToServiceOrderDto(so));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving service orders for date range {StartDate} to {EndDate}", start, end);
                    throw;
                }
            }

            #endregion

            #region Business Operations

            public async Task<bool> CompleteServiceOrderAsync(int id, string notes, int completedById)
            {
                try
                {
                    var serviceOrder = await _context.ServiceOrders.FindAsync(id);
                    if (serviceOrder == null)
                    {
                        throw new KeyNotFoundException($"Service order with ID {id} not found");
                    }

                    // Validate employee
                    var employee = await _context.Users.FindAsync(completedById);
                    if (employee == null)
                    {
                        throw new KeyNotFoundException($"Employee with ID {completedById} not found");
                    }

                    // Update service order
                    serviceOrder.Status = ServiceOrderStatus.Completed;
                    serviceOrder.CompletedById = completedById;
                    serviceOrder.CompletedAt = DateTime.UtcNow;
                    serviceOrder.CompletionNotes = notes;

                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error completing service order with ID {ServiceOrderId}", id);
                    throw;
                }
            }

            public async Task<bool> CancelServiceOrderAsync(int id, string reason)
            {
                try
                {
                    var serviceOrder = await _context.ServiceOrders.FindAsync(id);
                    if (serviceOrder == null)
                    {
                        throw new KeyNotFoundException($"Service order with ID {id} not found");
                    }

                    // Don't allow cancellation of completed orders
                    if (serviceOrder.Status == ServiceOrderStatus.Completed)
                    {
                        throw new InvalidOperationException("Cannot cancel a completed service order");
                    }

                    serviceOrder.Status = ServiceOrderStatus.Cancelled;
                    serviceOrder.CompletionNotes = $"Cancelled: {reason}";

                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cancelling service order with ID {ServiceOrderId}", id);
                    throw;
                }
            }

            public async Task<ServiceOrderStatisticsDto> GetServiceOrderStatsAsync()
            {
                try
                {
                    var stats = new ServiceOrderStatisticsDto();

                    // Count orders by status
                    stats.TotalOrders = await _context.ServiceOrders.CountAsync();
                    stats.PendingOrders = await _context.ServiceOrders.CountAsync(so => so.Status == ServiceOrderStatus.Pending);
                    stats.CompletedOrders = await _context.ServiceOrders.CountAsync(so => so.Status == ServiceOrderStatus.Completed);
                    stats.CancelledOrders = await _context.ServiceOrders.CountAsync(so => so.Status == ServiceOrderStatus.Cancelled);
                    //Injoroje redundant code
                    //stats.InProgressOrders = await _context.ServiceOrders.CountAsync(so => so.Status == ServiceOrderStatus.InProgress;

                    // Calculate total revenue (from completed orders)
                    var completedOrders = await _context.ServiceOrders
                        .Include(so => so.Service)
                        .Where(so => so.Status == ServiceOrderStatus.Completed)
                        .ToListAsync();

                    stats.TotalRevenue = completedOrders.Sum(so => so.Service.Price * so.Quantity);

                    // Group by service type
                    var serviceTypes = await _context.Services
                        .GroupBy(s => s.ServiceType)
                        .Select(g => g.Key)
                        .ToListAsync();

                    stats.OrdersByServiceType = new Dictionary<string, int>();
                    stats.RevenueByServiceType = new Dictionary<string, decimal>();

                    foreach (var serviceType in serviceTypes)
                    {
                        var ordersOfType = await _context.ServiceOrders
                            .Include(so => so.Service)
                            .Where(so => so.Service.ServiceType == serviceType)
                            .ToListAsync();

                        stats.OrdersByServiceType[serviceType] = ordersOfType.Count;
                        stats.RevenueByServiceType[serviceType] = ordersOfType
                            .Where(so => so.Status == ServiceOrderStatus.Completed)
                            .Sum(so => so.Service.Price * so.Quantity);
                    }

                    // Calculate average completion time (in minutes)
                    var completedOrdersWithTime = await _context.ServiceOrders
                        .Where(so => so.Status == ServiceOrderStatus.Completed && so.CompletedAt.HasValue)
                        .Select(so => new
                        {
                            CompletionTimeMinutes = (so.CompletedAt.Value - so.OrderDateTime).TotalMinutes
                        })
                        .ToListAsync();

                    stats.AverageCompletionTime = completedOrdersWithTime.Any()
                        ? completedOrdersWithTime.Average(o => o.CompletionTimeMinutes)
                        : 0;

                    return stats;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving service order statistics");
                    throw;
                }
            }

            public async Task<Dictionary<string, int>> GetServiceOrderStatsByTypeAsync()
            {
                try
                {
                    return await _context.ServiceOrders
                        .Include(so => so.Service)
                        .GroupBy(so => so.Service.ServiceType)
                        .Select(g => new { ServiceType = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(g => g.ServiceType, g => g.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving service order statistics by type");
                    throw;
                }
            }

            #endregion

            #region Helper Methods

            private ServiceOrderDto MapToServiceOrderDto(ServiceOrder serviceOrder)
            {
                if (serviceOrder == null)
                    return null;

                var elapsedTime = DateTime.UtcNow - serviceOrder.OrderDateTime;

                return new ServiceOrderDto
                {
                    Id = serviceOrder.Id,
                    ReservationId = serviceOrder.ReservationId,
                    ReservationNumber = serviceOrder.Reservation?.ReservationNumber,
                    ServiceId = serviceOrder.ServiceId,
                    ServiceName = serviceOrder.Service?.ServiceName,
                    ServiceDescription = serviceOrder.Service?.Description,
                    ServiceCategory = serviceOrder.Service?.ServiceType,
                    OrderDateTime = serviceOrder.OrderDateTime,
                    Quantity = serviceOrder.Quantity,
                    PricePerUnit = serviceOrder.Service?.Price ?? 0,
                    TotalPrice = (serviceOrder.Service?.Price ?? 0) * serviceOrder.Quantity,
                    Status = serviceOrder.Status.ToString(),
                    SpecialInstructions = serviceOrder.SpecialInstructions,
                    DeliveryLocation = serviceOrder.DeliveryLocation,
                    ScheduledTime = serviceOrder.ScheduledTime,

                    // Completion information
                    CompletedById = serviceOrder.CompletedById,
                    CompletedByName = serviceOrder.CompletedBy != null
                        ? $"{serviceOrder.CompletedBy.FirstName} {serviceOrder.CompletedBy.LastName}"
                        : null,
                    CompletedAt = serviceOrder.CompletedAt,
                    CompletionNotes = serviceOrder.CompletionNotes,

                    // Guest information
                    GuestId = serviceOrder.Reservation?.UserId ?? 0,
                    GuestName = serviceOrder.Reservation?.User != null
                        ? $"{serviceOrder.Reservation.User.FirstName} {serviceOrder.Reservation.User.LastName}"
                        : null,
                    RoomNumber = serviceOrder.Reservation?.Room?.RoomNumber,

                    // Elapsed time
                    ElapsedTime = elapsedTime
                };
            }

            private ServiceOrderSummaryDto MapToServiceOrderSummaryDto(ServiceOrder serviceOrder)
            {
                if (serviceOrder == null)
                    return null;

                var elapsedTime = DateTime.UtcNow - serviceOrder.OrderDateTime;

                return new ServiceOrderSummaryDto
                {
                    Id = serviceOrder.Id,
                    ServiceName = serviceOrder.Service?.ServiceName,
                    ReservationNumber = serviceOrder.Reservation?.ReservationNumber,
                    RoomNumber = serviceOrder.Reservation?.Room?.RoomNumber,
                    GuestName = serviceOrder.Reservation?.User != null
                        ? $"{serviceOrder.Reservation.User.FirstName} {serviceOrder.Reservation.User.LastName}"
                        : null,
                    OrderDateTime = serviceOrder.OrderDateTime,
                    Status = serviceOrder.Status.ToString(),
                    ScheduledTime = serviceOrder.ScheduledTime,
                    TotalPrice = (serviceOrder.Service?.Price ?? 0) * serviceOrder.Quantity,
                    ElapsedTime = elapsedTime
                };
            }

            #endregion
        }
    

}
