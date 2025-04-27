namespace HotelManagementApp.Services.CleaningTaskSpace;

using global::HotelManagementApp.Models.DTOs.CleaningTask;
using global::HotelManagementApp.Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
    public interface ICleaningService
    {
        // CRUD operations
        Task<IEnumerable<CleaningTaskDto>> GetAllTasksAsync();
        Task<CleaningTaskDto> GetTaskByIdAsync(int id);
        Task<CleaningTaskDto> CreateTaskAsync(CreateCleaningTaskDto task);
        Task<CleaningTaskDto> UpdateTaskAsync(int id, UpdateCleaningTaskDto task);
        Task<bool> DeleteTaskAsync(int id);

        // Specialized operations
        Task<IEnumerable<CleaningTaskDto>> GetTasksByRoomAsync(int roomId);
        Task<IEnumerable<CleaningTaskDto>> GetTasksByCleanerAsync(int cleanerId);
        Task<IEnumerable<CleaningTaskDto>> GetTasksByStatusAsync(CleaningRequestStatus status);
        Task<IEnumerable<CleaningTaskDto>> GetTasksByPriorityAsync(Priority priority);
        Task<IEnumerable<CleaningTaskDto>> GetTasksByDateRangeAsync(DateTime start, DateTime end);

        // Business operations
        Task<bool> AssignTaskToCleanerAsync(int taskId, int cleanerId);
        Task<bool> CompleteTaskAsync(int taskId, string notes);
        Task<bool> StartTaskAsync(int taskId);
        Task<CleaningTaskDto> CreateTaskForRoomAsync(int roomId, Priority priority, string description = null);
        Task<bool> CreateTasksForCheckoutAsync(DateTime date);
        Task<Dictionary<string, int>> GetTasksStatsByStatusAsync();
        Task<Dictionary<string, int>> GetTasksStatsByPriorityAsync();
        Task<Dictionary<int, int>> GetTasksStatsByCleanerAsync();
        Task<CleaningTasksStatisticsDto> GetCleaningTasksStatisticsAsync();
    }

