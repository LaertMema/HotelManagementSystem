using HotelManagementApp.Models.DTOs.DashboardStats;
using HotelManagementApp.Services.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagementApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Manager")]
    public class DashboardController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IStatisticsService statisticsService, ILogger<DashboardController> logger)
        {
            _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("manager")]
        public async Task<ActionResult<DashboardStatisticsDto>> GetManagerDashboardStats()
        {
            try
            {
                var stats = await _statisticsService.GetDashboardStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving manager dashboard statistics");
                return StatusCode(500, "An error occurred while retrieving dashboard statistics");
            }
        }

        [HttpGet("revenue")]
        public async Task<ActionResult<RevenueStatisticsDto>> GetRevenueDashboardStats(
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                {
                    return BadRequest("Start date must be before end date");
                }

                var stats = await _statisticsService.GetRevenueStatisticsAsync(startDate, endDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving revenue statistics");
                return StatusCode(500, "An error occurred while retrieving revenue statistics");
            }
        }

        [HttpGet("daily-revenue")]
        public async Task<ActionResult<IEnumerable<DailyRevenueDto>>> GetDailyRevenue(
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                {
                    return BadRequest("Start date must be before end date");
                }

                var stats = await _statisticsService.GetDailyRevenueAsync(startDate, endDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving daily revenue statistics");
                return StatusCode(500, "An error occurred while retrieving daily revenue statistics");
            }
        }

        // Receptionist dashboard - simplified stats focused on daily operations
        [HttpGet("receptionist")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<ActionResult<object>> GetReceptionistDashboardStats()
        {
            try
            {
                var stats = await _statisticsService.GetDashboardStatisticsAsync();

                // Return only relevant data for receptionists
                var receptionistStats = new
                {
                    TodayArrivals = stats.TodayArrivals,
                    TodayDepartures = stats.TodayDepartures,
                    TodayNewReservations = stats.TodayNewReservations,
                    CurrentGuests = stats.CurrentGuests,
                    OccupiedRooms = stats.OccupiedRooms,
                    AvailableRooms = stats.AvailableRooms,
                    ExpectedArrivalsNext7Days = stats.ExpectedArrivalsNext7Days,
                    UnpaidInvoices = stats.UnpaidInvoices,
                    OccupancyRate = stats.OccupancyRate
                };

                return Ok(receptionistStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving receptionist dashboard statistics");
                return StatusCode(500, "An error occurred while retrieving dashboard statistics");
            }
        }

        // Housekeeper dashboard - focused on cleaning tasks
        [HttpGet("housekeeper")]
        [Authorize(Roles = "Admin,Manager,Housekeeper")]
        public async Task<ActionResult<object>> GetHousekeeperDashboardStats()
        {
            try
            {
                var stats = await _statisticsService.GetDashboardStatisticsAsync();

                // Return only relevant data for housekeepers
                var housekeeperStats = new
                {
                    RoomsNeedingCleaning = stats.RoomsNeedingCleaning,
                    TodayDepartures = stats.TodayDepartures,
                    OccupiedRooms = stats.OccupiedRooms,
                    MaintenanceRooms = stats.MaintenanceRooms
                };

                return Ok(housekeeperStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving housekeeper dashboard statistics");
                return StatusCode(500, "An error occurred while retrieving dashboard statistics");
            }
        }
    }
}

