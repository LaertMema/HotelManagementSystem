using HotelManagementApp.Models;
using HotelManagementApp.Models.DTOs;
using HotelManagementApp.Models.DTOs.HotelManagement.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManagementApp.Services.FeedbackServiceSpace
{
    public class FeedbackService : IFeedbackService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FeedbackService> _logger;

        public FeedbackService(AppDbContext context, ILogger<FeedbackService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // CRUD operations
        public async Task<IEnumerable<FeedbackDto>> GetAllFeedbackAsync()
        {
            try
            {
                var feedbacks = await _context.Feedback
                    .Include(f => f.User)
                    .Include(f => f.Reservation)
                    .Include(f => f.ResolvedBy)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                return feedbacks.ToFeedbackDtos();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all feedback");
                throw;
            }
        }

        public async Task<FeedbackDto> GetFeedbackByIdAsync(int id)
        {
            try
            {
                var feedback = await _context.Feedback
                    .Include(f => f.User)
                    .Include(f => f.Reservation)
                    .Include(f => f.ResolvedBy)
                    .FirstOrDefaultAsync(f => f.Id == id);

                return feedback?.ToFeedbackDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback with ID {FeedbackId}", id);
                throw;
            }
        }

        public async Task<FeedbackDto> CreateFeedbackAsync(CreateFeedbackDto feedbackDto, int? userId)
        {
            try
            {
                // Validate reservation if provided
                if (feedbackDto.ReservationId.HasValue)
                {
                    var reservation = await _context.Reservations.FindAsync(feedbackDto.ReservationId.Value);
                    if (reservation == null)
                    {
                        throw new KeyNotFoundException($"Reservation with ID {feedbackDto.ReservationId.Value} not found");
                    }
                }

                // Validate user if provided
                if (userId.HasValue)
                {
                    var user = await _context.Users.FindAsync(userId.Value);
                    if (user == null)
                    {
                        throw new KeyNotFoundException($"User with ID {userId.Value} not found");
                    }
                }

                // Convert DTO to entity
                var feedback = feedbackDto.ToFeedbackModel(userId);

                await _context.Feedback.AddAsync(feedback);
                await _context.SaveChangesAsync();

                // Retrieve the saved feedback with includes
                var savedFeedback = await _context.Feedback
                    .Include(f => f.User)
                    .Include(f => f.Reservation)
                    .FirstOrDefaultAsync(f => f.Id == feedback.Id);

                return savedFeedback.ToFeedbackDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating feedback");
                throw;
            }
        }

        public async Task<FeedbackDto> UpdateFeedbackAsync(int id, UpdateFeedbackDto feedbackDto)
        {
            try
            {
                var existingFeedback = await _context.Feedback.FindAsync(id);
                if (existingFeedback == null)
                {
                    throw new KeyNotFoundException($"Feedback with ID {id} not found");
                }

                // Apply updates to the entity
                existingFeedback.ApplyUpdateFeedbackDto(feedbackDto);

                _context.Feedback.Update(existingFeedback);
                await _context.SaveChangesAsync();

                // Retrieve the updated feedback with includes
                var updatedFeedback = await _context.Feedback
                    .Include(f => f.User)
                    .Include(f => f.Reservation)
                    .Include(f => f.ResolvedBy)
                    .FirstOrDefaultAsync(f => f.Id == id);

                return updatedFeedback.ToFeedbackDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating feedback with ID {FeedbackId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteFeedbackAsync(int id)
        {
            try
            {
                var feedback = await _context.Feedback.FindAsync(id);
                if (feedback == null)
                {
                    return false;
                }

                _context.Feedback.Remove(feedback);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting feedback with ID {FeedbackId}", id);
                throw;
            }
        }

        // Specialized operations
        public async Task<IEnumerable<FeedbackDto>> GetFeedbackByUserAsync(int userId)
        {
            try
            {
                var feedbacks = await _context.Feedback
                    .Include(f => f.User)
                    .Include(f => f.Reservation)
                    .Include(f => f.ResolvedBy)
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                return feedbacks.ToFeedbackDtos();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback for user with ID {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<FeedbackDto>> GetFeedbackByReservationAsync(int reservationId)
        {
            try
            {
                var feedbacks = await _context.Feedback
                    .Include(f => f.User)
                    .Include(f => f.Reservation)
                    .Include(f => f.ResolvedBy)
                    .Where(f => f.ReservationId == reservationId)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                return feedbacks.ToFeedbackDtos();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback for reservation with ID {ReservationId}", reservationId);
                throw;
            }
        }

        public async Task<IEnumerable<FeedbackDto>> GetFeedbackByRatingAsync(int rating)
        {
            try
            {
                if (rating < 1 || rating > 5)
                {
                    throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5");
                }

                var feedbacks = await _context.Feedback
                    .Include(f => f.User)
                    .Include(f => f.Reservation)
                    .Include(f => f.ResolvedBy)
                    .Where(f => f.Rating == rating)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                return feedbacks.ToFeedbackDtos();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback with rating {Rating}", rating);
                throw;
            }
        }

        public async Task<IEnumerable<FeedbackDto>> GetFeedbackByCategoryAsync(string category)
        {
            try
            {
                var feedbacks = await _context.Feedback
                    .Include(f => f.User)
                    .Include(f => f.Reservation)
                    .Include(f => f.ResolvedBy)
                    .Where(f => f.Category == category)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                return feedbacks.ToFeedbackDtos();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback with category {Category}", category);
                throw;
            }
        }

        public async Task<IEnumerable<FeedbackDto>> GetFeedbackByResolutionStatusAsync(bool isResolved)
        {
            try
            {
                var feedbacks = await _context.Feedback
                    .Include(f => f.User)
                    .Include(f => f.Reservation)
                    .Include(f => f.ResolvedBy)
                    .Where(f => f.IsResolved == isResolved)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                return feedbacks.ToFeedbackDtos();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback with resolution status {IsResolved}", isResolved);
                throw;
            }
        }

        // Reservation verification for authorization checks
        public async Task<ReservationDto> GetReservationByIdAsync(int reservationId)
        {
            try
            {
                var reservation = await _context.Reservations
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == reservationId);

                return reservation?.ToReservationDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservation with ID {ReservationId}", reservationId);
                throw;
            }
        }

        // Business operations
        public async Task<FeedbackDto> ResolveFeedbackAsync(int id, string resolutionNotes, int resolvedById)
        {
            try
            {
                var feedback = await _context.Feedback.FindAsync(id);
                if (feedback == null)
                {
                    throw new KeyNotFoundException($"Feedback with ID {id} not found");
                }

                // Validate resolver
                var resolver = await _context.Users.FindAsync(resolvedById);
                if (resolver == null)
                {
                    throw new KeyNotFoundException($"User with ID {resolvedById} not found");
                }

                // Update resolution information
                feedback.IsResolved = true;
                feedback.ResolvedById = resolvedById;
                feedback.ResolvedAt = DateTime.UtcNow;
                feedback.ResolutionNotes = resolutionNotes;

                await _context.SaveChangesAsync();

                // Retrieve the resolved feedback with includes
                var resolvedFeedback = await _context.Feedback
                    .Include(f => f.User)
                    .Include(f => f.Reservation)
                    .Include(f => f.ResolvedBy)
                    .FirstOrDefaultAsync(f => f.Id == id);

                return resolvedFeedback.ToFeedbackDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving feedback with ID {FeedbackId}", id);
                throw;
            }
        }

        public async Task<FeedbackStatsByCategoryDto> GetFeedbackStatsByCategoryAsync()
        {
            try
            {
                var categoryCounts = await _context.Feedback
                    .Where(f => !string.IsNullOrEmpty(f.Category))
                    .GroupBy(f => f.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Category, x => x.Count);

                return categoryCounts.ToFeedbackStatsByCategoryDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback statistics by category");
                throw;
            }
        }

        public async Task<FeedbackStatsByRatingDto> GetFeedbackStatsByRatingAsync()
        {
            try
            {
                var ratingCounts = await _context.Feedback
                    .GroupBy(f => f.Rating)
                    .Select(g => new { Rating = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Rating, x => x.Count);

                // Ensure all ratings 1-5 are represented
                for (int i = 1; i <= 5; i++)
                {
                    if (!ratingCounts.ContainsKey(i))
                    {
                        ratingCounts.Add(i, 0);
                    }
                }

                // Calculate average rating
                var avgRating = await GetAverageRatingAsync();

                return ratingCounts.ToFeedbackStatsByRatingDto(avgRating);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback statistics by rating");
                throw;
            }
        }

        public async Task<double> GetAverageRatingAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                IQueryable<Feedback> query = _context.Feedback;

                // Apply date filters if provided
                if (startDate.HasValue)
                {
                    query = query.Where(f => f.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(f => f.CreatedAt <= endDate.Value);
                }

                // Calculate average rating
                var feedbackCount = await query.CountAsync();
                if (feedbackCount == 0)
                {
                    return 0;
                }

                var sumRatings = await query.SumAsync(f => f.Rating);
                return (double)sumRatings / feedbackCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average rating");
                throw;
            }
        }
    }
}

