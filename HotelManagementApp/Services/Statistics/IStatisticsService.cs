namespace HotelManagementApp.Services.Statistics
{
    using global::HotelManagementApp.Models.DTOs.DashboardStats;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

        public interface IStatisticsService
        {
            Task<DashboardStatisticsDto> GetDashboardStatisticsAsync();
            Task<RevenueStatisticsDto> GetRevenueStatisticsAsync(DateTime startDate, DateTime endDate);
            Task<IEnumerable<DailyRevenueDto>> GetDailyRevenueAsync(DateTime startDate, DateTime endDate);
        }
    

}
