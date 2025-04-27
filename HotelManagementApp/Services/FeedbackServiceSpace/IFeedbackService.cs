using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelManagementApp.Models;
using HotelManagementApp.Models.DTOs;
using HotelManagementApp.Models.DTOs.HotelManagement.Models.DTOs;

namespace HotelManagementApp.Services.FeedbackServiceSpace
{
    public interface IFeedbackService
    {
        // CRUD operations
        Task<IEnumerable<FeedbackDto>> GetAllFeedbackAsync();
        Task<FeedbackDto> GetFeedbackByIdAsync(int id);
        Task<FeedbackDto> CreateFeedbackAsync(CreateFeedbackDto feedbackDto, int? userId);
        Task<FeedbackDto> UpdateFeedbackAsync(int id, UpdateFeedbackDto feedbackDto);
        Task<bool> DeleteFeedbackAsync(int id);

        // Specialized operations
        Task<IEnumerable<FeedbackDto>> GetFeedbackByUserAsync(int userId);
        Task<IEnumerable<FeedbackDto>> GetFeedbackByReservationAsync(int reservationId);
        Task<IEnumerable<FeedbackDto>> GetFeedbackByRatingAsync(int rating);
        Task<IEnumerable<FeedbackDto>> GetFeedbackByCategoryAsync(string category);
        Task<IEnumerable<FeedbackDto>> GetFeedbackByResolutionStatusAsync(bool isResolved);

        // Reservation verification for authorization checks
        Task<ReservationDto> GetReservationByIdAsync(int reservationId);

        // Business operations
        Task<FeedbackDto> ResolveFeedbackAsync(int id, string resolutionNotes, int resolvedById);
        Task<FeedbackStatsByCategoryDto> GetFeedbackStatsByCategoryAsync();
        Task<FeedbackStatsByRatingDto> GetFeedbackStatsByRatingAsync();
        Task<double> GetAverageRatingAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}

