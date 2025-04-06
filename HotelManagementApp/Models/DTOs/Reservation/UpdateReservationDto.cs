using HotelManagementApp.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace HotelManagementApp.Models.DTOs.Reservation
{
    public class UpdateReservationDto
    {
        public DateTime? CheckInDate { get; set; }

        public DateTime? CheckOutDate { get; set; }

        public int? RoomTypeId { get; set; }

        [Range(1, 10, ErrorMessage = "Number of guests must be between 1 and 10")]
        public int? NumberOfGuests { get; set; }

        [StringLength(500)]
        public string SpecialRequests { get; set; }

        public PaymentMethod? PaymentMethod { get; set; }
    }
}
