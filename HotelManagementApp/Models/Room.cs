using HotelManagementApp.Models.Enums;

namespace HotelManagementApp.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public int Floor { get; set; }
        public int RoomTypeId { get; set; }
        public RoomType RoomType { get; set; }

        public decimal BasePrice { get; set; } // Base price for the room
        public RoomStatus Status { get; set; } // Enum for Available, Occupied, Maintenance, Cleaning

        public DateTime? LastCleaned { get; set; }
        public int? CleanedById { get; set; }
        public ApplicationUser LastCleanedBy { get; set; }
        public bool NeedsCleaning { get; set; }

        public string Notes { get; set; } // Additional room-related notes

        // Navigation properties
        public ICollection<Reservation> Reservations { get; set; }
        public ICollection<CleaningTask> CleaningTasks { get; set; }
    }
}
