using HotelManagementApp.Models.DTOs.MaintenanceRequest;
using HotelManagementApp.Models.Enums;
using HotelManagementApp.Services.MaintenanceRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MaintenanceRequestController : ControllerBase
    {
        private readonly IMaintenanceRequestService _maintenanceService;
        private readonly ILogger<MaintenanceRequestController> _logger;

        public MaintenanceRequestController(IMaintenanceRequestService maintenanceService, ILogger<MaintenanceRequestController> logger)
        {
            _maintenanceService = maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Maintenance")]
        public async Task<ActionResult<IEnumerable<MaintenanceRequestDto>>> GetAllMaintenanceRequests()
        {
            try
            {
                var requests = await _maintenanceService.GetAllMaintenanceRequestsAsync();
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all maintenance requests");
                return StatusCode(500, "An error occurred while retrieving maintenance requests");
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Manager,Maintenance,Staff")]
        public async Task<ActionResult<MaintenanceRequestDto>> GetMaintenanceRequestById(int id)
        {
            try
            {
                var request = await _maintenanceService.GetMaintenanceRequestByIdAsync(id);
                if (request == null)
                {
                    return NotFound($"Maintenance request with ID {id} not found");
                }
                return Ok(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving maintenance request with ID {RequestId}", id);
                return StatusCode(500, "An error occurred while retrieving the maintenance request");
            }
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin,Manager,Maintenance")]
        public async Task<ActionResult<IEnumerable<MaintenanceRequestDto>>> GetMaintenanceRequestsByStatus(MaintenanceRequestStatus status)
        {
            try
            {
                var requests = await _maintenanceService.GetMaintenanceRequestsByStatusAsync(status);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving maintenance requests with status {Status}", status);
                return StatusCode(500, "An error occurred while retrieving maintenance requests");
            }
        }

        [HttpGet("room/{roomId}")]
        [Authorize(Roles = "Admin,Manager,Maintenance,Staff")]
        public async Task<ActionResult<IEnumerable<MaintenanceRequestDto>>> GetMaintenanceRequestsByRoom(int roomId)
        {
            try
            {
                var requests = await _maintenanceService.GetMaintenanceRequestsByRoomAsync(roomId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving maintenance requests for room {RoomId}", roomId);
                return StatusCode(500, "An error occurred while retrieving maintenance requests");
            }
        }

        [HttpGet("assigned/{userId}")]
        [Authorize(Roles = "Admin,Manager,Maintenance")]
        public async Task<ActionResult<IEnumerable<MaintenanceRequestDto>>> GetMaintenanceRequestsAssignedToUser(int userId)
        {
            try
            {
                // If maintenance staff, only allow viewing own tasks
                if (User.IsInRole("Maintenance"))
                {
                    var currentUserId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                    if (userId != currentUserId)
                    {
                        return Forbid();
                    }
                }

                var requests = await _maintenanceService.GetMaintenanceRequestsAssignedToUserAsync(userId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving maintenance requests assigned to user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving maintenance requests");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Staff,Receptionist")]
        public async Task<ActionResult<MaintenanceRequestDto>> CreateMaintenanceRequest(CreateMaintenanceRequestDto createRequestDto)
        {
            try
            {
                var reportedById = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                var request = await _maintenanceService.CreateMaintenanceRequestAsync(createRequestDto, reportedById);
                return CreatedAtAction(nameof(GetMaintenanceRequestById), new { id = request.Id }, request);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating maintenance request");
                return StatusCode(500, "An error occurred while creating the maintenance request");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager,Maintenance")]
        public async Task<ActionResult<MaintenanceRequestDto>> UpdateMaintenanceRequest(int id, UpdateMaintenanceRequestDto updateRequestDto)
        {
            try
            {
                // If maintenance staff, verify they are assigned to this request
                if (User.IsInRole("Maintenance"))
                {
                    var currentUserId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                    var request = await _maintenanceService.GetMaintenanceRequestByIdAsync(id);
                    if (request == null || request.AssignedTo != currentUserId)
                    {
                        return Forbid();
                    }
                }

                var updatedRequest = await _maintenanceService.UpdateMaintenanceRequestAsync(id, updateRequestDto);
                return Ok(updatedRequest);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Maintenance request not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating maintenance request with ID {RequestId}", id);
                return StatusCode(500, "An error occurred while updating the maintenance request");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteMaintenanceRequest(int id)
        {
            try
            {
                var result = await _maintenanceService.DeleteMaintenanceRequestAsync(id);
                if (!result)
                {
                    return NotFound($"Maintenance request with ID {id} not found");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting maintenance request with ID {RequestId}", id);
                return StatusCode(500, "An error occurred while deleting the maintenance request");
            }
        }

        [HttpPost("{id}/assign/{assignedToId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AssignMaintenanceRequest(int id, int assignedToId)
        {
            try
            {
                var result = await _maintenanceService.AssignMaintenanceRequestAsync(id, assignedToId);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning maintenance request {RequestId} to user {UserId}", id, assignedToId);
                return StatusCode(500, "An error occurred while assigning the maintenance request");
            }
        }

        [HttpPost("{id}/complete")]
        [Authorize(Roles = "Admin,Manager,Maintenance")]
        public async Task<IActionResult> CompleteMaintenanceRequest(int id, [FromBody] CompleteMaintenanceRequestDto completeDto)
        {
            try
            {
                // If maintenance staff, verify they are assigned to this request
                if (User.IsInRole("Maintenance"))
                {
                    var currentUserId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                    var request = await _maintenanceService.GetMaintenanceRequestByIdAsync(id);
                    if (request == null || request.AssignedTo != currentUserId)
                    {
                        return Forbid();
                    }
                }

                var result = await _maintenanceService.CompleteMaintenanceRequestAsync(id, completeDto.ResolutionNotes, completeDto.CostOfRepair);
                if (!result)
                {
                    return NotFound($"Maintenance request with ID {id} not found");
                }
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing maintenance request with ID {RequestId}", id);
                return StatusCode(500, "An error occurred while completing the maintenance request");
            }
        }
    }

    // Additional DTO for complete action
    public class CompleteMaintenanceRequestDto
    {
        public string ResolutionNotes { get; set; }
        public decimal? CostOfRepair { get; set; }
    }
}
