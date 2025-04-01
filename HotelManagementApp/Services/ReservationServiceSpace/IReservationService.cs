using HotelManagementApp.Models;
using HotelManagementApp.Models.Enums;

namespace HotelManagementApp.Services.ReservationServiceSpace
{
    public interface IReservationService
    {
        // CRUD operations
        Task<IEnumerable<Reservation>> GetAllReservationsAsync();
        Task<Reservation> GetReservationByIdAsync(int id);
        Task<Reservation> CreateReservationAsync(Reservation reservation);
        Task<Reservation> UpdateReservationAsync(Reservation reservation);
        Task<bool> DeleteReservationAsync(int id);
        // Specialized operations
        Task<IEnumerable<Reservation>> GetReservationsByUserAsync(int userId);
        Task<IEnumerable<Reservation>> GetReservationsByStatusAsync(ReservationStatus status);
        Task<IEnumerable<Reservation>> GetReservationsByDateRangeAsync(DateTime start, DateTime end);
        Task<IEnumerable<Reservation>> GetReservationsByRoomAsync(int roomId);
        Task<IEnumerable<Reservation>> GetTodayArrivalsAsync();
        Task<IEnumerable<Reservation>> GetTodayDeparturesAsync();

        // Business operations
        Task<bool> CancelReservationAsync(int id, string reason);
        Task<bool> CheckInAsync(int id,int roomId, int receptionistId);
        Task<bool> CheckOutAsync(int id, int receptionistId);
        Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null);
        Task<Dictionary<string, int>> GetReservationStatsAsync();
        Task<Dictionary<DateTime, int>> GetReservationForecastAsync(DateTime start, DateTime end);
    
    }
}
