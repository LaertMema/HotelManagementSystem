namespace HotelManagementApp.Services.Statistics
{
    using global::HotelManagementApp.Models.DTOs.DashboardStats;
    using global::HotelManagementApp.Models.Enums;
    using global::HotelManagementApp.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

        public class StatisticsService : IStatisticsService
        {
            private readonly AppDbContext _context;
            private readonly ILogger<StatisticsService> _logger;

            public StatisticsService(AppDbContext context, ILogger<StatisticsService> logger)
            {
                _context = context ?? throw new ArgumentNullException(nameof(context));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync()
            {
                try
                {
                    var today = DateTime.Today;
                    var endOfToday = today.AddDays(1).AddTicks(-1);
                    var nextWeek = today.AddDays(7);

                    // Room statistics
                    var totalRooms = await _context.Rooms.CountAsync();
                    var occupiedRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Occupied);
                    var maintenanceRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Maintenance);
                    var availableRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Available);

                    // Reservation statistics
                    var todayNewReservations = await _context.Reservations
                        .CountAsync(r => r.ReservationDate.Date == today);

                    var todayArrivals = await _context.Reservations
                        .CountAsync(r => r.CheckInDate.Date == today && r.Status == ReservationStatus.Reserved);

                    var todayDepartures = await _context.Reservations
                        .CountAsync(r => r.CheckOutDate.Date == today && r.Status == ReservationStatus.CheckedIn);

                    var activeReservations = await _context.Reservations
                        .CountAsync(r => r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.CheckedIn);

                    var pendingReservations = await _context.Reservations
                        .CountAsync(r => r.Status == ReservationStatus.Reserved);

                    var checkedInReservations = await _context.Reservations
                        .CountAsync(r => r.Status == ReservationStatus.CheckedIn);

                    var currentGuests = await _context.Reservations
                        .Where(r => r.Status == ReservationStatus.CheckedIn)
                        .SumAsync(r => r.NumberOfGuests);

                    var expectedArrivals = await _context.Reservations
                        .CountAsync(r => r.CheckInDate > today && r.CheckInDate <= nextWeek && r.Status == ReservationStatus.Reserved);

                    // Maintenance and cleaning statistics
                    var pendingMaintenance = await _context.MaintenanceRequests
                        .CountAsync(m => m.Status == MaintenanceRequestStatus.Reported || m.Status == MaintenanceRequestStatus.InProgress);

                    var urgentMaintenance = await _context.MaintenanceRequests
                        .CountAsync(m => (m.Status == MaintenanceRequestStatus.Reported || m.Status == MaintenanceRequestStatus.InProgress)
                                        && m.Priority == MaintenanceRequestPriority.High);

                    var roomsNeedingCleaning = await _context.Rooms
                        .CountAsync(r => r.NeedsCleaning);

                    // Financial statistics
                    var todayRevenue = await _context.Payments
                        .Where(p => p.PaymentDate >= today && p.PaymentDate <= endOfToday && !p.IsRefunded)
                        .SumAsync(p => p.AmountPaid);

                    var outstandingPayments = await _context.Invoices
                        .Where(i => i.Status != PaymentStatus.Paid && i.Status != PaymentStatus.Cancelled)
                        .SumAsync(i => i.Total - i.Payments.Where(p => !p.IsRefunded).Sum(p => p.AmountPaid));

                    var unpaidInvoices = await _context.Invoices
                        .CountAsync(i => i.Status != PaymentStatus.Paid && i.Status != PaymentStatus.Cancelled);

                    // Customer satisfaction
                    var averageRating = await _context.Feedback
                        .Where(f => f.CreatedAt >= today.AddDays(-30))
                        .AverageAsync(f => (double?)f.Rating) ?? 0;

                    var unresolvedFeedbacks = await _context.Feedback
                        .CountAsync(f => !f.IsResolved);

                    // Calculate occupancy rate
                    double occupancyRate = totalRooms > 0 ?
                        (double)(occupiedRooms + availableRooms - availableRooms) / (totalRooms - maintenanceRooms) * 100 : 0;

                    // Get 7-day occupancy forecast
                    var occupancyForecast = await GetOccupancyForecastAsync(today, nextWeek);

                    // Get 7-day revenue forecast
                    var revenueForecast = await GetRevenueForecastAsync(today, nextWeek);

                    return new DashboardStatisticsDto
                    {
                        // Room statistics
                        TotalRooms = totalRooms,
                        OccupiedRooms = occupiedRooms,
                        AvailableRooms = availableRooms,
                        MaintenanceRooms = maintenanceRooms,
                        OccupancyRate = Math.Round(occupancyRate, 2),

                        // Reservation statistics
                        TodayNewReservations = todayNewReservations,
                        TodayArrivals = todayArrivals,
                        TodayDepartures = todayDepartures,
                        TotalActiveReservations = activeReservations,
                        PendingReservations = pendingReservations,
                        CheckedInReservations = checkedInReservations,
                        CurrentGuests = currentGuests,
                        ExpectedArrivalsNext7Days = expectedArrivals,

                        // Maintenance and cleaning
                        PendingMaintenanceRequests = pendingMaintenance,
                        UrgentMaintenanceRequests = urgentMaintenance,
                        RoomsNeedingCleaning = roomsNeedingCleaning,

                        // Financial
                        TodayRevenue = todayRevenue,
                        OutstandingPayments = outstandingPayments,
                        UnpaidInvoices = unpaidInvoices,

                        // Customer satisfaction
                        AverageRating = Math.Round(averageRating, 1),
                        UnresolvedFeedbacks = unresolvedFeedbacks,

                        // Forecasts
                        OccupancyForecast = occupancyForecast,
                        RevenueForecast = revenueForecast
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving dashboard statistics");
                    throw;
                }
            }

            public async Task<RevenueStatisticsDto> GetRevenueStatisticsAsync(DateTime startDate, DateTime endDate)
            {
                try
                {
                    // Calculate time periods
                    var today = DateTime.Today;
                    var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                    var firstDayOfLastMonth = firstDayOfMonth.AddMonths(-1);
                    var lastDayOfLastMonth = firstDayOfMonth.AddDays(-1);
                    var firstDayOfYear = new DateTime(today.Year, 1, 1);
                    var firstDayOfLastYear = new DateTime(today.Year - 1, 1, 1);
                    var lastDayOfLastYear = new DateTime(today.Year, 1, 1).AddDays(-1);

                    // Get all payments in the specified period
                    var periodPayments = await _context.Payments
                        .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && !p.IsRefunded)
                        .ToListAsync();

                    // Total revenue for the specified period
                    var totalRevenue = periodPayments.Sum(p => p.AmountPaid);

                    // Get reservation count in the period
                    var reservationsInPeriod = await _context.Reservations
                        .CountAsync(r => r.CheckOutDate >= startDate && r.CheckInDate <= endDate &&
                                        r.Status != ReservationStatus.Cancelled);

                    // Get room statistics
                    var totalRooms = await _context.Rooms.CountAsync();
                    var roomDays = totalRooms * (endDate - startDate).Days;

                    // Calculate average revenue metrics
                    decimal averageRevenuePerRoom = roomDays > 0 ? totalRevenue / roomDays : 0;
                    decimal averageRevenuePerBooking = reservationsInPeriod > 0 ? totalRevenue / reservationsInPeriod : 0;

                    // Get revenue for current month
                    var currentMonthPayments = await _context.Payments
                        .Where(p => p.PaymentDate >= firstDayOfMonth && p.PaymentDate <= today && !p.IsRefunded)
                        .SumAsync(p => p.AmountPaid);

                    // Get revenue for previous month
                    var previousMonthPayments = await _context.Payments
                        .Where(p => p.PaymentDate >= firstDayOfLastMonth && p.PaymentDate <= lastDayOfLastMonth && !p.IsRefunded)
                        .SumAsync(p => p.AmountPaid);

                    // Get revenue for current year
                    var currentYearPayments = await _context.Payments
                        .Where(p => p.PaymentDate >= firstDayOfYear && p.PaymentDate <= today && !p.IsRefunded)
                        .SumAsync(p => p.AmountPaid);

                    // Get revenue for previous year
                    var previousYearPayments = await _context.Payments
                        .Where(p => p.PaymentDate >= firstDayOfLastYear && p.PaymentDate <= lastDayOfLastYear && !p.IsRefunded)
                        .SumAsync(p => p.AmountPaid);

                    // Calculate growth percentages
                    double monthOverMonthGrowth = previousMonthPayments > 0 ?
                        (double)((currentMonthPayments - previousMonthPayments) / previousMonthPayments) * 100 : 0;

                    double yearOverYearGrowth = previousYearPayments > 0 ?
                        (double)((currentYearPayments - previousYearPayments) / previousYearPayments) * 100 : 0;

                    // Revenue by room type
                    var revenueByRoomType = await _context.Invoices
                        .Where(i => i.Reservation.CheckOutDate >= startDate &&
                                   i.Reservation.CheckInDate <= endDate)
                        .Include(i => i.Reservation)
                        .ThenInclude(r => r.RoomType)
                        .GroupBy(i => i.Reservation.RoomType.Name)
                        .Select(g => new
                        {
                            RoomType = g.Key,
                            Revenue = g.Sum(i => i.Total)
                        })
                        .ToDictionaryAsync(x => x.RoomType, x => x.Revenue);

                    // Revenue by service type
                    var revenueByServiceType = await _context.ServiceOrders
                        .Where(so => so.OrderDateTime >= startDate &&
                                    so.OrderDateTime <= endDate &&
                                    so.Status == ServiceOrderStatus.Completed)
                        .Include(so => so.Service)
                        .GroupBy(so => so.Service.ServiceName)
                        .Select(g => new
                        {
                            ServiceType = g.Key,
                            Revenue = g.Sum(so => so.TotalPrice)
                        })
                        .ToDictionaryAsync(x => x.ServiceType, x => x.Revenue);

                    // Revenue by month for current year
                    var monthlyRevenue = await _context.Payments
                        .Where(p => p.PaymentDate.Year == today.Year && !p.IsRefunded)
                        .GroupBy(p => p.PaymentDate.Month)
                        .Select(g => new
                        {
                            Month = g.Key,
                            Revenue = g.Sum(p => p.AmountPaid)
                        })
                        .ToDictionaryAsync(x => x.Month, x => x.Revenue);

                    // Revenue by payment method
                    var revenueByPaymentMethod = await _context.Payments
                        .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && !p.IsRefunded)
                        .GroupBy(p => p.Method)
                        .Select(g => new
                        {
                            PaymentMethod = g.Key.ToString(),
                            Revenue = g.Sum(p => p.AmountPaid)
                        })
                        .ToDictionaryAsync(x => x.PaymentMethod, x => x.Revenue);

                    // Count by payment method
                    var countByPaymentMethod = await _context.Payments
                        .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && !p.IsRefunded)
                        .GroupBy(p => p.Method)
                        .Select(g => new
                        {
                            PaymentMethod = g.Key.ToString(),
                            Count = g.Count()
                        })
                        .ToDictionaryAsync(x => x.PaymentMethod, x => x.Count);

                    // Calculate average daily rate
                    var reservationsCheckingOutInPeriod = await _context.Reservations
                        .Where(r => r.CheckOutDate >= startDate && r.CheckOutDate <= endDate &&
                                   r.Status == ReservationStatus.CheckedOut)
                        .ToListAsync();

                    var totalNights = reservationsCheckingOutInPeriod.Sum(r =>
                        (r.CheckOutDate - r.CheckInDate).Days);

                    var totalRoomRevenue = reservationsCheckingOutInPeriod.Sum(r => r.TotalPrice);

                    decimal averageDailyRate = totalNights > 0 ? totalRoomRevenue / totalNights : 0;

                    // Average daily rate by room type
                    var averageDailyRateByRoomType = await _context.Reservations
                        .Where(r => r.CheckOutDate >= startDate && r.CheckOutDate <= endDate &&
                                   r.Status == ReservationStatus.CheckedOut)
                        .Include(r => r.RoomType)
                        .GroupBy(r => r.RoomType.Name)
                        .Select(g => new
                        {
                            RoomType = g.Key,
                            Nights = g.Sum(r => (r.CheckOutDate - r.CheckInDate).Days),
                            Revenue = g.Sum(r => r.TotalPrice)
                        })
                        .ToDictionaryAsync(
                            x => x.RoomType,
                            x => x.Nights > 0 ? x.Revenue / x.Nights : 0
                        );

                    // Calculate RevPAR (Revenue Per Available Room)
                    var totalRoomDays = totalRooms * (endDate - startDate).Days;
                    decimal revPAR = totalRoomDays > 0 ? totalRevenue / totalRoomDays : 0;

                    return new RevenueStatisticsDto
                    {
                        // Overall revenue stats
                        TotalRevenue = totalRevenue,
                        AverageRevenuePerRoom = averageRevenuePerRoom,
                        AverageRevenuePerBooking = averageRevenuePerBooking,

                        // Period comparisons
                        CurrentMonthRevenue = currentMonthPayments,
                        PreviousMonthRevenue = previousMonthPayments,
                        MonthOverMonthGrowth = Math.Round(monthOverMonthGrowth, 2),

                        CurrentYearRevenue = currentYearPayments,
                        PreviousYearRevenue = previousYearPayments,
                        YearOverYearGrowth = Math.Round(yearOverYearGrowth, 2),

                        // Revenue breakdown
                        RevenueByRoomType = revenueByRoomType,
                        RevenueByServiceType = revenueByServiceType,

                        // Monthly revenue
                        MonthlyRevenue = monthlyRevenue,

                        // Average daily rate
                        AverageDailyRate = averageDailyRate,
                        AverageDailyRateByRoomType = averageDailyRateByRoomType,

                        // RevPAR
                        RevPAR = revPAR,

                        // Payment method statistics
                        RevenueByPaymentMethod = revenueByPaymentMethod,
                        CountByPaymentMethod = countByPaymentMethod
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving revenue statistics between {StartDate} and {EndDate}", startDate, endDate);
                    throw;
                }
            }

            public async Task<IEnumerable<DailyRevenueDto>> GetDailyRevenueAsync(DateTime startDate, DateTime endDate)
            {
                try
                {
                    var dailyRevenue = new List<DailyRevenueDto>();
                    var totalRooms = await _context.Rooms.CountAsync();

                    // Process each day in the range
                    for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                    {
                        var nextDay = date.AddDays(1);

                        // Total revenue from payments made on this day
                        var totalPayments = await _context.Payments
                            .Where(p => p.PaymentDate >= date && p.PaymentDate < nextDay && !p.IsRefunded)
                            .SumAsync(p => p.AmountPaid);

                        // Room revenue (from accommodations)
                        var roomRevenue = await _context.ServiceOrders
                            .Where(so => so.OrderDateTime >= date && so.OrderDateTime < nextDay &&
                                        so.Status == ServiceOrderStatus.Completed &&
                                        so.Service.ServiceName == "Accommodation")
                            .SumAsync(so => so.TotalPrice);

                        // Service revenue (all non-accommodation services)
                        var serviceRevenue = await _context.ServiceOrders
                            .Where(so => so.OrderDateTime >= date && so.OrderDateTime < nextDay &&
                                        so.Status == ServiceOrderStatus.Completed &&
                                        so.Service.ServiceName != "Accommodation")
                            .SumAsync(so => so.TotalPrice);

                        // Other revenue (calculated as total - room - service)
                        var otherRevenue = totalPayments - roomRevenue - serviceRevenue;

                        // Occupancy information
                        var occupiedRooms = await _context.Reservations
                            .CountAsync(r => r.CheckInDate <= date && r.CheckOutDate > date &&
                                            (r.Status == ReservationStatus.CheckedIn || r.Status == ReservationStatus.CheckedOut));

                        var occupancyRate = totalRooms > 0 ?
                            (double)occupiedRooms / totalRooms * 100 : 0;

                        // Reservation activity
                        var newReservations = await _context.Reservations
                            .CountAsync(r => r.ReservationDate.Date == date);

                        var checkIns = await _context.Reservations
                            .CountAsync(r => r.CheckedInTime.HasValue && r.CheckedInTime.Value.Date == date);

                        var checkOuts = await _context.Reservations
                            .CountAsync(r => r.CheckedOutTime.HasValue && r.CheckedOutTime.Value.Date == date);

                        var cancellations = await _context.Reservations
                            .CountAsync(r => r.CancelledAt.HasValue && r.CancelledAt.Value.Date == date);

                        // Calculate ADR (Average Daily Rate)
                        var roomNightsSold = await _context.Reservations
                            .Where(r => r.CheckInDate <= date && r.CheckOutDate > date &&
                                      (r.Status == ReservationStatus.CheckedIn || r.Status == ReservationStatus.CheckedOut))
                            .CountAsync();

                        var averageDailyRate = roomNightsSold > 0 ?
                            roomRevenue / roomNightsSold : 0;

                        // Calculate RevPAR (Revenue Per Available Room)
                        var revPAR = totalRooms > 0 ?
                            roomRevenue / totalRooms : 0;

                        dailyRevenue.Add(new DailyRevenueDto
                        {
                            Date = date,
                            TotalRevenue = totalPayments,
                            OccupiedRooms = occupiedRooms,
                            TotalRooms = totalRooms,
                            OccupancyRate = Math.Round(occupancyRate, 2),
                            RoomRevenue = roomRevenue,
                            ServiceRevenue = serviceRevenue,
                            OtherRevenue = otherRevenue,
                            AverageDailyRate = averageDailyRate,
                            RevPAR = revPAR,
                            NewReservations = newReservations,
                            CheckIns = checkIns,
                            CheckOuts = checkOuts,
                            CancelledReservations = cancellations
                        });
                    }

                    return dailyRevenue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving daily revenue between {StartDate} and {EndDate}", startDate, endDate);
                    throw;
                }
            }

            #region Helper Methods

            private async Task<Dictionary<DateTime, int>> GetOccupancyForecastAsync(DateTime startDate, DateTime endDate)
            {
                var forecast = new Dictionary<DateTime, int>();

                // Initialize dictionary with all dates in range
                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    forecast[date] = 0;
                }

                // Get all reservations that overlap with the date range
                var reservations = await _context.Reservations
                    .Where(r =>
                        r.Status != ReservationStatus.Cancelled &&
                        ((r.CheckInDate >= startDate && r.CheckInDate <= endDate) ||
                         (r.CheckOutDate >= startDate && r.CheckOutDate <= endDate) ||
                         (r.CheckInDate <= startDate && r.CheckOutDate >= endDate)))
                    .ToListAsync();

                // Count occupancy for each day
                foreach (var reservation in reservations)
                {
                    var reservationStart = reservation.CheckInDate > startDate ? reservation.CheckInDate : startDate;
                    var reservationEnd = reservation.CheckOutDate < endDate ? reservation.CheckOutDate : endDate;

                    for (var date = reservationStart.Date; date < reservationEnd.Date; date = date.AddDays(1))
                    {
                        if (forecast.ContainsKey(date))
                        {
                            forecast[date]++;
                        }
                    }
                }

                return forecast;
            }

            private async Task<Dictionary<DateTime, decimal>> GetRevenueForecastAsync(DateTime startDate, DateTime endDate)
            {
                var forecast = new Dictionary<DateTime, decimal>();

                // Initialize dictionary with all dates in range
                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    forecast[date] = 0;
                }

                // Get confirmed reservations with their daily rates
                var reservations = await _context.Reservations
                    .Where(r =>
                        (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.CheckedIn) &&
                        ((r.CheckInDate >= startDate && r.CheckInDate <= endDate) ||
                         (r.CheckOutDate >= startDate && r.CheckOutDate <= endDate) ||
                         (r.CheckInDate <= startDate && r.CheckOutDate >= endDate)))
                    .Include(r => r.Room)
                    .ToListAsync();

                // Calculate expected revenue for each day
                foreach (var reservation in reservations)
                {
                    var reservationStart = reservation.CheckInDate > startDate ? reservation.CheckInDate : startDate;
                    var reservationEnd = reservation.CheckOutDate < endDate ? reservation.CheckOutDate : endDate;
                    var numberOfNights = (reservation.CheckOutDate - reservation.CheckInDate).Days;
                    var dailyRate = numberOfNights > 0 ? reservation.TotalPrice / numberOfNights : 0;

                    for (var date = reservationStart.Date; date < reservationEnd.Date; date = date.AddDays(1))
                    {
                        if (forecast.ContainsKey(date))
                        {
                            forecast[date] += dailyRate;
                        }
                    }
                }

                return forecast;
            }

            #endregion
        }
    

}
