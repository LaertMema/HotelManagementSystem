using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HotelManagementApp.Models
{
    public class RoomType
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } // Merged from TypeName

        public string Description { get; set; }

        [Required, Column(TypeName = "decimal(10,2)")]
        public decimal BasePrice { get; set; } // Kept from the second class

        [Required]
        public int Capacity { get; set; }

        public string ImageUrl { get; set; } // Kept for UI

        public string Amenities { get; set; } // JSON string, consider List<string> with conversion

        // Navigation property
        public virtual ICollection<Room> Rooms { get; set; }

        public virtual ICollection<Reservation> Reservations { get; set; }
    }
}
