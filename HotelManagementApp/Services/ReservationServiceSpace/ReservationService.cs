using HotelManagementApp.Models;
using HotelManagementApp.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementApp.Services.ReservationServiceSpace
{
    public class ReservationService : IReservationService
    {
        private readonly AppDbContext _context;

        public ReservationService(AppDbContext context)
        {
            _context = context;
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
            return await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Reservation> CreateReservationAsync(Reservation reservation)
        {
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }

        public async Task<Reservation> UpdateReservationAsync(Reservation reservation)
        {
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }

        public async Task<bool> DeleteReservationAsync(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return false;
            }

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
            return true;
        }

        // Specialized operations
        public async Task<IEnumerable<Reservation>> GetReservationsByUserAsync(int userId)
        {
            return await _context.Reservations
                .Where(r => r.UserId == userId)
                .Include(r => r.User)
                .Include(r => r.Room)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByStatusAsync(string status)
        {
            return await _context.Reservations
                .Where(r => r.Status.ToString() == status)
                .Include(r => r.User)
                .Include(r => r.Room)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByDateRangeAsync(DateTime start, DateTime end)
        {
            return await _context.Reservations
                .Where(r => r.CheckInDate >= start && r.CheckOutDate <= end)
                .Include(r => r.User)
                .Include(r => r.Room)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByRoomAsync(int roomId)
        {
            return await _context.Reservations
                .Where(r => r.RoomId == roomId)
                .Include(r => r.User)
                .Include(r => r.Room)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reservation>> GetTodayArrivalsAsync()
        {
            var today = DateTime.Today;
            return await _context.Reservations
                .Where(r => r.CheckInDate == today)
                .Include(r => r.User)
                .Include(r => r.Room)
                .ToListAsync();
        }

        public async Task<IEnumerable<Reservation>> GetTodayDeparturesAsync()
        {
            var today = DateTime.Today;
            return await _context.Reservations
                .Where(r => r.CheckOutDate == today)
                .Include(r => r.User)
                .Include(r => r.Room)
                .ToListAsync();
        }

        // Business operations
        public async Task<bool> CancelReservationAsync(int id, string reason)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return false;
            }

            reservation.Status = ReservationStatus.Cancelled;
            reservation.CancellationReason = reason;
            reservation.CancelledAt = DateTime.UtcNow;

            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CheckInAsync(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return false;
            }

            reservation.Status = ReservationStatus.CheckedIn;
            reservation.CheckedInBy = reservation.UserId;

            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CheckOutAsync(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return false;
            }

            reservation.Status = ReservationStatus.CheckedOut;
            reservation.CheckedOutBy = reservation.UserId;

            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null)
        {
            var overlappingReservations = await _context.Reservations
                .Where(r => r.RoomId == roomId && r.Id != excludeReservationId)
                .Where(r => r.CheckInDate < checkOut && r.CheckOutDate > checkIn)
                .ToListAsync();

            return !overlappingReservations.Any();
        }

        public async Task<Dictionary<string, int>> GetReservationStatsAsync()
        {
            var stats = new Dictionary<string, int>
            {
                { "TotalReservations", await _context.Reservations.CountAsync() },
                { "CheckedIn", await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.CheckedIn) },
                { "CheckedOut", await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.CheckedOut) },
                { "Cancelled", await _context.Reservations.CountAsync(r => r.Status == ReservationStatus.Cancelled) }
            };

            return stats;
        }

        public async Task<Dictionary<DateTime, int>> GetReservationForecastAsync(DateTime start, DateTime end)
        {
            var forecast = await _context.Reservations
                .Where(r => r.CheckInDate >= start && r.CheckInDate <= end)
                .GroupBy(r => r.CheckInDate)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Date, g => g.Count);

            return forecast;
        }
    }
}

