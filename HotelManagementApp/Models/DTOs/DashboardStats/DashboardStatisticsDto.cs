namespace HotelManagementApp.Models.DTOs.DashboardStats
{
    using System;
    using System.Collections.Generic;
        public class DashboardStatisticsDto
        {
            // Current day statistics
            public int TodayNewReservations { get; set; }
            public int TodayArrivals { get; set; }
            public int TodayDepartures { get; set; }
            public decimal TodayRevenue { get; set; }

            // Occupancy statistics
            public int TotalRooms { get; set; }
            public int OccupiedRooms { get; set; }
            public int AvailableRooms { get; set; }
            public int MaintenanceRooms { get; set; }
            public double OccupancyRate { get; set; } // Percentage

            // Reservation statistics
            public int TotalActiveReservations { get; set; }
            public int PendingReservations { get; set; }
            public int CheckedInReservations { get; set; }

            // Task statistics
            public int PendingMaintenanceRequests { get; set; }
            public int UrgentMaintenanceRequests { get; set; }
            public int RoomsNeedingCleaning { get; set; }

            // Guest statistics
            public int CurrentGuests { get; set; }
            public int ExpectedArrivalsNext7Days { get; set; }

            // Financial statistics
            public decimal OutstandingPayments { get; set; }
            public int UnpaidInvoices { get; set; }

            // Customer satisfaction
            public double AverageRating { get; set; }
            public int UnresolvedFeedbacks { get; set; }

            // Forecast
            public Dictionary<DateTime, int> OccupancyForecast { get; set; } // Next 7 days
            public Dictionary<DateTime, decimal> RevenueForecast { get; set; } // Next 7 days
        }

        public class RevenueStatisticsDto
        {
            // Overall revenue stats
            public decimal TotalRevenue { get; set; }
            public decimal AverageRevenuePerRoom { get; set; }
            public decimal AverageRevenuePerBooking { get; set; }

            // Period comparisons
            public decimal CurrentMonthRevenue { get; set; }
            public decimal PreviousMonthRevenue { get; set; }
            public double MonthOverMonthGrowth { get; set; } // Percentage

            public decimal CurrentYearRevenue { get; set; }
            public decimal PreviousYearRevenue { get; set; }
            public double YearOverYearGrowth { get; set; } // Percentage

            // Revenue breakdown
            public Dictionary<string, decimal> RevenueByRoomType { get; set; }
            public Dictionary<string, decimal> RevenueByServiceType { get; set; }

            // Revenue by month for current year
            public Dictionary<int, decimal> MonthlyRevenue { get; set; } // Month number -> revenue

            // Average daily rate
            public decimal AverageDailyRate { get; set; }
            public Dictionary<string, decimal> AverageDailyRateByRoomType { get; set; }

            // RevPAR (Revenue Per Available Room)
            public decimal RevPAR { get; set; }

            // Payment method statistics
            public Dictionary<string, decimal> RevenueByPaymentMethod { get; set; }
            public Dictionary<string, int> CountByPaymentMethod { get; set; }
        }

        public class DailyRevenueDto
        {
            public DateTime Date { get; set; }
            public decimal TotalRevenue { get; set; }
            public int OccupiedRooms { get; set; }
            public int TotalRooms { get; set; }
            public double OccupancyRate { get; set; }
            public decimal RoomRevenue { get; set; }
            public decimal ServiceRevenue { get; set; }
            public decimal OtherRevenue { get; set; }
            public decimal AverageDailyRate { get; set; }
            public decimal RevPAR { get; set; } // Revenue Per Available Room
            public int NewReservations { get; set; }
            public int CheckIns { get; set; }
            public int CheckOuts { get; set; }
            public int CancelledReservations { get; set; }
        }

        public class OccupancyStatisticsDto
        {
            public double CurrentOccupancyRate { get; set; }
            public Dictionary<DateTime, double> DailyOccupancyRates { get; set; } // Date -> rate
            public Dictionary<int, double> OccupancyRateByRoomType { get; set; } // RoomTypeId -> rate
            public Dictionary<int, double> OccupancyRateByFloor { get; set; } // Floor -> rate
            public Dictionary<string, int> RoomStatusCounts { get; set; } // Status -> count
            public double AverageOccupancyRate { get; set; } // Over selected period
            public double PeakOccupancyRate { get; set; }
            public DateTime PeakOccupancyDate { get; set; }
            public double LowestOccupancyRate { get; set; }
            public DateTime LowestOccupancyDate { get; set; }
        }
    

}
