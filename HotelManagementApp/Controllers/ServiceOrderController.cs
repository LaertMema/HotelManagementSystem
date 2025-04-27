using HotelManagementApp.Models.DTOs.ServiceOrder;
using HotelManagementApp.Models.Enums;
using HotelManagementApp.Services.ServiceOrder;
using HotelManagementApp.Services.ReservationServiceSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServiceOrderController : ControllerBase
    {
        private readonly ServiceOrderService _serviceOrderService;
        private readonly ReservationService _reservationService;
        private readonly ILogger<ServiceOrderController> _logger;

        public ServiceOrderController(ServiceOrderService serviceOrderService, ILogger<ServiceOrderController> logger, ReservationService reservationService)
        {
            _serviceOrderService = serviceOrderService ?? throw new ArgumentNullException(nameof(serviceOrderService));
            _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Receptionist,Staff")]
        public async Task<ActionResult<IEnumerable<ServiceOrderDto>>> GetAllServiceOrders()
        {
            try
            {
                var serviceOrders = await _serviceOrderService.GetAllServiceOrdersAsync();
                return Ok(serviceOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all service orders");
                return StatusCode(500, "An error occurred while retrieving service orders");
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Manager,Receptionist,Staff,Guest")]
        public async Task<ActionResult<ServiceOrderDto>> GetServiceOrderById(int id)
        {
            try
            {
                var serviceOrder = await _serviceOrderService.GetServiceOrderByIdAsync(id);
                if (serviceOrder == null)
                {
                    return NotFound($"Service order with ID {id} not found");
                }

                // If guest role, verify the order belongs to their reservation
                if (User.IsInRole("Guest"))
                {
                    var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                    if (serviceOrder.GuestId != userId)
                    {
                        return Forbid();
                    }
                }

                return Ok(serviceOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service order with ID {ServiceOrderId}", id);
                return StatusCode(500, "An error occurred while retrieving the service order");
            }
        }

        [HttpGet("reservation/{reservationId}")]
        [Authorize(Roles = "Admin,Manager,Receptionist,Staff,Guest")]
        public async Task<ActionResult<IEnumerable<ServiceOrderDto>>> GetServiceOrdersByReservation(int reservationId)
        {
            try
            {
                // If guest role, verify the reservation belongs to them
                if (User.IsInRole("Guest"))
                {
                    var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                    // Get the reservation first to check ownership
                    // This would be better handled through a reservation service
                    var reservation = await _reservationService.GetReservationByIdAsync(reservationId);
                    if (reservation == null || reservation.UserId != userId)
                    {
                        return Forbid();
                    }
                }

                var serviceOrders = await _serviceOrderService.GetServiceOrdersByReservationAsync(reservationId);
                return Ok(serviceOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service orders for reservation {ReservationId}", reservationId);
                return StatusCode(500, "An error occurred while retrieving service orders");
            }
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin,Manager,Receptionist,Staff")]
        public async Task<ActionResult<IEnumerable<ServiceOrderDto>>> GetServiceOrdersByStatus(ServiceOrderStatus status)
        {
            try
            {
                var serviceOrders = await _serviceOrderService.GetServiceOrdersByStatusAsync(status);
                return Ok(serviceOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service orders with status {Status}", status);
                return StatusCode(500, "An error occurred while retrieving service orders");
            }
        }

        [HttpGet("daterange")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<IEnumerable<ServiceOrderDto>>> GetServiceOrdersByDateRange(
            [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            try
            {
                if (start > end)
                {
                    return BadRequest("Start date must be before end date");
                }

                var serviceOrders = await _serviceOrderService.GetServiceOrdersByDateRangeAsync(start, end);
                return Ok(serviceOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service orders between {StartDate} and {EndDate}", start, end);
                return StatusCode(500, "An error occurred while retrieving service orders");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Receptionist,Guest")]
        public async Task<ActionResult<ServiceOrderDto>> CreateServiceOrder(CreateServiceOrderDto serviceOrderDto)
        {
            try
            {
                // If guest role, verify the reservation belongs to them
                if (User.IsInRole("Guest"))
                {
                    var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                    // Get the reservation first to check ownership
                    var reservation = await _reservationService.GetReservationByIdAsync(serviceOrderDto.ReservationId);
                    if (reservation == null || reservation.UserId != userId)
                    {
                        return Forbid();
                    }
                }

                var serviceOrder = await _serviceOrderService.CreateServiceOrderAsync(serviceOrderDto);
                return CreatedAtAction(nameof(GetServiceOrderById), new { id = serviceOrder.Id }, serviceOrder);
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
                _logger.LogError(ex, "Error creating service order");
                return StatusCode(500, "An error occurred while creating the service order");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager,Receptionist,Staff")]
        public async Task<ActionResult<ServiceOrderDto>> UpdateServiceOrder(int id, UpdateServiceOrderDto serviceOrderDto)
        {
            try
            {
                var serviceOrder = await _serviceOrderService.UpdateServiceOrderAsync(id, serviceOrderDto);
                return Ok(serviceOrder);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Service order not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service order with ID {ServiceOrderId}", id);
                return StatusCode(500, "An error occurred while updating the service order");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteServiceOrder(int id)
        {
            try
            {
                var result = await _serviceOrderService.DeleteServiceOrderAsync(id);
                if (!result)
                {
                    return NotFound($"Service order with ID {id} not found");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service order with ID {ServiceOrderId}", id);
                return StatusCode(500, "An error occurred while deleting the service order");
            }
        }

        [HttpPost("{id}/complete")]
        [Authorize(Roles = "Admin,Manager,Staff")]
        public async Task<IActionResult> CompleteServiceOrder(int id, [FromBody] CompleteServiceOrderDto completeDto)
        {
            try
            {
                var staffId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                var result = await _serviceOrderService.CompleteServiceOrderAsync(id, completeDto.Notes, staffId);
                if (!result)
                {
                    return NotFound($"Service order with ID {id} not found");
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
                _logger.LogError(ex, "Error completing service order with ID {ServiceOrderId}", id);
                return StatusCode(500, "An error occurred while completing the service order");
            }
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Admin,Manager,Receptionist,Guest")]
        public async Task<IActionResult> CancelServiceOrder(int id, [FromBody] CancelServiceOrderDto cancelDto)
        {
            try
            {
                // If guest role, verify the order belongs to their reservation
                if (User.IsInRole("Guest"))
                {
                    var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                    var serviceOrder = await _serviceOrderService.GetServiceOrderByIdAsync(id);
                    if (serviceOrder == null || serviceOrder.GuestId != userId)
                    {
                        return Forbid();
                    }
                }

                var result = await _serviceOrderService.CancelServiceOrderAsync(id, cancelDto.Reason);
                if (!result)
                {
                    return NotFound($"Service order with ID {id} not found");
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
                _logger.LogError(ex, "Error cancelling service order with ID {ServiceOrderId}", id);
                return StatusCode(500, "An error occurred while cancelling the service order");
            }
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<ServiceOrderStatisticsDto>> GetServiceOrderStats()
        {
            try
            {
                var stats = await _serviceOrderService.GetServiceOrderStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service order statistics");
                return StatusCode(500, "An error occurred while retrieving service order statistics");
            }
        }

        [HttpGet("stats/bytype")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<Dictionary<string, int>>> GetServiceOrderStatsByType()
        {
            try
            {
                var stats = await _serviceOrderService.GetServiceOrderStatsByTypeAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service order statistics by type");
                return StatusCode(500, "An error occurred while retrieving service order statistics");
            }
        }
    }

    // Additional DTOs for actions that need specific data
    public class CompleteServiceOrderDto
    {
        public string Notes { get; set; }
    }

    public class CancelServiceOrderDto
    {
        public string Reason { get; set; }
    }
}

