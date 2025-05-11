using HotelManagementApp.Models.DTOs.Room;
using HotelManagementApp.Models.Enums;
using HotelManagementApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace HotelManagementApp.Services.RoomServiceSpace
    {
        public class RoomService /*: IRoomService */
        {
            private readonly AppDbContext _context;
            private readonly ILogger<RoomService> _logger;

            public RoomService(AppDbContext context, ILogger<RoomService> logger)
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            #region CRUD Operations

            public async Task<IEnumerable<RoomDto>> GetAllRoomsAsync()
            {
                try
                {
                    var rooms = await _context.Rooms
                        .Include(r => r.RoomType)
                        .Include(r => r.LastCleanedBy)
                        .AsNoTracking()
                        .ToListAsync();

                    return rooms.Select(r => MapToRoomDto(r));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving all rooms");
                    throw;
                }
            }

            public async Task<RoomDto> GetRoomByIdAsync(int id)
            {
                try
                {
                    var room = await _context.Rooms
                        .Include(r => r.RoomType)
                        .Include(r => r.LastCleanedBy)
                        .Include(r => r.Reservations)
                        .Include(r => r.CleaningTasks)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r => r.Id == id);

                    if (room == null)
                    {
                        return null;
                    }

                    return MapToRoomDto(room);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving room with ID {RoomId}", id);
                    throw;
                }
            }

            public async Task<RoomDto> CreateRoomAsync(CreateRoomDto roomDto)
            {
                try
                {
                    // Check if room with same number already exists
                    if (await _context.Rooms.AnyAsync(r => r.RoomNumber == roomDto.RoomNumber))
                    {
                        throw new InvalidOperationException($"A room with number {roomDto.RoomNumber} already exists");
                    }

                    // Check if the room type exists
                    var roomType = await _context.RoomTypes.FindAsync(roomDto.RoomTypeId);
                    if (roomType == null)
                    {
                        throw new KeyNotFoundException($"Room type with ID {roomDto.RoomTypeId} not found");
                    }

                    // Create a new Room entity
                    var room = new Room
                    {
                        RoomNumber = roomDto.RoomNumber,
                        Floor = roomDto.Floor,
                        RoomTypeId = roomDto.RoomTypeId,
                        RoomType = roomType,
                        BasePrice = roomDto.BasePrice,
                        Status = roomDto.Status,
                        Notes = roomDto.Notes,
                        NeedsCleaning = false
                    };

                    // Add room to database
                    _context.Rooms.Add(room);
                    await _context.SaveChangesAsync();

                    return await GetRoomByIdAsync(room.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating room {@RoomDto}", roomDto);
                    throw;
                }
            }

            public async Task<RoomDto> UpdateRoomAsync(int id, UpdateRoomDto roomDto)
            {
                try
                {
                    var room = await _context.Rooms.FindAsync(id);
                    if (room == null)
                    {
                        throw new KeyNotFoundException($"Room with ID {id} not found");
                    }

                    // Update room number if provided and not duplicate
                    if (!string.IsNullOrEmpty(roomDto.RoomNumber) && roomDto.RoomNumber != room.RoomNumber)
                    {
                        if (await _context.Rooms.AnyAsync(r => r.RoomNumber == roomDto.RoomNumber && r.Id != id))
                        {
                            throw new InvalidOperationException($"A room with number {roomDto.RoomNumber} already exists");
                        }
                        room.RoomNumber = roomDto.RoomNumber;
                    }

                    // Update other properties if provided
                    if (roomDto.Floor.HasValue)
                    {
                        room.Floor = roomDto.Floor.Value;
                    }

                    if (roomDto.RoomTypeId.HasValue)
                    {
                        var roomType = await _context.RoomTypes.FindAsync(roomDto.RoomTypeId.Value);
                        if (roomType == null)
                        {
                            throw new KeyNotFoundException($"Room type with ID {roomDto.RoomTypeId} not found");
                        }
                        room.RoomTypeId = roomDto.RoomTypeId.Value;
                    }

                    if (roomDto.BasePrice.HasValue)
                    {
                        room.BasePrice = roomDto.BasePrice.Value;
                    }

                    if (roomDto.Status.HasValue)
                    {
                        room.Status = roomDto.Status.Value;
                    }

                    if (roomDto.NeedsCleaning.HasValue)
                    {
                        room.NeedsCleaning = roomDto.NeedsCleaning.Value;

                        // If room is marked as not needing cleaning, update last cleaned time
                        if (roomDto.NeedsCleaning.Value == false)
                        {
                            room.LastCleaned = DateTime.UtcNow;
                        }
                    }

                    if (!string.IsNullOrEmpty(roomDto.Notes))
                    {
                        room.Notes = roomDto.Notes;
                    }

                    // Save changes
                    await _context.SaveChangesAsync();

                    return await GetRoomByIdAsync(id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating room with ID {RoomId}", id);
                    throw;
                }
            }

            public async Task<bool> DeleteRoomAsync(int id)
            {
                try
                {
                    var room = await _context.Rooms.FindAsync(id);
                    if (room == null)
                    {
                        return false;
                    }

                    // Check if room has active reservations
                    bool hasActiveReservations = await _context.Reservations
                        .AnyAsync(r => r.RoomId == id &&
                                       (r.Status == ReservationStatus.Confirmed ||
                                        r.Status == ReservationStatus.CheckedIn));

                    if (hasActiveReservations)
                    {
                        throw new InvalidOperationException("Cannot delete room with active reservations");
                    }

                    _context.Rooms.Remove(room);
                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting room with ID {RoomId}", id);
                    throw;
                }
            }

            #endregion

            #region Specialized Operations

            public async Task<IEnumerable<RoomDto>> GetRoomsByStatusAsync(RoomStatus status)
            {
                try
                {
                    var rooms = await _context.Rooms
                        .Include(r => r.RoomType)
                        .Include(r => r.LastCleanedBy)
                        .Where(r => r.Status == status)
                        .AsNoTracking()
                        .ToListAsync();

                    return rooms.Select(r => MapToRoomDto(r));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving rooms with status {Status}", status);
                    throw;
                }
            }

            public async Task<IEnumerable<RoomDto>> GetRoomsByTypeAsync(int roomTypeId)
            {
                try
                {
                    var rooms = await _context.Rooms
                        .Include(r => r.RoomType)
                        .Include(r => r.LastCleanedBy)
                        .Where(r => r.RoomTypeId == roomTypeId)
                        .AsNoTracking()
                        .ToListAsync();

                    return rooms.Select(r => MapToRoomDto(r));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving rooms with room type ID {RoomTypeId}", roomTypeId);
                    throw;
                }
            }

            public async Task<IEnumerable<RoomDto>> GetRoomsByFloorAsync(int floor)
            {
                try
                {
                    var rooms = await _context.Rooms
                        .Include(r => r.RoomType)
                        .Include(r => r.LastCleanedBy)
                        .Where(r => r.Floor == floor)
                        .AsNoTracking()
                        .ToListAsync();

                    return rooms.Select(r => MapToRoomDto(r));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving rooms on floor {Floor}", floor);
                    throw;
                }
            }

            public async Task<IEnumerable<RoomAvailabilityDto>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut)
            {
                try
                {
                    // Get all rooms
                    var rooms = await _context.Rooms
                        .Include(r => r.RoomType)
                        .AsNoTracking()
                        .ToListAsync();

                    var availableRooms = new List<RoomAvailabilityDto>();

                    // Check availability for each room
                    foreach (var room in rooms)
                    {
                        bool isAvailable = await IsRoomAvailableAsync(room.Id, checkIn, checkOut);

                        // Only include rooms not in maintenance
                        if (room.Status != RoomStatus.Maintenance)
                        {
                            DateTime? nextAvailableDate = null;

                            // If not available for the requested dates, find the next available date
                            if (!isAvailable)
                            {
                                nextAvailableDate = await GetNextAvailableDateAsync(room.Id, checkIn);
                            }

                            // Parse amenities string to array
                            string[] amenities = room.RoomType.Amenities?.Split(',') ?? Array.Empty<string>();

                            availableRooms.Add(new RoomAvailabilityDto
                            {
                                Id = room.Id,
                                RoomNumber = room.RoomNumber,
                                RoomTypeName = room.RoomType.Name,
                                BasePrice = room.BasePrice,
                                Capacity = room.RoomType.Capacity,
                                IsAvailable = isAvailable,
                                NextAvailableDate = nextAvailableDate,
                                Amenities = amenities,
                                ImageUrl = room.RoomType.ImageUrl
                            });
                        }
                    }

                    return availableRooms;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving available rooms");
                    throw;
                }
            }

            public async Task<IEnumerable<RoomDto>> GetRoomsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
            {
                try
                {
                    var rooms = await _context.Rooms
                        .Include(r => r.RoomType)
                        .Include(r => r.LastCleanedBy)
                        .Where(r => r.BasePrice >= minPrice && r.BasePrice <= maxPrice)
                        .AsNoTracking()
                        .ToListAsync();

                    return rooms.Select(r => MapToRoomDto(r));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving rooms in price range {MinPrice} to {MaxPrice}", minPrice, maxPrice);
                    throw;
                }
            }

            public async Task<decimal> CalculateRoomPriceAsync(int roomId, DateTime checkIn, DateTime checkOut)
            {
                try
                {
                    var room = await _context.Rooms
                        .Include(r => r.RoomType)
                        .FirstOrDefaultAsync(r => r.Id == roomId);

                    if (room == null)
                    {
                        throw new KeyNotFoundException($"Room with ID {roomId} not found");
                    }

                    var numberOfNights = (checkOut - checkIn).Days;
                    if (numberOfNights <= 0)
                    {
                        throw new ArgumentException("Check-out date must be after check-in date");
                    }

                    // Calculate base price
                    decimal totalPrice = room.BasePrice * numberOfNights;

                    return totalPrice;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating price for room with ID {RoomId}", roomId);
                    throw;
                }
            }

            #endregion

            #region Business Operations

            public async Task<bool> UpdateRoomStatusAsync(int roomId, RoomStatus status)
            {
                try
                {
                    var room = await _context.Rooms.FindAsync(roomId);
                    if (room == null)
                    {
                        return false;
                    }

                    // If changing to available, ensure there are no overlapping reservations
                    if (status == RoomStatus.Available)
                    {
                        bool hasActiveReservations = await _context.Reservations
                            .AnyAsync(r => r.RoomId == roomId &&
                                          (r.Status == ReservationStatus.Confirmed ||
                                           r.Status == ReservationStatus.CheckedIn));

                        if (hasActiveReservations)
                        {
                            throw new InvalidOperationException("Cannot mark room as available when it has active reservations");
                        }
                    }

                    room.Status = status;
                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating status for room with ID {RoomId}", roomId);
                    throw;
                }
            }

            public async Task<bool> AssignRoomToReservationAsync(int roomId, int reservationId)
            {
                try
                {
                    var room = await _context.Rooms.FindAsync(roomId);
                    if (room == null)
                    {
                        throw new KeyNotFoundException($"Room with ID {roomId} not found");
                    }

                    var reservation = await _context.Reservations
                        .Include(r => r.Room)
                        .FirstOrDefaultAsync(r => r.Id == reservationId);

                    if (reservation == null)
                    {
                        throw new KeyNotFoundException($"Reservation with ID {reservationId} not found");
                    }

                    // Check if room is available for the reservation dates
                    if (!await IsRoomAvailableAsync(roomId, reservation.CheckInDate, reservation.CheckOutDate, reservationId))
                    {
                        throw new InvalidOperationException("The room is not available for the reservation dates");
                    }

                    // Check if the room type matches the reservation's required room type
                    if (room.RoomTypeId != reservation.RoomTypeId)
                    {
                        throw new InvalidOperationException("The room type does not match the reservation's requirements");
                    }

                    // Assign room to reservation
                    reservation.RoomId = roomId;
                    room.Status = RoomStatus.Reserved;

                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error assigning room with ID {RoomId} to reservation with ID {ReservationId}", roomId, reservationId);
                    throw;
                }
            }

            public async Task<RoomDto> FindAvailableRoomByTypeAsync(int roomTypeId, DateTime checkIn, DateTime checkOut)
            {
                try
                {
                    // Get all rooms of the specified type
                    var roomsOfType = await _context.Rooms
                        .Include(r => r.RoomType)
                        .Include(r => r.LastCleanedBy)
                        .Where(r => r.RoomTypeId == roomTypeId && r.Status != RoomStatus.Maintenance)
                        .ToListAsync();

                    if (!roomsOfType.Any())
                    {
                        return null;
                    }

                    // Find the first available room
                    foreach (var room in roomsOfType)
                    {
                        if (await IsRoomAvailableAsync(room.Id, checkIn, checkOut))
                        {
                            return MapToRoomDto(room);
                        }
                    }

                    return null; // No available rooms found
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error finding available room for room type ID {RoomTypeId}", roomTypeId);
                    throw;
                }
            }

            public async Task<Dictionary<string, int>> GetRoomOccupancyStatsAsync()
            {
                try
                {
                    var stats = new Dictionary<string, int>();

                    // Count rooms by status
                    stats["Total"] = await _context.Rooms.CountAsync();
                    stats["Available"] = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Available);
                    stats["Occupied"] = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Occupied);
                    stats["Reserved"] = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Reserved);
                    stats["Maintenance"] = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Maintenance);
                    stats["NeedsCleaning"] = await _context.Rooms.CountAsync(r => r.NeedsCleaning);

                    // Calculate occupancy rate - occupied + reserved / total available rooms
                    decimal occupiedRooms = stats["Occupied"] + stats["Reserved"];
                    decimal availableRooms = stats["Total"] - stats["Maintenance"];

                    int occupancyRatePercentage = availableRooms > 0
                        ? (int)Math.Round((occupiedRooms / availableRooms) * 100)
                        : 0;

                    stats["OccupancyRate"] = occupancyRatePercentage;

                    return stats;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving room occupancy statistics");
                    throw;
                }
            }

            public async Task<Dictionary<int, int>> GetRoomsByTypeStatsAsync()
            {
                try
                {
                    var stats = new Dictionary<int, int>();

                    // Get all room types
                    var roomTypes = await _context.RoomTypes.ToListAsync();

                    // Count rooms for each room type
                    foreach (var roomType in roomTypes)
                    {
                        stats[roomType.Id] = await _context.Rooms.CountAsync(r => r.RoomTypeId == roomType.Id);
                    }

                    return stats;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving rooms by type statistics");
                    throw;
                }
            }

            #endregion

            #region Helper Methods

            private async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null)
            {
                try
                {
                    // Check room existence
                    var room = await _context.Rooms.FindAsync(roomId);
                    if (room == null)
                    {
                        throw new KeyNotFoundException($"Room with ID {roomId} not found");
                    }

                    // Check if room is out of service
                    if (room.Status == RoomStatus.Maintenance)
                    {
                        return false;
                    }

                    // Query for overlapping reservations
                    var overlappingReservations = _context.Reservations
                        .Where(r => r.RoomId == roomId &&
                                    r.Status != ReservationStatus.Cancelled &&
                                    ((checkIn >= r.CheckInDate && checkIn < r.CheckOutDate) ||
                                     (checkOut > r.CheckInDate && checkOut <= r.CheckOutDate) ||
                                     (checkIn <= r.CheckInDate && checkOut >= r.CheckOutDate)));

                    // Exclude the current reservation if provided
                    if (excludeReservationId.HasValue)
                    {
                        overlappingReservations = overlappingReservations.Where(r => r.Id != excludeReservationId.Value);
                    }

                    // If there are no overlapping reservations, the room is available
                    return !await overlappingReservations.AnyAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking room availability");
                    throw;
                }
            }

            private async Task<DateTime?> GetNextAvailableDateAsync(int roomId, DateTime startDate)
            {
                // Get all future reservations for this room
                var futureReservations = await _context.Reservations
                    .Where(r => r.RoomId == roomId &&
                               r.Status != ReservationStatus.Cancelled &&
                               r.CheckOutDate > startDate)
                    .OrderBy(r => r.CheckInDate)
                    .ToListAsync();

                if (!futureReservations.Any())
                {
                    // If no future reservations, the room is available from startDate
                    return startDate;
                }

                // Find the next available date based on gaps between reservations
                DateTime currentDate = startDate;
                foreach (var reservation in futureReservations)
                {
                    if (currentDate < reservation.CheckInDate)
                    {
                        // Found a gap where the room is available
                        return currentDate;
                    }

                    // Move to the day after this reservation ends
                    currentDate = reservation.CheckOutDate;
                }

                // Room is next available after the last reservation ends
                return currentDate;
            }

            private RoomDto MapToRoomDto(Room room)
            {
                if (room == null)
                    return null;

                return new RoomDto
                {
                    Id = room.Id,
                    RoomNumber = room.RoomNumber,
                    Floor = room.Floor,
                    RoomTypeId = room.RoomTypeId,
                    RoomTypeName = room.RoomType?.Name,
                    BasePrice = room.BasePrice,
                    Status = room.Status.ToString(),
                    LastCleaned = room.LastCleaned,
                    LastCleanedByName = room.LastCleanedBy?.UserName,
                    NeedsCleaning = room.NeedsCleaning,
                    Notes = room.Notes,

                    // Additional properties
                    Capacity = room.RoomType?.Capacity ?? 0,
                    RoomTypeDescription = room.RoomType?.Description,
                    Amenities = room.RoomType?.Amenities?.Split(','),
                    ImageUrl = room.RoomType?.ImageUrl,

                    // Statistics
                    ActiveReservationsCount = room.Reservations?.Count(r =>
                        r.Status == ReservationStatus.Confirmed ||
                        r.Status == ReservationStatus.CheckedIn) ?? 0,
                    PendingCleaningTasksCount = room.CleaningTasks?.Count(t =>
                        t.Status == CleaningRequestStatus.Dirty ||
                        t.Status == CleaningRequestStatus.InProgress) ?? 0
                };
            }

            #endregion
        }
    
}
