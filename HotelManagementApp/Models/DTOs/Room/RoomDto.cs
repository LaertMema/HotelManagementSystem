using HotelManagementApp.Models.Enums;
using System;
namespace HotelManagementApp.Models.DTOs.Room
{
        public class RoomDto
        {
            public int Id { get; set; }
            public string RoomNumber { get; set; }
            public int Floor { get; set; }
            public int RoomTypeId { get; set; }
            public string RoomTypeName { get; set; }
            public decimal BasePrice { get; set; }
            public string Status { get; set; }
            public DateTime? LastCleaned { get; set; }
            public string LastCleanedByName { get; set; }
            public bool NeedsCleaning { get; set; }
            public string Notes { get; set; }

            // Additional properties for detailed information
            public int Capacity { get; set; }
            public string RoomTypeDescription { get; set; }
            public string[] Amenities { get; set; }
            public string ImageUrl { get; set; }

            // Statistics
            public int ActiveReservationsCount { get; set; }
            public int PendingCleaningTasksCount { get; set; }
            public DateTime? NextAvailableDate { get; set; }
        }
    

}
