using HotelManagementApp.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace HotelManagementApp.Models.DTOs.Room
{
    public class UpdateRoomDto
    {
        [StringLength(10, ErrorMessage = "Room number must be at most 10 characters")]
        public string RoomNumber { get; set; }

        [Range(1, 100, ErrorMessage = "Floor must be between 1 and 100")]
        public int? Floor { get; set; }

        public int? RoomTypeId { get; set; }

        [Range(0, 10000, ErrorMessage = "Base price must be between 0 and 10000")]
        public decimal? BasePrice { get; set; }

        public RoomStatus? Status { get; set; }

        public bool? NeedsCleaning { get; set; }

        [StringLength(500, ErrorMessage = "Notes must be at most 500 characters")]
        public string Notes { get; set; }
    }
}

