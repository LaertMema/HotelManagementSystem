//using HotelManagementApp.Models.DTOs;
using HotelManagementApp.Services.InvoiceSpace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace HotelManagementApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<InvoiceController> _logger;

        public InvoiceController(IInvoiceService invoiceService, ILogger<InvoiceController> logger)
        {
            _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAllInvoices()
        {
            try
            {
                var invoices = await _invoiceService.GetAllInvoicesAsync();
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all invoices");
                return StatusCode(500, "An error occurred while retrieving invoices");
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Manager,Receptionist,Guest")]
        public async Task<ActionResult<InvoiceDto>> GetInvoiceById(int id)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                if (invoice == null)
                {
                    return NotFound($"Invoice with ID {id} not found");
                }

                // If guest role, verify the invoice belongs to them
                if (User.IsInRole("Guest"))
                {
                    var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                    var reservation = await _invoiceService.GetReservationByIdAsync(invoice.ReservationId);
                    if (reservation == null || reservation.UserId != userId)
                    {
                        return Forbid();
                    }
                }

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice with ID {InvoiceId}", id);
                return StatusCode(500, "An error occurred while retrieving the invoice");
            }
        }

        [HttpGet("reservation/{reservationId}")]
        [Authorize(Roles = "Admin,Manager,Receptionist,Guest")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetInvoicesByReservation(int reservationId)
        {
            try
            {
                // If guest role, verify the reservation belongs to them
                if (User.IsInRole("Guest"))
                {
                    var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                    var reservation = await _invoiceService.GetReservationByIdAsync(reservationId);
                    if (reservation == null || reservation.UserId != userId)
                    {
                        return Forbid();
                    }
                }

                var invoices = await _invoiceService.GetInvoicesByReservationAsync(reservationId);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices for reservation {ReservationId}", reservationId);
                return StatusCode(500, "An error occurred while retrieving invoices");
            }
        }

        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin,Manager,Receptionist,Guest")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetInvoicesByUser(int userId)
        {
            try
            {
                // If guest role, verify the user is requesting their own invoices
                if (User.IsInRole("Guest"))
                {
                    var currentUserId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                    if (userId != currentUserId)
                    {
                        return Forbid();
                    }
                }

                var invoices = await _invoiceService.GetInvoicesByUserAsync(userId);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving invoices");
            }
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetInvoicesByStatus(string status)
        {
            try
            {
                var invoices = await _invoiceService.GetInvoicesByStatusAsync(status);
                return Ok(invoices);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices with status {Status}", status);
                return StatusCode(500, "An error occurred while retrieving invoices");
            }
        }

        [HttpGet("daterange")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetInvoicesByDateRange(
            [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            try
            {
                if (start > end)
                {
                    return BadRequest("Start date must be before end date");
                }

                var invoices = await _invoiceService.GetInvoicesByDateRangeAsync(start, end);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices between {StartDate} and {EndDate}", start, end);
                return StatusCode(500, "An error occurred while retrieving invoices");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<ActionResult<InvoiceDto>> CreateInvoice(CreateInvoiceDto createInvoiceDto)
        {
            try
            {
                var invoice = await _invoiceService.CreateInvoiceAsync(createInvoiceDto);
                return CreatedAtAction(nameof(GetInvoiceById), new { id = invoice.Id }, invoice);
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
                _logger.LogError(ex, "Error creating invoice");
                return StatusCode(500, "An error occurred while creating the invoice");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<ActionResult<InvoiceDto>> UpdateInvoice(int id, UpdateInvoiceDto updateInvoiceDto)
        {
            try
            {
                var invoice = await _invoiceService.UpdateInvoiceAsync(id, updateInvoiceDto);
                return Ok(invoice);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Invoice not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice with ID {InvoiceId}", id);
                return StatusCode(500, "An error occurred while updating the invoice");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            try
            {
                var result = await _invoiceService.DeleteInvoiceAsync(id);
                if (!result)
                {
                    return NotFound($"Invoice with ID {id} not found");
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
                _logger.LogError(ex, "Error deleting invoice with ID {InvoiceId}", id);
                return StatusCode(500, "An error occurred while deleting the invoice");
            }
        }

        [HttpPost("{id}/finalize")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> FinalizeInvoice(int id)
        {
            try
            {
                var result = await _invoiceService.FinalizeInvoiceAsync(id);
                if (!result)
                {
                    return NotFound($"Invoice with ID {id} not found");
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
                _logger.LogError(ex, "Error finalizing invoice with ID {InvoiceId}", id);
                return StatusCode(500, "An error occurred while finalizing the invoice");
            }
        }

        [HttpPost("{id}/send")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> SendInvoice(int id, [FromBody] SendInvoiceDto sendDto)
        {
            try
            {
                var result = await _invoiceService.SendInvoiceAsync(id, sendDto.Email);
                if (!result)
                {
                    return NotFound($"Invoice with ID {id} not found");
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
                _logger.LogError(ex, "Error sending invoice with ID {InvoiceId}", id);
                return StatusCode(500, "An error occurred while sending the invoice");
            }
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<InvoiceStatisticsDto>> GetInvoiceStats()
        {
            try
            {
                var stats = await _invoiceService.GetInvoiceStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice statistics");
                return StatusCode(500, "An error occurred while retrieving invoice statistics");
            }
        }

        [HttpGet("unpaid")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetUnpaidInvoices()
        {
            try
            {
                var invoices = await _invoiceService.GetUnpaidInvoicesAsync();
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unpaid invoices");
                return StatusCode(500, "An error occurred while retrieving unpaid invoices");
            }
        }

        [HttpGet("overdue")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetOverdueInvoices()
        {
            try
            {
                var invoices = await _invoiceService.GetOverdueInvoicesAsync();
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overdue invoices");
                return StatusCode(500, "An error occurred while retrieving overdue invoices");
            }
        }
    }

    // Additional DTOs for actions that need specific data
    public class SendInvoiceDto
    {
        public string Email { get; set; }
    }
}

