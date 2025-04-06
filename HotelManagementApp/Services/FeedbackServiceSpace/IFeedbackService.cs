
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelManagementApp.Models;
namespace HotelManagementApp.Services.FeedbackServiceSpace
{
    
        public interface IFeedbackService
        {
            // CRUD operations
            Task<IEnumerable<Feedback>> GetAllFeedbackAsync();
            Task<Feedback> GetFeedbackByIdAsync(int id);
            Task<Feedback> CreateFeedbackAsync(Feedback feedback);
            Task<Feedback> UpdateFeedbackAsync(Feedback feedback);
            Task<bool> DeleteFeedbackAsync(int id);

            // Specialized operations
            Task<IEnumerable<Feedback>> GetFeedbackByUserAsync(int userId);
            Task<IEnumerable<Feedback>> GetFeedbackByReservationAsync(int reservationId);
            Task<IEnumerable<Feedback>> GetFeedbackByRatingAsync(int rating);
            Task<IEnumerable<Feedback>> GetFeedbackByCategoryAsync(string category);
            Task<IEnumerable<Feedback>> GetUnresolvedFeedbackAsync();

            // Business operations
            Task<Feedback> ResolveFeedbackAsync(int id, string resolutionNotes, int resolvedById);
            Task<Dictionary<string, int>> GetFeedbackStatsByCategoryAsync();
            Task<Dictionary<int, int>> GetFeedbackStatsByRatingAsync();
            Task<double> GetAverageRatingAsync(DateTime? startDate = null, DateTime? endDate = null);
        }
    
}
