using HotelManagementApp.Models.DTOs.CleaningTask;
using HotelManagementApp.Models.Enums;
using HotelManagementApp.Services.CleaningTaskSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CleaningTaskController : ControllerBase
    {
        private readonly ICleaningService _cleaningService;
        private readonly ILogger<CleaningTaskController> _logger;

        public CleaningTaskController(ICleaningService cleaningService, ILogger<CleaningTaskController> logger)
        {
            _cleaningService = cleaningService ?? throw new ArgumentNullException(nameof(cleaningService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Housekeeper")]
        public async Task<ActionResult<IEnumerable<CleaningTaskDto>>> GetAllTasks()
        {
            try
            {
                var tasks = await _cleaningService.GetAllTasksAsync();
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all cleaning tasks");
                return StatusCode(500, "An error occurred while retrieving cleaning tasks");
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Manager,Housekeeper")]
        public async Task<ActionResult<CleaningTaskDto>> GetTaskById(int id)
        {
            try
            {
                var task = await _cleaningService.GetTaskByIdAsync(id);
                if (task == null)
                {
                    return NotFound($"Cleaning task with ID {id} not found");
                }
                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cleaning task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while retrieving the cleaning task");
            }
        }

        [HttpGet("room/{roomId}")]
        [Authorize(Roles = "Admin,Manager,Housekeeper")]
        public async Task<ActionResult<IEnumerable<CleaningTaskDto>>> GetTasksByRoom(int roomId)
        {
            try
            {
                var tasks = await _cleaningService.GetTasksByRoomAsync(roomId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cleaning tasks for room {RoomId}", roomId);
                return StatusCode(500, "An error occurred while retrieving cleaning tasks");
            }
        }

        [HttpGet("cleaner/{cleanerId}")]
        [Authorize(Roles = "Admin,Manager,Housekeeper")]
        public async Task<ActionResult<IEnumerable<CleaningTaskDto>>> GetTasksByCleaner(int cleanerId)
        {
            try
            {
                // If housekeeper role, only allow viewing own tasks
                if (User.IsInRole("Housekeeper"))
                {
                    var currentUserId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                    if (cleanerId != currentUserId)
                    {
                        return Forbid();
                    }
                }

                var tasks = await _cleaningService.GetTasksByCleanerAsync(cleanerId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cleaning tasks for cleaner {CleanerId}", cleanerId);
                return StatusCode(500, "An error occurred while retrieving cleaning tasks");
            }
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin,Manager,Housekeeper")]
        public async Task<ActionResult<IEnumerable<CleaningTaskDto>>> GetTasksByStatus(CleaningRequestStatus status)
        {
            try
            {
                var tasks = await _cleaningService.GetTasksByStatusAsync(status);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cleaning tasks with status {Status}", status);
                return StatusCode(500, "An error occurred while retrieving cleaning tasks");
            }
        }

        [HttpGet("priority/{priority}")]
        [Authorize(Roles = "Admin,Manager,Housekeeper")]
        public async Task<ActionResult<IEnumerable<CleaningTaskDto>>> GetTasksByPriority(Priority priority)
        {
            try
            {
                var tasks = await _cleaningService.GetTasksByPriorityAsync(priority);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cleaning tasks with priority {Priority}", priority);
                return StatusCode(500, "An error occurred while retrieving cleaning tasks");
            }
        }

        [HttpGet("daterange")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<IEnumerable<CleaningTaskDto>>> GetTasksByDateRange(
            [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            try
            {
                if (start > end)
                {
                    return BadRequest("Start date must be before end date");
                }

                var tasks = await _cleaningService.GetTasksByDateRangeAsync(start, end);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cleaning tasks between {StartDate} and {EndDate}", start, end);
                return StatusCode(500, "An error occurred while retrieving cleaning tasks");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<ActionResult<CleaningTaskDto>> CreateTask(CreateCleaningTaskDto taskDto)
        {
            try
            {
                var task = await _cleaningService.CreateTaskAsync(taskDto);
                return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cleaning task");
                return StatusCode(500, "An error occurred while creating the cleaning task");
            }
        }

        [HttpPost("room")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<ActionResult<CleaningTaskDto>> CreateTaskForRoom(CreateTaskForRoomDto taskDto)
        {
            try
            {
                var task = await _cleaningService.CreateTaskForRoomAsync(
                    taskDto.RoomId,
                    taskDto.Priority,
                    taskDto.Description);

                return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cleaning task for room");
                return StatusCode(500, "An error occurred while creating the cleaning task");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager,Housekeeper")]
        public async Task<ActionResult<CleaningTaskDto>> UpdateTask(int id, UpdateCleaningTaskDto taskDto)
        {
            try
            {
                var task = await _cleaningService.UpdateTaskAsync(id, taskDto);
                return Ok(task);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Cleaning task not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cleaning task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while updating the cleaning task");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                var result = await _cleaningService.DeleteTaskAsync(id);
                if (!result)
                {
                    return NotFound($"Cleaning task with ID {id} not found");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting cleaning task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while deleting the cleaning task");
            }
        }

        [HttpPost("{id}/assign/{cleanerId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AssignTaskToCleaner(int id, int cleanerId)
        {
            try
            {
                var result = await _cleaningService.AssignTaskToCleanerAsync(id, cleanerId);
                if (!result)
                {
                    return NotFound($"Cleaning task with ID {id} not found");
                }
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning cleaning task with ID {TaskId} to cleaner {CleanerId}", id, cleanerId);
                return StatusCode(500, "An error occurred while assigning the cleaning task");
            }
        }

        [HttpPost("{id}/complete")]
        [Authorize(Roles = "Admin,Manager,Housekeeper")]
        public async Task<IActionResult> CompleteTask(int id, [FromBody] CompleteCleaningTaskDto completeDto)
        {
            try
            {
                // If housekeeper role, only allow completing tasks assigned to them
                if (User.IsInRole("Housekeeper"))
                {
                    var currentUserId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                    var task = await _cleaningService.GetTaskByIdAsync(id);
                    if (task == null || task.AssignedToId != currentUserId)
                    {
                        return Forbid();
                    }
                }

                var result = await _cleaningService.CompleteTaskAsync(id, completeDto.Notes);
                if (!result)
                {
                    return NotFound($"Cleaning task with ID {id} not found");
                }
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing cleaning task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while completing the cleaning task");
            }
        }

        [HttpPost("{id}/start")]
        [Authorize(Roles = "Admin,Manager,Housekeeper")]
        public async Task<IActionResult> StartTask(int id)
        {
            try
            {
                // If housekeeper role, only allow starting tasks assigned to them
                if (User.IsInRole("Housekeeper"))
                {
                    var currentUserId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                    var task = await _cleaningService.GetTaskByIdAsync(id);
                    if (task == null || task.AssignedToId != currentUserId)
                    {
                        return Forbid();
                    }
                }

                var result = await _cleaningService.StartTaskAsync(id);
                if (!result)
                {
                    return NotFound($"Cleaning task with ID {id} not found");
                }
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting cleaning task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while starting the cleaning task");
            }
        }

        [HttpPost("checkout-tasks")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateTasksForCheckout([FromBody] CreateCheckoutTasksDto dto)
        {
            try
            {
                var result = await _cleaningService.CreateTasksForCheckoutAsync(dto.Date);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cleaning tasks for checkout on {Date}", dto.Date);
                return StatusCode(500, "An error occurred while creating cleaning tasks for checkout");
            }
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<CleaningTasksStatisticsDto>> GetCleaningTasksStatistics()
        {
            try
            {
                var stats = await _cleaningService.GetCleaningTasksStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cleaning task statistics");
                return StatusCode(500, "An error occurred while retrieving cleaning task statistics");
            }
        }

        [HttpGet("stats/status")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<Dictionary<string, int>>> GetTasksStatsByStatus()
        {
            try
            {
                var stats = await _cleaningService.GetTasksStatsByStatusAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cleaning task statistics by status");
                return StatusCode(500, "An error occurred while retrieving cleaning task statistics");
            }
        }

        [HttpGet("stats/priority")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<Dictionary<string, int>>> GetTasksStatsByPriority()
        {
            try
            {
                var stats = await _cleaningService.GetTasksStatsByPriorityAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cleaning task statistics by priority");
                return StatusCode(500, "An error occurred while retrieving cleaning task statistics");
            }
        }

        [HttpGet("stats/cleaner")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<Dictionary<int, int>>> GetTasksStatsByCleaner()
        {
            try
            {
                var stats = await _cleaningService.GetTasksStatsByCleanerAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cleaning task statistics by cleaner");
                return StatusCode(500, "An error occurred while retrieving cleaning task statistics");
            }
        }
    }

    // Additional DTOs for actions that need specific data
    public class CompleteCleaningTaskDto
    {
        public string Notes { get; set; }
    }

    public class CreateTaskForRoomDto
    {
        public int RoomId { get; set; }
        public Priority Priority { get; set; } = Priority.Medium;
        public string Description { get; set; }
    }

    public class CreateCheckoutTasksDto
    {
        public DateTime Date { get; set; } = DateTime.Today;
    }
}
