namespace HotelManagementApp.Models.DTOs.RoomType
{
    using System.ComponentModel.DataAnnotations;

    namespace HotelManagementApp.Models.DTOs.RoomType
    {
        public class CreateRoomTypeDto
        {
            [Required]
            [StringLength(50, ErrorMessage = "Name must be at most 50 characters")]
            public string Name { get; set; }

            [Required]
            [StringLength(500, ErrorMessage = "Description must be at most 500 characters")]
            public string Description { get; set; }

            [Required]
            [Range(1, 20, ErrorMessage = "Capacity must be between 1 and 20")]
            public int Capacity { get; set; }

            [Required]
            [Range(0, 10000, ErrorMessage = "Base price must be between 0 and 10000")]
            public decimal BasePrice { get; set; }

            [Required]
            public string Amenities { get; set; }

            public string ImageUrl { get; set; }
        }

        public class UpdateRoomTypeDto
        {
            [StringLength(50, ErrorMessage = "Name must be at most 50 characters")]
            public string Name { get; set; }

            [StringLength(500, ErrorMessage = "Description must be at most 500 characters")]
            public string Description { get; set; }

            [Range(1, 20, ErrorMessage = "Capacity must be between 1 and 20")]
            public int? Capacity { get; set; }

            [Range(0, 10000, ErrorMessage = "Base price must be between 0 and 10000")]
            public decimal? BasePrice { get; set; }

            public string Amenities { get; set; }

            public string ImageUrl { get; set; }
        }

        public class RoomTypeDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public int Capacity { get; set; }
            public decimal BasePrice { get; set; }
            public string[] Amenities { get; set; }
            public string ImageUrl { get; set; }

            // Statistics
            public int TotalRooms { get; set; }
            public int AvailableRooms { get; set; }
            public int OccupiedRooms { get; set; }
            public double OccupancyRate { get; set; }
            public decimal AverageRating { get; set; }
        }

        public class RoomTypeSummaryDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Capacity { get; set; }
            public decimal BasePrice { get; set; }
            public string ImageUrl { get; set; }
            public bool IsAvailable { get; set; }
        }
    }

}
