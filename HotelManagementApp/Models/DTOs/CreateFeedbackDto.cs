using System.ComponentModel.DataAnnotations;

namespace HotelManagementApp.Models.DTOs
{
    namespace HotelManagement.Models.DTOs
    {
        // Request DTOs
        public class CreateFeedbackDto
        {
            [StringLength(100)]
            public string GuestName { get; set; }

            [StringLength(100)]
            [EmailAddress]
            public string GuestEmail { get; set; }

            [Required]
            [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
            public int Rating { get; set; }

            [StringLength(200)]
            public string Subject { get; set; }

            [Required]
            [StringLength(2000)]
            public string Comments { get; set; }

            [StringLength(50)]
            public string Category { get; set; }

            public int? ReservationId { get; set; }
        }

        public class UpdateFeedbackDto
        {
            [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
            public int Rating { get; set; }

            [StringLength(200)]
            public string Subject { get; set; }

            [StringLength(2000)]
            public string Comments { get; set; }

            [StringLength(50)]
            public string Category { get; set; }
        }

        public class ResolveFeedbackDto
        {
            [Required]
            [StringLength(1000)]
            public string ResolutionNotes { get; set; }
        }

        // Response DTOs
        public class FeedbackDto
        {
            public int Id { get; set; }
            public int? UserId { get; set; }
            public string UserName { get; set; }
            public int? ReservationId { get; set; }
            public string ReservationNumber { get; set; }
            public string GuestName { get; set; }
            public string GuestEmail { get; set; }
            public int Rating { get; set; }
            public string Subject { get; set; }
            public string Comments { get; set; }
            public string Category { get; set; }
            public bool IsResolved { get; set; }
            public string ResolutionNotes { get; set; }
            public int? ResolvedById { get; set; }
            public string ResolvedByName { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ResolvedAt { get; set; }
        }

        public class FeedbackSummaryDto
        {
            public int Id { get; set; }
            public string GuestName { get; set; }
            public int Rating { get; set; }
            public string Subject { get; set; }
            public string Category { get; set; }
            public bool IsResolved { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class FeedbackStatsByCategoryDto
        {
            public Dictionary<string, int> CategoryCounts { get; set; }
            public int TotalFeedbackCount { get; set; }
        }

        public class FeedbackStatsByRatingDto
        {
            public Dictionary<int, int> RatingCounts { get; set; }
            public double AverageRating { get; set; }
            public int TotalFeedbackCount { get; set; }
        }
    }
}
