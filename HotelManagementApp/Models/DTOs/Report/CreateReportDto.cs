namespace HotelManagementApp.Models.DTOs.Report
{
    
        using System;
        using System.ComponentModel.DataAnnotations;
        using System.Collections.Generic;

        public class CreateReportDto
        {
            [Required]
            [StringLength(100, ErrorMessage = "Report name must be at most 100 characters")]
            public string ReportName { get; set; }

            [Required]
            [StringLength(50, ErrorMessage = "Report type must be at most 50 characters")]
            public string ReportType { get; set; }  // Financial, Occupancy, Staff, etc.

            [Required]
            public DateTime StartDate { get; set; }

            [Required]
            public DateTime EndDate { get; set; }

            [StringLength(1000)]
            public string Parameters { get; set; } // JSON string with report parameters

            [StringLength(500)]
            public string Description { get; set; }
        }

        public class ReportDto
        {
            public int ReportId { get; set; }
            public string ReportName { get; set; }
            public string ReportType { get; set; }
            public DateTime CreationDate { get; set; }
            public int? CreatedById { get; set; }
            public string CreatedByName { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Parameters { get; set; }
            public string Description { get; set; }
            public string Status { get; set; }
            public string ReportUrl { get; set; }
            public string FileFormat { get; set; }
            public Dictionary<string, object> Data { get; set; }
        }

        public class ReportSummaryDto
        {
            public int ReportId { get; set; }
            public string ReportName { get; set; }
            public string ReportType { get; set; }
            public DateTime CreationDate { get; set; }
            public string CreatedByName { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Status { get; set; }
        }

        public class ExportReportDto
        {
            [Required]
            public int ReportId { get; set; }

            [Required]
            [StringLength(10)]
            public string Format { get; set; } = "PDF"; // PDF, CSV, Excel, etc.

            public bool IncludeCharts { get; set; } = true;
        }
    

}
