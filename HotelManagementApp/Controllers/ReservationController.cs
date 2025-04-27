using HotelManagementApp.Models;
using HotelManagementApp.Models.DTOs;
using HotelManagementApp.Models.Enums;
using HotelManagementApp.Services;
using HotelManagementApp.Services.ReservationServiceSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReservationController : ControllerBase
    {
        private readonly IReservationService _reservationService;
        private readonly ILogger<ReservationController> _logger;

        public ReservationController(IReservationService reservationService, ILogger<ReservationController> logger)
        {
            _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetAllReservations()
        {
            try
            {
                var reservations = await _reservationService.GetAllReservationsAsync();
                return Ok(reservations.ToReservationDtos());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all reservations");
                return StatusCode(500, "An error occurred while retrieving reservations");
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Manager,Receptionist,Guest")]
        public async Task<ActionResult<ReservationDto>> GetReservationById(int id)
        {
            try
            {
                var reservation = await _reservationService.GetReservationByIdAsync(id);
                if (reservation == null)
                {
                    return NotFound($"Reservation with ID {id} not found");
                }

                // If guest role, check if reservation belongs to user
                if (User.IsInRole("Guest"))
                {
                    var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                    if (reservation.UserId != userId)
                    {
                        return Forbid();
                    }
                }

                return Ok(reservation.ToReservationDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservation with ID {ReservationId}", id);
                return StatusCode(500, "An error occurred while retrieving the reservation");
            }
        }

        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin,Manager,Receptionist,Guest")]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservationsByUser(int userId)
        {
            try
            {
                // If guest role, check if user is requesting own reservations
                if (User.IsInRole("Guest"))
                {
                    var currentUserId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                    if (userId != currentUserId)
                    {
                        return Forbid();
                    }
                }

                var reservations = await _reservationService.GetReservationsByUserAsync(userId);
                return Ok(reservations.ToReservationDtos());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservations for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving the reservations");
            }
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservationsByStatus(ReservationStatus status)
        {
            try
            {
                var reservations = await _reservationService.GetReservationsByStatusAsync(status);
                return Ok(reservations.ToReservationDtos());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservations with status {Status}", status);
                return StatusCode(500, "An error occurred while retrieving the reservations");
            }
        }

        [HttpGet("daterange")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservationsByDateRange(
            [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            try
            {
                if (start > end)
                {
                    return BadRequest("Start date must be before end date");
                }

                var reservations = await _reservationService.GetReservationsByDateRangeAsync(start, end);
                return Ok(reservations.ToReservationDtos());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservations between {StartDate} and {EndDate}", start, end);
                return StatusCode(500, "An error occurred while retrieving the reservations");
            }
        }

        [HttpGet("room/{roomId}")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservationsByRoom(int roomId)
        {
            try
            {
                var reservations = await _reservationService.GetReservationsByRoomAsync(roomId);
                return Ok(reservations.ToReservationDtos());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservations for room {RoomId}", roomId);
                return StatusCode(500, "An error occurred while retrieving the reservations");
            }
        }

        [HttpGet("today/arrivals")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetTodayArrivals()
        {
            try
            {
                var arrivals = await _reservationService.GetTodayArrivalsAsync();
                return Ok(arrivals.ToReservationDtos());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving today's arrivals");
                return StatusCode(500, "An error occurred while retrieving today's arrivals");
            }
        }

        [HttpGet("today/departures")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetTodayDepartures()
        {
            try
            {
                var departures = await _reservationService.GetTodayDeparturesAsync();
                return Ok(departures.ToReservationDtos());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving today's departures");
                return StatusCode(500, "An error occurred while retrieving today's departures");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Receptionist,Guest")]
        public async Task<ActionResult<ReservationDto>> CreateReservation(CreateReservationDto createReservationDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");

                // Generate reservation number
                var reservationNumber = $"RES-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

                // Calculate total price (simplified, should be more complex in real implementation)
                var reservation = createReservationDto.ToReservationModel(userId, reservationNumber, 0); // Price will be calculated in service

                var createdReservation = await _reservationService.CreateReservationAsync(reservation);
                return CreatedAtAction(nameof(GetReservationById), new { id = createdReservation.Id }, createdReservation.ToReservationDto());
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reservation");
                return StatusCode(500, "An error occurred while creating the reservation");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<ActionResult<ReservationDto>> UpdateReservation(int id, UpdateReservationDto updateReservationDto)
        {
            try
            {
                var reservation = await _reservationService.GetReservationByIdAsync(id);
                if (reservation == null)
                {
                    return NotFound($"Reservation with ID {id} not found");
                }

                // Apply updates
                reservation.ApplyUpdateReservationDto(updateReservationDto);

                var updatedReservation = await _reservationService.UpdateReservationAsync(reservation);
                return Ok(updatedReservation.ToReservationDto());
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Reservation not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reservation with ID {ReservationId}", id);
                return StatusCode(500, "An error occurred while updating the reservation");
            }
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Admin,Manager,Receptionist,Guest")]
        public async Task<IActionResult> CancelReservation(int id, [FromBody] CancelReservationDto cancelDto)
        {
            try
            {
                // If guest role, check if reservation belongs to user
                if (User.IsInRole("Guest"))
                {
                    var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                    var reservation = await _reservationService.GetReservationByIdAsync(id);
                    if (reservation == null || reservation.UserId != userId)
                    {
                        return Forbid();
                    }
                }

                var result = await _reservationService.CancelReservationAsync(id, cancelDto.CancellationReason);
                if (!result)
                {
                    return NotFound($"Reservation with ID {id} not found");
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
                _logger.LogError(ex, "Error cancelling reservation with ID {ReservationId}", id);
                return StatusCode(500, "An error occurred while cancelling the reservation");
            }
        }

        [HttpPost("{id}/checkin")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> CheckInGuest(int id, [FromBody] CheckInDto checkInDto)
        {
            try
            {
                var receptionistId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                var result = await _reservationService.CheckInAsync(id, checkInDto.RoomId, receptionistId);
                if (!result)
                {
                    return NotFound($"Reservation with ID {id} not found");
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
                _logger.LogError(ex, "Error checking in guest for reservation {ReservationId}", id);
                return StatusCode(500, "An error occurred while checking in the guest");
            }
        }

        [HttpPost("{id}/checkout")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> CheckOutGuest(int id)
        {
            try
            {
                var receptionistId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                var result = await _reservationService.CheckOutAsync(id, receptionistId);
                if (!result)
                {
                    return NotFound($"Reservation with ID {id} not found");
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
                _logger.LogError(ex, "Error checking out guest for reservation {ReservationId}", id);
                return StatusCode(500, "An error occurred while checking out the guest");
            }
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<Dictionary<string, int>>> GetReservationStats()
        {
            try
            {
                var stats = await _reservationService.GetReservationStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservation statistics");
                return StatusCode(500, "An error occurred while retrieving reservation statistics");
            }
        }

        [HttpGet("forecast")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<Dictionary<DateTime, int>>> GetReservationForecast(
            [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            try
            {
                if (start > end)
                {
                    return BadRequest("Start date must be before end date");
                }

                var forecast = await _reservationService.GetReservationForecastAsync(start, end);
                return Ok(forecast);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservation forecast");
                return StatusCode(500, "An error occurred while retrieving reservation forecast");
            }
        }
    }
}

