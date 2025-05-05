using HotelManagementApp.Models.DTOs.Service;
using HotelManagementApp.Services.ServiceService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServiceController : ControllerBase
    {
        private readonly IServiceService _serviceService;
        private readonly ILogger<ServiceController> _logger;

        public ServiceController(IServiceService serviceService, ILogger<ServiceController> logger)
        {
            _serviceService = serviceService ?? throw new ArgumentNullException(nameof(serviceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ServiceDto>>> GetAllServices()
        {
            try
            {
                var services = await _serviceService.GetAllServicesAsync();
                return Ok(services);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all services");
                return StatusCode(500, "An error occurred while retrieving services");
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ServiceDto>> GetServiceById(int id)
        {
            try
            {
                var service = await _serviceService.GetServiceByIdAsync(id);
                if (service == null)
                {
                    return NotFound($"Service with ID {id} not found");
                }
                return Ok(service);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service with ID {ServiceId}", id);
                return StatusCode(500, "An error occurred while retrieving the service");
            }
        }

        [HttpGet("type/{serviceType}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ServiceDto>>> GetServicesByType(string serviceType)
        {
            try
            {
                var services = await _serviceService.GetServicesByTypeAsync(serviceType);
                return Ok(services);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving services with type {ServiceType}", serviceType);
                return StatusCode(500, "An error occurred while retrieving services by type");
            }
        }

        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ServiceDto>>> GetActiveServices()
        {
            try
            {
                var services = await _serviceService.GetActiveServicesAsync();
                return Ok(services);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active services");
                return StatusCode(500, "An error occurred while retrieving active services");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<ServiceDto>> CreateService(CreateServiceDto createServiceDto)
        {
            try
            {
                var service = await _serviceService.CreateServiceAsync(createServiceDto);
                return CreatedAtAction(nameof(GetServiceById), new { id = service.Id }, service);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service");
                return StatusCode(500, "An error occurred while creating the service");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<ServiceDto>> UpdateService(int id, UpdateServiceDto updateServiceDto)
        {
            try
            {
                var service = await _serviceService.UpdateServiceAsync(id, updateServiceDto);
                return Ok(service);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Service not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service with ID {ServiceId}", id);
                return StatusCode(500, "An error occurred while updating the service");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteService(int id)
        {
            try
            {
                var result = await _serviceService.DeleteServiceAsync(id);
                if (!result)
                {
                    return NotFound($"Service with ID {id} not found");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service with ID {ServiceId}", id);
                return StatusCode(500, "An error occurred while deleting the service");
            }
        }

        [HttpGet("stats/bytype")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<Dictionary<string, int>>> GetServiceStatsByType()
        {
            try
            {
                var stats = await _serviceService.GetServiceStatsByTypeAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service statistics by type");
                return StatusCode(500, "An error occurred while retrieving service statistics");
            }
        }
    }
}

