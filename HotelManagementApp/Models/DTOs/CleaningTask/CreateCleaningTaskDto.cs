namespace HotelManagementApp.Models.DTOs.CleaningTask
{
    
        using global::HotelManagementApp.Models.Enums;
        using System;
        using System.ComponentModel.DataAnnotations;

        public class CreateCleaningTaskDto
        {
            [Required]
            public int RoomId { get; set; }

            [Required]
            [StringLength(200, ErrorMessage = "Description must be at most 200 characters")]
            public string Description { get; set; }

            [Required]
            public CleaningRequestStatus Status { get; set; } = CleaningRequestStatus.Dirty;

            [Required]
            public Priority Priority { get; set; } = Priority.Medium;

            public int? AssignedToId { get; set; }

            public DateTime? ScheduledFor { get; set; }

            [StringLength(500)]
            public string Notes { get; set; }
        }

        public class UpdateCleaningTaskDto
        {
            [StringLength(200, ErrorMessage = "Description must be at most 200 characters")]
            public string Description { get; set; }

            public CleaningRequestStatus? Status { get; set; }

            public Priority? Priority { get; set; }

            public int? AssignedToId { get; set; }

            public DateTime? ScheduledFor { get; set; }

            [StringLength(500)]
            public string Notes { get; set; }

            public DateTime? CompletedAt { get; set; }

            [StringLength(500)]
            public string CompletionNotes { get; set; }
        }

        public class CleaningTaskDto
        {
            public int Id { get; set; }
            public string TaskId { get; set; }
            public int RoomId { get; set; }
            public string RoomNumber { get; set; }
            public int Floor { get; set; }
            public string RoomTypeName { get; set; }
            public string Description { get; set; }
            public string Status { get; set; }
            public string Priority { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ScheduledFor { get; set; }
            public DateTime? CompletedAt { get; set; }

            // Assignment information
            public int? AssignedToId { get; set; }
            public string AssignedToName { get; set; }
            public DateTime? AssignedAt { get; set; }

            // Completion information
            public string CompletionNotes { get; set; }
            public TimeSpan? TimeToComplete { get; set; }

            // Notes
            public string Notes { get; set; }
        }

        public class CleaningTaskSummaryDto
        {
            public int Id { get; set; }
            public string TaskId { get; set; }
            public string RoomNumber { get; set; }
            public string Status { get; set; }
            public string Priority { get; set; }
            public DateTime CreatedAt { get; set; }
            public string AssignedToName { get; set; }
        }

        public class CleaningTasksStatisticsDto
        {
            public int TotalTasks { get; set; }
            public int CompletedTasks { get; set; }
            public int PendingTasks { get; set; }
            public int InProgressTasks { get; set; }
            public double AverageCompletionTime { get; set; } // In minutes
            public Dictionary<string, int> TasksByStatus { get; set; }
            public Dictionary<string, int> TasksByPriority { get; set; }
            public Dictionary<string, int> TasksByHousekeeper { get; set; }
            public Dictionary<string, double> AverageTimeByHousekeeper { get; set; }
        }
    

}
