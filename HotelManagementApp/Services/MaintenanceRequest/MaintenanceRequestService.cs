namespace HotelManagementApp.Services.MaintenanceRequest
{
    using global::HotelManagementApp.Models.DTOs.MaintenanceRequest;
    using global::HotelManagementApp.Models.Enums;
    using global::HotelManagementApp.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
        public class MaintenanceRequestService : IMaintenanceRequestService
        {
            private readonly AppDbContext _context;
            private readonly ILogger<MaintenanceRequestService> _logger;

            public MaintenanceRequestService(AppDbContext context, ILogger<MaintenanceRequestService> logger)
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public async Task<MaintenanceRequestDto> CreateMaintenanceRequestAsync(CreateMaintenanceRequestDto dto, int reportedById)
            {
                try
                {
                    // Validate room if provided
                    if (dto.RoomId.HasValue)
                    {
                        var room = await _context.Rooms.FindAsync(dto.RoomId.Value);
                        if (room == null)
                        {
                            throw new KeyNotFoundException($"Room with ID {dto.RoomId.Value} not found");
                        }
                    }

                    // Validate reservation if provided
                    if (dto.ReservationId.HasValue)
                    {
                        var reservation = await _context.Reservations.FindAsync(dto.ReservationId.Value);
                        if (reservation == null)
                        {
                            throw new KeyNotFoundException($"Reservation with ID {dto.ReservationId.Value} not found");
                        }
                    }

                    // Create maintenance request
                    var maintenanceRequest = new MaintenanceRequest
                    {
                        //Title = dto.Title,
                        IssueDescription = dto.IssueDescription,
                        RoomId = dto.RoomId,
                        //ReservationId = dto.ReservationId,
                        ReportDate = DateTime.UtcNow,
                        Status = MaintenanceRequestStatus.Reported,
                        Priority = dto.Priority,
                        //RequestType = dto.RequestType.ToString(),
                        //Location = dto.Location,
                        ReportedBy = reportedById,
                        //RequiresFollowUp = false
                    };

                    _context.MaintenanceRequests.Add(maintenanceRequest);
                    await _context.SaveChangesAsync();

                    // If room is provided, update room status to maintenance if critical
                    if (dto.RoomId.HasValue && dto.Priority == MaintenanceRequestPriority.High)
                    {
                        var room = await _context.Rooms.FindAsync(dto.RoomId.Value);
                        if (room != null && room.Status != RoomStatus.Occupied)
                        {
                            room.Status = RoomStatus.Maintenance;
                            await _context.SaveChangesAsync();
                        }
                    }

                    return await GetMaintenanceRequestByIdAsync(maintenanceRequest.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating maintenance request");
                    throw;
                }
            }

            public async Task<MaintenanceRequestDto> GetMaintenanceRequestByIdAsync(int id)
            {
                try
                {
                    var maintenanceRequest = await _context.MaintenanceRequests
                        .Include(m => m.Room)
                        //.Include(m => m.Reservation)
                        .Include(m => m.ReportedByUser)
                        .Include(m => m.AssignedToUser)
                        .FirstOrDefaultAsync(m => m.Id == id);

                    if (maintenanceRequest == null)
                    {
                        return null;
                    }

                    return MapToMaintenanceRequestDto(maintenanceRequest);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving maintenance request with ID {Id}", id);
                    throw;
                }
            }

            public async Task<IEnumerable<MaintenanceRequestDto>> GetAllMaintenanceRequestsAsync()
            {
                try
                {
                    var maintenanceRequests = await _context.MaintenanceRequests
                        .Include(m => m.Room)
                        //.Include(m => m.Reservation)
                        .Include(m => m.ReportedByUser)
                        .Include(m => m.AssignedToUser)
                        .OrderByDescending(m => m.Priority)
                        .ThenByDescending(m => m.ReportDate)
                        .ToListAsync();

                    return maintenanceRequests.Select(m => MapToMaintenanceRequestDto(m));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving all maintenance requests");
                    throw;
                }
            }

            public async Task<IEnumerable<MaintenanceRequestDto>> GetMaintenanceRequestsByStatusAsync(MaintenanceRequestStatus status)
            {
                try
                {
                    var maintenanceRequests = await _context.MaintenanceRequests
                        .Include(m => m.Room)
                        //.Include(m => m.Reservation)
                        .Include(m => m.ReportedByUser)
                        .Include(m => m.AssignedToUser)
                        .Where(m => m.Status == status)
                        .OrderByDescending(m => m.Priority)
                        .ThenByDescending(m => m.ReportDate)
                        .ToListAsync();

                    return maintenanceRequests.Select(m => MapToMaintenanceRequestDto(m));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving maintenance requests with status {Status}", status);
                    throw;
                }
            }

            public async Task<IEnumerable<MaintenanceRequestDto>> GetMaintenanceRequestsByRoomAsync(int roomId)
            {
                try
                {
                    var maintenanceRequests = await _context.MaintenanceRequests
                        .Include(m => m.Room)
                        //.Include(m => m.Reservation)
                        .Include(m => m.ReportedByUser)
                        .Include(m => m.AssignedToUser)
                        .Where(m => m.RoomId == roomId)
                        .OrderByDescending(m => m.Priority)
                        .ThenByDescending(m => m.ReportDate)
                        .ToListAsync();

                    return maintenanceRequests.Select(m => MapToMaintenanceRequestDto(m));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving maintenance requests for room ID {RoomId}", roomId);
                    throw;
                }
            }

            public async Task<IEnumerable<MaintenanceRequestDto>> GetMaintenanceRequestsAssignedToUserAsync(int userId)
            {
                try
                {
                    var maintenanceRequests = await _context.MaintenanceRequests
                        .Include(m => m.Room)
                        //.Include(m => m.Reservation)
                        .Include(m => m.ReportedByUser)
                        .Include(m => m.AssignedToUser)
                        .Where(m => m.AssignedTo == userId)
                        .OrderByDescending(m => m.Priority)
                        .ThenByDescending(m => m.ReportDate)
                        .ToListAsync();

                    return maintenanceRequests.Select(m => MapToMaintenanceRequestDto(m));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving maintenance requests assigned to user ID {UserId}", userId);
                    throw;
                }
            }

            public async Task<MaintenanceRequestDto> UpdateMaintenanceRequestAsync(int id, UpdateMaintenanceRequestDto dto)
            {
                try
                {
                    var maintenanceRequest = await _context.MaintenanceRequests.FindAsync(id);
                    if (maintenanceRequest == null)
                    {
                        throw new KeyNotFoundException($"Maintenance request with ID {id} not found");
                    }

                    // Update fields if provided
                    //if (!string.IsNullOrEmpty(dto.Title))
                    //{
                    //    maintenanceRequest.Title = dto.Title;
                    //}

                    if (!string.IsNullOrEmpty(dto.IssueDescription))
                    {
                        maintenanceRequest.IssueDescription = dto.IssueDescription;
                    }

                    if (dto.Status.HasValue)
                    {
                        // If completing the request, set completed date
                        if (dto.Status.Value == MaintenanceRequestStatus.Resolved &&
                            maintenanceRequest.Status != MaintenanceRequestStatus.Resolved)
                        {
                            maintenanceRequest.CompletedAt = DateTime.UtcNow;

                            // If room was in maintenance, set back to available
                            if (maintenanceRequest.RoomId.HasValue)
                            {
                                var room = await _context.Rooms.FindAsync(maintenanceRequest.RoomId.Value);
                                if (room != null && room.Status == RoomStatus.Maintenance)
                                {
                                    room.Status = RoomStatus.Available;
                                }
                            }
                        }
                        maintenanceRequest.Status = dto.Status.Value;
                    }

                    if (dto.Priority.HasValue)
                    {
                        maintenanceRequest.Priority = dto.Priority.Value;
                    }

                    if (dto.AssignedTo.HasValue)
                    {
                        var assignedTo = await _context.Users.FindAsync(dto.AssignedTo.Value);
                        if (assignedTo == null)
                        {
                            throw new KeyNotFoundException($"User with ID {dto.AssignedTo.Value} not found");
                        }

                        // If this is a new assignment
                        if (maintenanceRequest.AssignedTo != dto.AssignedTo.Value)
                        {
                            maintenanceRequest.AssignedTo = dto.AssignedTo.Value;
                            //maintenanceRequest.AssignedAt = DateTime.UtcNow;
                        }
                    }

                    if (!string.IsNullOrEmpty(dto.ResolutionNotes))
                    {
                        maintenanceRequest.ResolutionNotes = dto.ResolutionNotes;
                    }

                    if (dto.CompletedAt.HasValue)
                    {
                        maintenanceRequest.CompletedAt = dto.CompletedAt.Value;
                    }

                    //if (!string.IsNullOrEmpty(dto.Location))
                    //{
                    //    maintenanceRequest.Location = dto.Location;
                    //}

                    //if (dto.CostOfRepair.HasValue)
                    //{
                    //    maintenanceRequest.CostOfRepair = dto.CostOfRepair.Value;
                    //}

                    //if (dto.RequiresFollowUp.HasValue)
                    //{
                    //    maintenanceRequest.RequiresFollowUp = dto.RequiresFollowUp.Value;
                    //}

                    await _context.SaveChangesAsync();
                    return await GetMaintenanceRequestByIdAsync(id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating maintenance request with ID {Id}", id);
                    throw;
                }
            }

            public async Task<bool> DeleteMaintenanceRequestAsync(int id)
            {
                try
                {
                    var maintenanceRequest = await _context.MaintenanceRequests.FindAsync(id);
                    if (maintenanceRequest == null)
                    {
                        return false;
                    }

                    _context.MaintenanceRequests.Remove(maintenanceRequest);
                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting maintenance request with ID {Id}", id);
                    throw;
                }
            }

            public async Task<bool> AssignMaintenanceRequestAsync(int id, int assignedToId)
            {
                try
                {
                    var maintenanceRequest = await _context.MaintenanceRequests.FindAsync(id);
                    if (maintenanceRequest == null)
                    {
                        throw new KeyNotFoundException($"Maintenance request with ID {id} not found");
                    }

                    var assignedTo = await _context.Users.FindAsync(assignedToId);
                    if (assignedTo == null)
                    {
                        throw new KeyNotFoundException($"User with ID {assignedToId} not found");
                    }

                    maintenanceRequest.AssignedTo = assignedToId;
                    //maintenanceRequest.AssignedAt = DateTime.UtcNow;
                    maintenanceRequest.Status = MaintenanceRequestStatus.InProgress;

                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error assigning maintenance request with ID {Id} to user ID {UserId}", id, assignedToId);
                    throw;
                }
            }

            public async Task<bool> CompleteMaintenanceRequestAsync(int id, string resolutionNotes, decimal? costOfRepair = null)
            {
                try
                {
                    var maintenanceRequest = await _context.MaintenanceRequests.FindAsync(id);
                    if (maintenanceRequest == null)
                    {
                        throw new KeyNotFoundException($"Maintenance request with ID {id} not found");
                    }

                    maintenanceRequest.Status = MaintenanceRequestStatus.Resolved;
                    maintenanceRequest.ResolutionNotes = resolutionNotes;
                    maintenanceRequest.CompletedAt = DateTime.UtcNow;

                    //if (costOfRepair.HasValue)
                    //{
                    //    maintenanceRequest.CostOfRepair = costOfRepair.Value;
                    //}

                    // Update room status if needed
                    if (maintenanceRequest.RoomId.HasValue)
                    {
                        var room = await _context.Rooms.FindAsync(maintenanceRequest.RoomId.Value);
                        if (room != null && room.Status == RoomStatus.Maintenance)
                        {
                            room.Status = RoomStatus.Available;
                        }
                    }

                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error completing maintenance request with ID {Id}", id);
                    throw;
                }
            }

            #region Helper Methods

            private MaintenanceRequestDto MapToMaintenanceRequestDto(MaintenanceRequest maintenanceRequest)
            {
                if (maintenanceRequest == null)
                    return null;

                var timeToResolve = maintenanceRequest.CompletedAt.HasValue
                    ? maintenanceRequest.CompletedAt.Value - maintenanceRequest.ReportDate
                    : (TimeSpan?)null;

                return new MaintenanceRequestDto
                {
                    Id = maintenanceRequest.Id,
                    //Title = maintenanceRequest.Title,
                    IssueDescription = maintenanceRequest.IssueDescription,
                    ReportDate = maintenanceRequest.ReportDate,
                    Status = maintenanceRequest.Status.ToString(),
                    Priority = maintenanceRequest.Priority.ToString(),
                    //RequestType = maintenanceRequest.RequestType,
                    //Location = maintenanceRequest.Location,

                    RoomId = maintenanceRequest.RoomId,
                    RoomNumber = maintenanceRequest.Room?.RoomNumber,
                    Floor = maintenanceRequest.Room?.Floor,

                    //ReservationId = maintenanceRequest.ReservationId,
                    //ReservationNumber = maintenanceRequest.Reservation?.ReservationNumber,

                    ReportedBy = maintenanceRequest.ReportedBy,
                    ReportedByName = maintenanceRequest.ReportedByUser != null
                        ? $"{maintenanceRequest.ReportedByUser.FirstName} {maintenanceRequest.ReportedByUser.LastName}"
                        : string.Empty,

                    AssignedTo = maintenanceRequest.AssignedTo,
                    AssignedToName = maintenanceRequest.AssignedToUser != null
                        ? $"{maintenanceRequest.AssignedToUser.FirstName} {maintenanceRequest.AssignedToUser.LastName}"
                        : string.Empty,
                    //AssignedAt = maintenanceRequest.AssignedAt,

                    ResolutionNotes = maintenanceRequest.ResolutionNotes,
                    CompletedAt = maintenanceRequest.CompletedAt,
                    //CostOfRepair = maintenanceRequest.CostOfRepair,
                    //RequiresFollowUp = maintenanceRequest.RequiresFollowUp,
                    TimeToResolve = timeToResolve
                };
            }

            #endregion
        }
    

}
