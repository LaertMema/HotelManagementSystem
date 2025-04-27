using HotelManagementApp.Models.DTOs.Room;
using HotelManagementApp.Models.Enums;
using HotelManagementApp.Services.RoomServiceSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HotelManagementApp.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoomController : ControllerBase
    {
        private readonly RoomService _roomService;
        private readonly ILogger<RoomController> _logger;

        public RoomController(RoomService roomService, ILogger<RoomController> logger)
        {
            _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<RoomDto>>> GetAllRooms()
        {
            try
            {
                var rooms = await _roomService.GetAllRoomsAsync();
                return Ok(rooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all rooms");
                return StatusCode(500, "An error occurred while retrieving rooms");
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<RoomDto>> GetRoomById(int id)
        {
            try
            {
                var room = await _roomService.GetRoomByIdAsync(id);
                if (room == null)
                {
                    return NotFound($"Room with ID {id} not found");
                }
                return Ok(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving room with ID {RoomId}", id);
                return StatusCode(500, "An error occurred while retrieving the room");
            }
        }

        [HttpGet("type/{roomTypeId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<RoomDto>>> GetRoomsByType(int roomTypeId)
        {
            try
            {
                var rooms = await _roomService.GetRoomsByTypeAsync(roomTypeId);
                return Ok(rooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rooms with type ID {RoomTypeId}", roomTypeId);
                return StatusCode(500, "An error occurred while retrieving rooms by type");
            }
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin,Manager,Receptionist,Housekeeper")]
        public async Task<ActionResult<IEnumerable<RoomDto>>> GetRoomsByStatus(RoomStatus status)
        {
            try
            {
                var rooms = await _roomService.GetRoomsByStatusAsync(status);
                return Ok(rooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rooms with status {Status}", status);
                return StatusCode(500, "An error occurred while retrieving rooms by status");
            }
        }

        [HttpGet("available")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<RoomAvailabilityDto>>> GetAvailableRooms(
            [FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut)
        {
            try
            {
                if (checkIn >= checkOut)
                {
                    return BadRequest("Check-in date must be before check-out date");
                }

                var availableRooms = await _roomService.GetAvailableRoomsAsync(checkIn, checkOut);
                return Ok(availableRooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available rooms");
                return StatusCode(500, "An error occurred while retrieving available rooms");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<RoomDto>> CreateRoom(CreateRoomDto createRoomDto)
        {
            try
            {
                var room = await _roomService.CreateRoomAsync(createRoomDto);
                return CreatedAtAction(nameof(GetRoomById), new { id = room.Id }, room);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating room");
                return StatusCode(500, "An error occurred while creating the room");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<RoomDto>> UpdateRoom(int id, UpdateRoomDto updateRoomDto)
        {
            try
            {
                var room = await _roomService.UpdateRoomAsync(id, updateRoomDto);
                return Ok(room);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Room not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating room with ID {RoomId}", id);
                return StatusCode(500, "An error occurred while updating the room");
            }
        }

        [HttpPost("{id}/status/{status}")]
        [Authorize(Roles = "Admin,Manager,Receptionist,Housekeeper")]
        public async Task<IActionResult> ChangeRoomStatus(int id, RoomStatus status)
        {
            try
            {
                var result = await _roomService.UpdateRoomStatusAsync(id, status);
                if (!result)
                {
                    return NotFound($"Room with ID {id} not found");
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
                _logger.LogError(ex, "Error changing status for room with ID {RoomId}", id);
                return StatusCode(500, "An error occurred while changing the room status");
            }
        }

        [HttpGet("occupancy")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<Dictionary<string, int>>> GetRoomOccupancyStats()
        {
            try
            {
                var stats = await _roomService.GetRoomOccupancyStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving room occupancy statistics");
                return StatusCode(500, "An error occurred while retrieving room occupancy statistics");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            try
            {
                var result = await _roomService.DeleteRoomAsync(id);
                if (!result)
                {
                    return NotFound($"Room with ID {id} not found");
                }
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting room with ID {RoomId}", id);
                return StatusCode(500, "An error occurred while deleting the room");
            }
        }
    }
}
