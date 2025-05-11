namespace HotelManagementApp.Models.DTOs.Service
{
   
        using System.ComponentModel.DataAnnotations;

        public class CreateServiceDto
        {
            [Required]
            [StringLength(100, ErrorMessage = "Service name must be at most 100 characters")]
            public string ServiceName { get; set; }

            [Required]
            [StringLength(500, ErrorMessage = "Description must be at most 500 characters")]
            public string Description { get; set; }

            [Required]
            [Range(0, 10000, ErrorMessage = "Price must be between 0 and 10,000")]
            public decimal Price { get; set; }

            [Required]
            [StringLength(50, ErrorMessage = "Service type must be at most 50 characters")]
            public string ServiceType { get; set; }  // Room service, Spa, Laundry, etc.

            //[StringLength(100)]
            //public string Category { get; set; }

            public bool IsActive { get; set; } = true;

            //[StringLength(200)]
            //public string ImageUrl { get; set; }
        }

        public class UpdateServiceDto
        {
            [StringLength(100, ErrorMessage = "Service name must be at most 100 characters")]
            public string ServiceName { get; set; }

            [StringLength(500, ErrorMessage = "Description must be at most 500 characters")]
            public string Description { get; set; }

            [Range(0, 10000, ErrorMessage = "Price must be between 0 and 10,000")]
            public decimal? Price { get; set; }

            [StringLength(50, ErrorMessage = "Service type must be at most 50 characters")]
            public string ServiceType { get; set; }

            //[StringLength(100)]
            //public string Category { get; set; }

            public bool? IsActive { get; set; }

            //[StringLength(200)]
            //public string ImageUrl { get; set; }
        }

        public class ServiceDto
        {
            public int Id { get; set; }
            public string ServiceName { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public string ServiceType { get; set; }
            //public string Category { get; set; }
            public bool IsActive { get; set; }
            //public string ImageUrl { get; set; }
            //public DateTime CreatedAt { get; set; }
            //public DateTime? UpdatedAt { get; set; }

            // Statistics
            public int TotalOrders { get; set; }
            public decimal TotalRevenue { get; set; }
            public double AverageRating { get; set; }
        }

        public class ServiceSummaryDto
        {
            public int Id { get; set; }
            public string ServiceName { get; set; }
            public decimal Price { get; set; }
            public string ServiceType { get; set; }
            public bool IsActive { get; set; }
        }
    

}
