namespace HotelManagementApp.Services.CleaningTaskSpace
{
    using HotelManagementApp.Models.DTOs.CleaningTask;
    using HotelManagementApp.Models.Enums;
    using HotelManagementApp.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class CleaningService : ICleaningService
        {
            private readonly AppDbContext _context;
            private readonly ILogger<CleaningService> _logger;

            public CleaningService(AppDbContext context, ILogger<CleaningService> logger)
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            #region CRUD Operations

            public async Task<IEnumerable<CleaningTaskDto>> GetAllTasksAsync()
            {
                try
                {
                    var tasks = await _context.CleaningTasks
                        .Include(t => t.Room)
                            .ThenInclude(r => r.RoomType)
                        .Include(t => t.AssignedTo)
                        .AsNoTracking()
                        .ToListAsync();

                    return tasks.Select(t => MapToCleaningTaskDto(t));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving all cleaning tasks");
                    throw;
                }
            }

            public async Task<CleaningTaskDto> GetTaskByIdAsync(int id)
            {
                try
                {
                    var task = await _context.CleaningTasks
                        .Include(t => t.Room)
                            .ThenInclude(r => r.RoomType)
                        .Include(t => t.AssignedTo)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == id);

                    if (task == null)
                        return null;

                    return MapToCleaningTaskDto(task);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving cleaning task with ID {TaskId}", id);
                    throw;
                }
            }

            public async Task<CleaningTaskDto> CreateTaskAsync(CreateCleaningTaskDto taskDto)
            {
                try
                {
                    // Validate room
                    var room = await _context.Rooms
                        .Include(r => r.RoomType)
                        .FirstOrDefaultAsync(r => r.Id == taskDto.RoomId);

                    if (room == null)
                    {
                        throw new KeyNotFoundException($"Room with ID {taskDto.RoomId} not found");
                    }

                    // Generate task ID
                    string taskId = GenerateTaskId(taskDto.RoomId);

                    // Create cleaning task
                    var task = new CleaningTask
                    {
                        TaskId = taskId,
                        RoomId = taskDto.RoomId,
                        Description = taskDto.Description,
                        Status = taskDto.Status,
                        Priority = taskDto.Priority,
                        CreatedAt = DateTime.UtcNow,
                        AssignedToId = taskDto.AssignedToId ?? 0, // 0 means unassigned
                        CompletedAt = null,
                        CompletionNotes = null
                    };

                    // If assigned to someone, validate user
                    if (taskDto.AssignedToId.HasValue && taskDto.AssignedToId.Value > 0)
                    {
                        var user = await _context.Users.FindAsync(taskDto.AssignedToId.Value);
                        if (user == null)
                        {
                            throw new KeyNotFoundException($"User with ID {taskDto.AssignedToId.Value} not found");
                        }
                        task.AssignedToId = taskDto.AssignedToId.Value;
                    }

                    // Mark the room as needing cleaning
                    room.NeedsCleaning = true;

                    // Add task to database
                    _context.CleaningTasks.Add(task);
                    await _context.SaveChangesAsync();

                    return await GetTaskByIdAsync(task.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating cleaning task for room {RoomId}", taskDto.RoomId);
                    throw;
                }
            }

            public async Task<CleaningTaskDto> UpdateTaskAsync(int id, UpdateCleaningTaskDto taskDto)
            {
                try
                {
                    var task = await _context.CleaningTasks.FindAsync(id);
                    if (task == null)
                    {
                        throw new KeyNotFoundException($"Cleaning task with ID {id} not found");
                    }

                    // Update properties if provided
                    if (!string.IsNullOrEmpty(taskDto.Description))
                    {
                        task.Description = taskDto.Description;
                    }

                    if (taskDto.Status.HasValue)
                    {
                        // If changing to Cleaned status, set completed date
                        if (taskDto.Status.Value == CleaningRequestStatus.Cleaned && task.Status != CleaningRequestStatus.Cleaned)
                        {
                            task.CompletedAt = DateTime.UtcNow;

                            // Update the room's cleaning status
                            var room = await _context.Rooms.FindAsync(task.RoomId);
                            if (room != null)
                            {
                                room.NeedsCleaning = false;
                                room.LastCleaned = DateTime.UtcNow;
                                room.CleanedById = task.AssignedToId;
                            }
                        }
                        task.Status = taskDto.Status.Value;
                    }

                    if (taskDto.Priority.HasValue)
                    {
                        task.Priority = taskDto.Priority.Value;
                    }

                    if (taskDto.AssignedToId.HasValue)
                    {
                        // Validate user
                        if (taskDto.AssignedToId.Value > 0)
                        {
                            var user = await _context.Users.FindAsync(taskDto.AssignedToId.Value);
                            if (user == null)
                            {
                                throw new KeyNotFoundException($"User with ID {taskDto.AssignedToId.Value} not found");
                            }
                        }
                        task.AssignedToId = taskDto.AssignedToId.Value;
                    }

                    if (taskDto.ScheduledFor.HasValue)
                    {
                        // Implement if you add ScheduledFor property to model
                    }

                    if (!string.IsNullOrEmpty(taskDto.Notes))
                    {
                        // Implement if you add Notes property to model
                    }

                    if (taskDto.CompletedAt.HasValue)
                    {
                        task.CompletedAt = taskDto.CompletedAt;
                    }

                    if (!string.IsNullOrEmpty(taskDto.CompletionNotes))
                    {
                        task.CompletionNotes = taskDto.CompletionNotes;
                    }

                    await _context.SaveChangesAsync();
                    return await GetTaskByIdAsync(id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating cleaning task with ID {TaskId}", id);
                    throw;
                }
            }

            public async Task<bool> DeleteTaskAsync(int id)
            {
                try
                {
                    var task = await _context.CleaningTasks.FindAsync(id);
                    if (task == null)
                    {
                        return false;
                    }

                    _context.CleaningTasks.Remove(task);
                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting cleaning task with ID {TaskId}", id);
                    throw;
                }
            }

            #endregion

            #region Specialized Operations

            public async Task<IEnumerable<CleaningTaskDto>> GetTasksByRoomAsync(int roomId)
            {
                try
                {
                    var tasks = await _context.CleaningTasks
                        .Include(t => t.Room)
                            .ThenInclude(r => r.RoomType)
                        .Include(t => t.AssignedTo)
                        .Where(t => t.RoomId == roomId)
                        .AsNoTracking()
                        .ToListAsync();

                    return tasks.Select(t => MapToCleaningTaskDto(t));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving cleaning tasks for room {RoomId}", roomId);
                    throw;
                }
            }

            public async Task<IEnumerable<CleaningTaskDto>> GetTasksByCleanerAsync(int cleanerId)
            {
                try
                {
                    var tasks = await _context.CleaningTasks
                        .Include(t => t.Room)
                            .ThenInclude(r => r.RoomType)
                        .Include(t => t.AssignedTo)
                        .Where(t => t.AssignedToId == cleanerId)
                        .AsNoTracking()
                        .ToListAsync();

                    return tasks.Select(t => MapToCleaningTaskDto(t));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving cleaning tasks for cleaner {CleanerId}", cleanerId);
                    throw;
                }
            }

            public async Task<IEnumerable<CleaningTaskDto>> GetTasksByStatusAsync(CleaningRequestStatus status)
            {
                try
                {
                    var tasks = await _context.CleaningTasks
                        .Include(t => t.Room)
                            .ThenInclude(r => r.RoomType)
                        .Include(t => t.AssignedTo)
                        .Where(t => t.Status == status)
                        .AsNoTracking()
                        .ToListAsync();

                    return tasks.Select(t => MapToCleaningTaskDto(t));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving cleaning tasks with status {Status}", status);
                    throw;
                }
            }

            public async Task<IEnumerable<CleaningTaskDto>> GetTasksByPriorityAsync(Priority priority)
            {
                try
                {
                    var tasks = await _context.CleaningTasks
                        .Include(t => t.Room)
                            .ThenInclude(r => r.RoomType)
                        .Include(t => t.AssignedTo)
                        .Where(t => t.Priority == priority)
                        .AsNoTracking()
                        .ToListAsync();

                    return tasks.Select(t => MapToCleaningTaskDto(t));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving cleaning tasks with priority {Priority}", priority);
                    throw;
                }
            }

            public async Task<IEnumerable<CleaningTaskDto>> GetTasksByDateRangeAsync(DateTime start, DateTime end)
            {
                try
                {
                    var tasks = await _context.CleaningTasks
                        .Include(t => t.Room)
                            .ThenInclude(r => r.RoomType)
                        .Include(t => t.AssignedTo)
                        .Where(t => t.CreatedAt >= start && t.CreatedAt <= end)
                        .AsNoTracking()
                        .ToListAsync();

                    return tasks.Select(t => MapToCleaningTaskDto(t));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving cleaning tasks for date range {StartDate} to {EndDate}", start, end);
                    throw;
                }
            }

            #endregion

            #region Business Operations

            public async Task<bool> AssignTaskToCleanerAsync(int taskId, int cleanerId)
            {
                try
                {
                    var task = await _context.CleaningTasks.FindAsync(taskId);
                    if (task == null)
                    {
                        throw new KeyNotFoundException($"Cleaning task with ID {taskId} not found");
                    }

                    // Validate cleaner
                    if (cleanerId > 0)
                    {
                        var cleaner = await _context.Users.FindAsync(cleanerId);
                        if (cleaner == null)
                        {
                            throw new KeyNotFoundException($"User with ID {cleanerId} not found");
                        }
                        task.AssignedToId = cleanerId;
                    }
                    else
                    {
                        task.AssignedToId = 0; // Unassign
                    }

                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error assigning cleaner {CleanerId} to task {TaskId}", cleanerId, taskId);
                    throw;
                }
            }

            public async Task<bool> CompleteTaskAsync(int taskId, string notes)
            {
                try
                {
                    var task = await _context.CleaningTasks.FindAsync(taskId);
                    if (task == null)
                    {
                        throw new KeyNotFoundException($"Cleaning task with ID {taskId} not found");
                    }

                    task.Status = CleaningRequestStatus.Cleaned;
                    task.CompletedAt = DateTime.UtcNow;
                    task.CompletionNotes = notes;

                    // Update room status
                    var room = await _context.Rooms.FindAsync(task.RoomId);
                    if (room != null)
                    {
                        room.NeedsCleaning = false;
                        room.LastCleaned = DateTime.UtcNow;
                        room.CleanedById = task.AssignedToId;

                        // If room is not occupied, set status to Available
                        if (room.Status != RoomStatus.Occupied)
                        {
                            room.Status = RoomStatus.Available;
                        }
                    }

                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error completing cleaning task with ID {TaskId}", taskId);
                    throw;
                }
            }

            public async Task<bool> StartTaskAsync(int taskId)
            {
                try
                {
                    var task = await _context.CleaningTasks.FindAsync(taskId);
                    if (task == null)
                    {
                        throw new KeyNotFoundException($"Cleaning task with ID {taskId} not found");
                    }

                    task.Status = CleaningRequestStatus.InProgress;
                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting cleaning task with ID {TaskId}", taskId);
                    throw;
                }
            }

            public async Task<CleaningTaskDto> CreateTaskForRoomAsync(int roomId, Priority priority, string description = null)
            {
                try
                {
                    // Check if room exists
                    var room = await _context.Rooms
                        .Include(r => r.RoomType)
                        .FirstOrDefaultAsync(r => r.Id == roomId);

                    if (room == null)
                    {
                        throw new KeyNotFoundException($"Room with ID {roomId} not found");
                    }

                    // Create cleaning task
                    var taskDto = new CreateCleaningTaskDto
                    {
                        RoomId = roomId,
                        Description = description ?? $"Cleaning for Room {room.RoomNumber}",
                        Status = CleaningRequestStatus.Dirty,
                        Priority = priority
                    };

                    return await CreateTaskAsync(taskDto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating cleaning task for room with ID {RoomId}", roomId);
                    throw;
                }
            }

            public async Task<bool> CreateTasksForCheckoutAsync(DateTime date)
            {
                try
                {
                    // Get all reservations with checkout date of today
                    var checkouts = await _context.Reservations
                        .Include(r => r.Room)
                        .Where(r => r.CheckOutDate.Date == date.Date &&
                                   r.Status == ReservationStatus.CheckedIn &&
                                   r.RoomId.HasValue)
                        .Select(r => r.RoomId.Value)
                        .Distinct()
                        .ToListAsync();

                    foreach (var roomId in checkouts)
                    {
                        // Create cleaning task
                        await CreateTaskForRoomAsync(roomId, Priority.High,
                            "Post-checkout cleaning required");
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating cleaning tasks for checkouts on {Date}", date);
                    throw;
                }
            }

            public async Task<Dictionary<string, int>> GetTasksStatsByStatusAsync()
            {
                try
                {
                    return await _context.CleaningTasks
                        .GroupBy(t => t.Status)
                        .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                        .ToDictionaryAsync(g => g.Status, g => g.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving cleaning tasks stats by status");
                    throw;
                }
            }

            public async Task<Dictionary<string, int>> GetTasksStatsByPriorityAsync()
            {
                try
                {
                    return await _context.CleaningTasks
                        .GroupBy(t => t.Priority)
                        .Select(g => new { Priority = g.Key.ToString(), Count = g.Count() })
                        .ToDictionaryAsync(g => g.Priority, g => g.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving cleaning tasks stats by priority");
                    throw;
                }
            }

            public async Task<Dictionary<int, int>> GetTasksStatsByCleanerAsync()
            {
                try
                {
                    return await _context.CleaningTasks
                        .Where(t => t.AssignedToId > 0)
                        .GroupBy(t => t.AssignedToId)
                        .Select(g => new { CleanerId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(g => g.CleanerId, g => g.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving cleaning tasks stats by cleaner");
                    throw;
                }
            }

            public async Task<CleaningTasksStatisticsDto> GetCleaningTasksStatisticsAsync()
            {
                try
                {
                    // Get total tasks
                    var totalTasks = await _context.CleaningTasks.CountAsync();

                    // Get tasks by status
                    var completedTasks = await _context.CleaningTasks
                        .CountAsync(t => t.Status == CleaningRequestStatus.Cleaned);

                    var pendingTasks = await _context.CleaningTasks
                        .CountAsync(t => t.Status == CleaningRequestStatus.Dirty);

                    var inProgressTasks = await _context.CleaningTasks
                        .CountAsync(t => t.Status == CleaningRequestStatus.InProgress);

                    // Get tasks by priority
                    var tasksByPriority = await _context.CleaningTasks
                        .GroupBy(t => t.Priority)
                        .Select(g => new { Priority = g.Key.ToString(), Count = g.Count() })
                        .ToDictionaryAsync(g => g.Priority, g => g.Count);

                    // Get tasks by status
                    var tasksByStatus = await _context.CleaningTasks
                        .GroupBy(t => t.Status)
                        .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                        .ToDictionaryAsync(g => g.Status, g => g.Count);

                    // Get tasks by housekeeper
                    var tasksByHousekeeper = await _context.CleaningTasks
                        .Where(t => t.AssignedToId > 0)
                        .GroupBy(t => t.AssignedToId)
                        .Select(g => new
                        {
                            HousekeeperId = g.Key,
                            HousekeeperName = _context.Users
                                .Where(u => u.Id == g.Key)
                                .Select(u => u.FirstName + " " + u.LastName)
                                .FirstOrDefault() ?? "Unknown",
                            Count = g.Count()
                        })
                        .ToDictionaryAsync(g => g.HousekeeperName, g => g.Count);

                    // Calculate average completion time
                    var completedTasksWithTime = await _context.CleaningTasks
                        .Where(t => t.Status == CleaningRequestStatus.Cleaned && t.CompletedAt.HasValue)
                        .Select(t => new
                        {
                            CompletionTimeMinutes = (t.CompletedAt.Value - t.CreatedAt).TotalMinutes
                        })
                        .ToListAsync();

                    double averageCompletionTime = completedTasksWithTime.Any()
                        ? completedTasksWithTime.Average(t => t.CompletionTimeMinutes)
                        : 0;

                    // Calculate average time by housekeeper
                    var averageTimeByHousekeeper = await _context.CleaningTasks
                        .Where(t => t.Status == CleaningRequestStatus.Cleaned &&
                                   t.CompletedAt.HasValue &&
                                   t.AssignedToId > 0)
                        .GroupBy(t => t.AssignedToId)
                        .Select(g => new
                        {
                            HousekeeperName = _context.Users
                                .Where(u => u.Id == g.Key)
                                .Select(u => u.FirstName + " " + u.LastName)
                                .FirstOrDefault() ?? "Unknown",
                            AverageTimeMinutes = g.Average(t => (t.CompletedAt.Value - t.CreatedAt).TotalMinutes)
                        })
                        .ToDictionaryAsync(g => g.HousekeeperName, g => g.AverageTimeMinutes);

                    return new CleaningTasksStatisticsDto
                    {
                        TotalTasks = totalTasks,
                        CompletedTasks = completedTasks,
                        PendingTasks = pendingTasks,
                        InProgressTasks = inProgressTasks,
                        AverageCompletionTime = Math.Round(averageCompletionTime, 2),
                        TasksByStatus = tasksByStatus,
                        TasksByPriority = tasksByPriority,
                        TasksByHousekeeper = tasksByHousekeeper,
                        AverageTimeByHousekeeper = averageTimeByHousekeeper
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving cleaning tasks statistics");
                    throw;
                }
            }

            #endregion

            #region Helper Methods

            private CleaningTaskDto MapToCleaningTaskDto(CleaningTask task)
            {
                if (task == null)
                    return null;

                var timeToComplete = task.CompletedAt.HasValue
                    ? task.CompletedAt.Value - task.CreatedAt
                    : (TimeSpan?)null;

                return new CleaningTaskDto
                {
                    Id = task.Id,
                    TaskId = task.TaskId,
                    RoomId = task.RoomId,
                    RoomNumber = task.Room?.RoomNumber,
                    Floor = task.Room?.Floor ?? 0,
                    RoomTypeName = task.Room?.RoomType?.Name,
                    Description = task.Description,
                    Status = task.Status.ToString(),
                    Priority = task.Priority.ToString(),
                    CreatedAt = task.CreatedAt,
                    AssignedToId = task.AssignedToId > 0 ? task.AssignedToId : null,
                    AssignedToName = task.AssignedTo != null
                        ? $"{task.AssignedTo.FirstName} {task.AssignedTo.LastName}"
                        : null,
                    CompletedAt = task.CompletedAt,
                    CompletionNotes = task.CompletionNotes,
                    TimeToComplete = timeToComplete
                };
            }

            private string GenerateTaskId(int roomId)
            {
                // Format: CLN-ROOMID-YYYYMMDD-COUNT
                var today = DateTime.UtcNow.ToString("yyyyMMdd");

                // Count how many cleaning tasks were created for this room today
                int tasksCount = _context.CleaningTasks
                    .Count(t => t.RoomId == roomId &&
                               t.CreatedAt.Date == DateTime.UtcNow.Date);

                return $"CLN-{roomId:D3}-{today}-{tasksCount + 1:D2}";
            }

            #endregion
        }
    


}
