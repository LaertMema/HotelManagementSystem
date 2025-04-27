using HotelManagementApp.Models;
using HotelManagementApp.Models.Enums;

using HotelManagementApp.Services; // Add this line to ensure the namespace is included
using Microsoft.EntityFrameworkCore;
using HotelManagementApp.Services.InvoiceSpace;
using HotelManagementApp.Services.CleaningTaskSpace;
using HotelManagementApp.Models.DTOs.CleaningTask;

namespace HotelManagementApp.Services.ReservationServiceSpace
{
    public class ReservationService : IReservationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ReservationService> _logger;
        private readonly ICleaningService _cleaningTaskService;
        private readonly IInvoiceService _invoiceService;


        public ReservationService(AppDbContext context, ILogger<ReservationService> logger, ICleaningService cleaningService, IInvoiceService invoiceService)
        {
            _context = context;
            _logger = logger;
            _cleaningTaskService = cleaningService;
            _invoiceService = invoiceService;

        }

        // CRUD operations
        public async Task<IEnumerable<Reservation>> GetAllReservationsAsync()
        {
            return await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Room)
                .ThenInclude(r => r.RoomType)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task<Reservation> GetReservationByIdAsync(int id)
        {
            //return await _context.Reservations
            //    .Include(r => r.User)
            //    .Include(r => r.Room)
            //    .FirstOrDefaultAsync(r => r.Id == id);
            return await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Room)
                .ThenInclude(r => r.RoomType)
                .Include(r => r.Invoices)
                .Include(r => r.ServiceOrders)
                .ThenInclude(so => so.Service)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Reservation> CreateReservationAsync(Reservation reservation)
        {
            //_context.Reservations.Add(reservation);
            //await _context.SaveChangesAsync();
            //return reservation;
            //Generating reservation number and automatic pending status
            //reservation.ReservationNumber = await GenerateReservationNumberAsync();
            //reservation.ReservationDate = DateTime.Now;
            //reservation.Status = ReservationStatus.Pending;
            try
            {
                // Check if the room type is available for the selected dates
                if (!await IsRoomTypeAvailableAsync(reservation.RoomTypeId, reservation.CheckInDate, reservation.CheckOutDate))
                {
                    throw new InvalidOperationException("Selected room type is not available for the specified dates");
                }

                // Generate reservation number
                reservation.ReservationNumber = GenerateReservationNumber();

                // Calculate total price based on room type, dates and additional services
                var roomType = await _context.RoomTypes.FindAsync(reservation.RoomTypeId);
                var numberOfNights = (reservation.CheckOutDate - reservation.CheckInDate).Days;

                // Base price calculation
                 // Assuming a base cost of $100
                reservation.TotalPrice = roomType.BasePrice * numberOfNights;
                //TODO MOS HARRO TE SHTOSH DHE CMIMET E SERVICES ME VONE KUR TE KRIJOSH SERVICE
                // Include the prices of additional services
                if (reservation.ServiceOrders != null && reservation.ServiceOrders.Any())
                {
                    foreach (var serviceOrder in reservation.ServiceOrders)
                    {
                        var service = await _context.Services.FindAsync(serviceOrder.ServiceId);
                        if (service != null)
                        {
                            reservation.TotalPrice += service.Price * serviceOrder.Quantity;
                        }
                    }
                }
                // Set status to Reserved
                reservation.Status = ReservationStatus.Reserved;
                // Create an invoice for the reservation
                var invoice = new Invoice
                {
                    Reservation = reservation,
                    Amount = reservation.TotalPrice,
                    Tax = reservation.TotalPrice * 0.1m, // Assuming a 10% tax rate
                    Total = reservation.TotalPrice * 1.1m, // Total amount including tax
                    CreatedAt = DateTime.UtcNow
                };

                reservation.Invoices = new List<Invoice> { invoice };


                // Set creation date
                reservation.ReservationDate = DateTime.UtcNow;

                await _context.Reservations.AddAsync(reservation);
                await _context.SaveChangesAsync();

                return reservation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reservation");
                throw;
            }
        
        }

        public async Task<Reservation> UpdateReservationAsync(Reservation reservation)
        {
            try
            {
                var existingReservation = await _context.Reservations.FindAsync(reservation.Id) ?? throw new KeyNotFoundException($"Reservation with ID {reservation.Id} not found");

                // If check-in/out dates have changed, verify availability
                if (existingReservation.CheckInDate != reservation.CheckInDate ||
                    existingReservation.CheckOutDate != reservation.CheckOutDate ||
                    existingReservation.RoomTypeId != reservation.RoomTypeId)
                {
                    if (!await IsRoomTypeAvailableAsync(reservation.RoomTypeId, reservation.CheckInDate, reservation.CheckOutDate, reservation.Id))
                    {
                        throw new InvalidOperationException("Selected room type is not available for the updated dates");
                    }

                    // Recalculate total price
                    var roomType = await _context.RoomTypes.FindAsync(reservation.RoomTypeId);
                    var numberOfNights = (reservation.CheckOutDate - reservation.CheckInDate).Days;
                    reservation.TotalPrice = roomType.BasePrice* numberOfNights; //UPDATE LATER
                                                                                 // Include the prices of additional services
                    if (reservation.ServiceOrders != null && reservation.ServiceOrders.Any())
                    {
                        foreach (var serviceOrder in reservation.ServiceOrders)
                        {
                            var service = await _context.Services.FindAsync(serviceOrder.ServiceId);
                            if (service != null)
                            {
                                reservation.TotalPrice += service.Price * serviceOrder.Quantity;
                            }
                        }
                    }
                    // Update the invoice
                    var invoice = existingReservation.Invoices.FirstOrDefault();
                    if (invoice != null)
                    {
                        invoice.Amount = reservation.TotalPrice;
                        invoice.Tax = reservation.TotalPrice * 0.1m; // Assuming a 10% tax rate
                        invoice.Total = reservation.TotalPrice * 1.1m; // Total amount including tax
                        _context.Invoices.Update(invoice);
                    }
                }
                // Preserve existing check-in and check-out times if not explicitly set
                reservation.CheckedInTime = existingReservation.CheckedInTime ?? reservation.CheckedInTime;
                reservation.CheckedOutTime = existingReservation.CheckedOutTime ?? reservation.CheckedOutTime;


                // Update the reservation
                _context.Entry(existingReservation).CurrentValues.SetValues(reservation);
                await _context.SaveChangesAsync();

                return reservation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reservation with ID {ReservationId}", reservation.Id);
                throw;
            }
        }

        public async Task<bool> DeleteReservationAsync(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return false;
            }

            _context.Reservations.Remove(reservation);
            // Delete all associated invoices ( e shtova une dhe ketu per siguri)
            _context.Invoices.RemoveRange(reservation.Invoices);
            await _context.SaveChangesAsync();
            return true;
        }

        // Specialized operations
        public async Task<IEnumerable<Reservation>> GetReservationsByUserAsync(int userId)
        {
            try
            {
                return await _context.Reservations
                    .Include(r => r.Room)
                    .Include(r => r.RoomType)
                    .Include(r => r.ServiceOrders)
                        .ThenInclude(so => so.Service)
                    .Where(r => r.UserId == userId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservations for user with ID {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByStatusAsync(ReservationStatus status)
        {
            try
            {
                return await _context.Reservations
                    .Include(r => r.User)
                    .Include(r => r.Room)
                    .Include(r => r.RoomType)
                    .Where(r => r.Status == status)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservations with status {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByDateRangeAsync(DateTime start, DateTime end)
        {
            try
            {
                return await _context.Reservations
                    .Include(r => r.User)
                    .Include(r => r.Room)
                    .Include(r => r.RoomType)
                    .Where(r =>
                        (r.CheckInDate >= start && r.CheckInDate <= end) ||
                        (r.CheckOutDate >= start && r.CheckOutDate <= end) ||
                        (r.CheckInDate <= start && r.CheckOutDate >= end))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservations between {StartDate} and {EndDate}", start, end);
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByRoomAsync(int roomId)
        {
            try
            {
                return await _context.Reservations
                    .Include(r => r.User)
                    .Include(r => r.RoomType)
                    .Where(r => r.RoomId == roomId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservations for room with ID {RoomId}", roomId);
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetTodayArrivalsAsync()
        {
            try
            {
                var today = DateTime.Today;
                return await _context.Reservations
                    .Include(r => r.User)
                    .Include(r => r.RoomType)
                    .Where(r => r.CheckInDate.Date == today && r.Status == ReservationStatus.Reserved)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving today's arrivals");
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetTodayDeparturesAsync()
        {
            try
            {
                var today = DateTime.Today;
                return await _context.Reservations
                    .Include(r => r.User)
                    .Include(r => r.Room)
                    .Include(r => r.RoomType)
                    .Where(r => r.CheckOutDate.Date == today && r.Status == ReservationStatus.CheckedIn)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving today's departures");
                throw;
            }
        }

        // Business operations
        public async Task<bool> CancelReservationAsync(int id, string reason)
        {
            try
            {
                var reservation = await _context.Reservations.FindAsync(id);
                if (reservation == null)
                {
                    return false;
                }

                // Only allow cancellation if not already checked in
                if (reservation.Status == ReservationStatus.CheckedIn)
                {
                    throw new InvalidOperationException("Cannot cancel a reservation that is already checked in");
                }

                reservation.Status = ReservationStatus.Cancelled;
                reservation.CancellationReason = $"Cancellation reason: {reason}. Cancelled on {DateTime.UtcNow}";
                reservation.CancelledAt = DateTime.UtcNow;
                // Delete all associated invoices
                _context.Invoices.RemoveRange(reservation.Invoices);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling reservation with ID {ReservationId}", id);
                throw;
            }
        }

        public async Task<bool> CheckInAsync(int id,int roomId, int receptionistId)
        {
            try
            {
                var reservation = await _context.Reservations.FindAsync(id);
                if (reservation == null)
                {
                    return false;
                }

                // Check if the reservation is in the correct status
                if (reservation.Status != ReservationStatus.CheckedIn)
                {
                    throw new InvalidOperationException($"Cannot check in reservation with status '{reservation.Status}'");
                }

                // Check if the room is available
                if (!await IsRoomAvailableAsync(roomId, reservation.CheckInDate, reservation.CheckOutDate))
                {
                    throw new InvalidOperationException("Selected room is not available for check-in");
                }

                // Check if the room is the correct type
                var room = await _context.Rooms.FindAsync(roomId);
                if (room.RoomTypeId != reservation.RoomTypeId)
                {
                    throw new InvalidOperationException("Selected room does not match the reserved room type");
                }

                // Update reservation
                reservation.Status = ReservationStatus.CheckedIn;
                reservation.RoomId = roomId;
                reservation.CheckedInTime = DateTime.UtcNow;
                ApplicationUser receptionist = await _context.Users.FindAsync(receptionistId);
                if(reservation.CheckedInBy!=null && reservation.CheckedInByUser != null)
                {
                    throw new InvalidOperationException("Reservation is already checked in");
                }
                if (reservation.CheckedInBy == null && receptionist!=null)
                {
                    reservation.CheckedInBy = receptionistId;
                    reservation.CheckedInByUser = receptionist;
                }
                
                

                // Update room status
                room.Status = RoomStatus.Occupied;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking in reservation with ID {ReservationId}", id);
                throw;
            }
        }

        public async Task<bool> CheckOutAsync(int id, int receptionistId)
        {
            try
            {
                var reservation = await _context.Reservations
                    .Include(r => r.Room)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reservation == null)
                {
                    return false;
                }

                // Check if the reservation is in the correct status
                if (reservation.Status != ReservationStatus.CheckedIn)
                {
                    throw new InvalidOperationException($"Cannot check out reservation with status '{reservation.Status}'");
                }

                // Update reservation
                reservation.Status = ReservationStatus.CheckedOut; 
                reservation.CheckedOutTime = DateTime.UtcNow;

                // Update room status
                var room = reservation.Room;
                room.Status = RoomStatus.Maintenance;
                room.NeedsCleaning = true;// Mark for cleaning
                var housekeepers = await _context.Users
                .Join(_context.UserRoles, user => user.Id, userRole => userRole.UserId, (user, userRole) => new { user, userRole })
                .Join(_context.Roles, combined => combined.userRole.RoleId, role => role.Id, (combined, role) => new { combined.user, role })
                .Where(combined => combined.role.Name == "Housekeeper" && combined.user.IsActive)
                .Select(combined => combined.user)
                .ToListAsync();

                // Assign to a random housekeeper if available, otherwise leave unassigned
                int? assignedToId = null;
                if (housekeepers.Any())
                {
                    var randomIndex = new Random().Next(0, housekeepers.Count);
                    assignedToId = housekeepers[randomIndex].Id;
                }



                // Create cleaning task
                CreateCleaningTaskDto cleaningTaskDto = new CreateCleaningTaskDto
                {
                    RoomId = room.Id,
                    Priority = Priority.Medium,
                    Description = $"Cleaning task for room {room.RoomNumber} after checkout",
                    AssignedToId = assignedToId// Assign to a cleaner later
                }; 

                await _cleaningTaskService.CreateTaskAsync(cleaningTaskDto);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking out reservation with ID {ReservationId}", id);
                throw;
            }
        
        }

        //public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null)
        //{
        //    var overlappingReservations = await _context.Reservations
        //        .Where(r => r.RoomId == roomId && r.Id != excludeReservationId)
        //        .Where(r => r.CheckInDate < checkOut && r.CheckOutDate > checkIn)
        //        .ToListAsync();

        //    return !overlappingReservations.Any();
        //}
        public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null)
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
                                r.Status != ReservationStatus.Reserved &&
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
        public async Task<bool> IsRoomTypeAvailableAsync(int roomTypeId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null)
        {
            try
            {
                // Get all rooms of the specified type
                var roomsOfType = await _context.Rooms
                    .Where(r => r.RoomTypeId == roomTypeId)
                    .ToListAsync();

                if (!roomsOfType.Any())
                {
                    return false;
                }

                // For each room, check if it's available
                foreach (var room in roomsOfType)
                {
                    if (await IsRoomAvailableAsync(room.Id, checkIn, checkOut, excludeReservationId))
                    {
                        return true; // If at least one room is available, return true
                    }
                }

                return false; // No available rooms of this type
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking room type availability");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetReservationStatsAsync()
        {
            //var stats = new Dictionary<string, int>
            //{
            //    { "TotalReservations", await _context.Reservations.CountAsync() },
            //    { "CheckedIn", await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.CheckedIn) },
            //    { "CheckedOut", await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.CheckedOut) },
            //    { "Cancelled", await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Cancelled) }
            //};

            //return stats;
            try
            {
                var stats = new Dictionary<string, int>();

                stats["Total"] = await _context.Reservations.CountAsync();
                stats["Reserved"] = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Confirmed);
                stats["CheckedIn"] = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.CheckedIn);
                stats["Completed"] = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.CheckedOut);
                stats["Cancelled"] = await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Cancelled);

                stats["TodayArrivals"] = await _context.Reservations
                    .CountAsync(r => r.CheckInDate.Date == DateTime.Today && r.Status == ReservationStatus.Reserved);

                stats["TodayDepartures"] = await _context.Reservations
                    .CountAsync(r => r.CheckOutDate.Date == DateTime.Today && r.Status == ReservationStatus.CheckedIn);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservation statistics");
                throw;
            }
        }

        public async Task<Dictionary<DateTime, int>> GetReservationForecastAsync(DateTime start, DateTime end)
        {
            //var forecast = await _context.Reservations
            //    .Where(r => r.CheckInDate >= start && r.CheckInDate <= end)
            //    .GroupBy(r => r.CheckInDate)
            //    .Select(g => new { Date = g.Key, Count = g.Count() })
            //    .ToDictionaryAsync(g => g.Date, g => g.Count);

            //return forecast;
            try
            {
                var forecast = new Dictionary<DateTime, int>();

                // Initialize the dictionary with all dates in the range
                for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
                {
                    forecast[date] = 0;
                }

                // Get all reservations that overlap with the date range
                var reservations = await _context.Reservations
                    .Where(r => r.Status != ReservationStatus.Cancelled &&
                                ((r.CheckInDate >= start && r.CheckInDate <= end) ||
                                 (r.CheckOutDate >= start && r.CheckOutDate <= end) ||
                                 (r.CheckInDate <= start && r.CheckOutDate >= end)))
                    .ToListAsync();

                // For each reservation, increment the count for each day it spans
                foreach (var reservation in reservations)
                {
                    var reservationStart = reservation.CheckInDate > start ? reservation.CheckInDate : start;
                    var reservationEnd = reservation.CheckOutDate < end ? reservation.CheckOutDate : end;

                    for (var date = reservationStart.Date; date < reservationEnd.Date; date = date.AddDays(1))
                    {
                        if (forecast.ContainsKey(date))
                        {
                            forecast[date]++;
                        }
                    }
                }

                return forecast;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservation forecast");
                throw;
            }
        
        }
        public async Task<List<Room>> GetAvailableRoomsForTypeAsync(int roomTypeId, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                // Get all rooms of the specified type
                var roomsOfType = await _context.Rooms
                    .Where(r => r.RoomTypeId == roomTypeId)
                    .ToListAsync();

                if (!roomsOfType.Any())
                {
                    return new List<Room>();
                }

                var availableRooms = new List<Room>();

                // Check availability for each room
                foreach (var room in roomsOfType)
                {
                    if (await IsRoomAvailableAsync(room.Id, checkIn, checkOut))
                    {
                        availableRooms.Add(room);
                    }
                }

                return availableRooms;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available rooms for room type");
                throw;
            }
        }
        private string GenerateReservationNumber()
        {
            // Generate a unique reservation number - format: RES-{Timestamp}-{Random4Digits}
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var randomPart = new Random().Next(1000, 9999).ToString();
            return $"RES-{timestamp}-{randomPart}";
        }
    }
}

