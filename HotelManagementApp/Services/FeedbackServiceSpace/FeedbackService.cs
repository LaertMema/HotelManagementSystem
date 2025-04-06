using HotelManagementApp.Models;
using Microsoft.EntityFrameworkCore;


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
        public async Task<IEnumerable<Feedback>> GetAllFeedbackAsync()
        {
            try
            {
                return await _context.Feedback
                    .Include(f => f.User)
                    .Include(f => f.Reservation)
                    .Include(f => f.ResolvedBy)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all feedback");
                throw;
            }
        }

        public async Task<Feedback> GetFeedbackByIdAsync(int id)
        {
            try
            {
                return await _context.Feedback
                    .Include(f => f.User)
                    .Include(f => f.Reservation)
                    .Include(f => f.ResolvedBy)
                    .FirstOrDefaultAsync(f => f.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback with ID {FeedbackId}", id);
                throw;
            }
        }

        public async Task<Feedback> CreateFeedbackAsync(Feedback feedback)
        {
            try
            {
                // Set creation date if not provided
                if (feedback.CreatedAt == default)
                {
                    feedback.CreatedAt = DateTime.UtcNow;
                }

                // Validate user if provided
                if (feedback.UserId.HasValue)
                {
                    var user = await _context.Users.FindAsync(feedback.UserId.Value);
                    if (user == null)
                    {
                        throw new KeyNotFoundException($"User with ID {feedback.UserId.Value} not found");
                    }
                }

                // Validate reservation if provided
                if (feedback.ReservationId.HasValue)
                {
                    var reservation = await _context.Reservations.FindAsync(feedback.ReservationId.Value);
                    if (reservation == null)
                    {
                        throw new KeyNotFoundException($"Reservation with ID {feedback.ReservationId.Value} not found");
                    }
                }

                // Initialize IsResolved to false for new feedback
                feedback.IsResolved = false;
                feedback.ResolvedById = null;
                feedback.ResolvedAt = null;
                feedback.ResolutionNotes = null;

                await _context.Feedback.AddAsync(feedback);
                await _context.SaveChangesAsync();

                return feedback;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating feedback");
                throw;
            }
        }

        public async Task<Feedback> UpdateFeedbackAsync(Feedback feedback)
        {
            try
            {
                var existingFeedback = await _context.Feedback.FindAsync(feedback.Id);
                if (existingFeedback == null)
                {
                    throw new KeyNotFoundException($"Feedback with ID {feedback.Id} not found");
                }

                // Preserve original creation date and resolved status information
                feedback.CreatedAt = existingFeedback.CreatedAt;

                // Only allow resolution status to be changed through the ResolveFeedback method
                if (!existingFeedback.IsResolved)
                {
                    feedback.IsResolved = false;
                    feedback.ResolvedById = null;
                    feedback.ResolvedAt = null;
                    feedback.ResolutionNotes = null;
                }
                else
                {
                    feedback.IsResolved = existingFeedback.IsResolved;
                    feedback.ResolvedById = existingFeedback.ResolvedById;
                    feedback.ResolvedAt = existingFeedback.ResolvedAt;
                    feedback.ResolutionNotes = existingFeedback.ResolutionNotes;
                }

                _context.Entry(existingFeedback).CurrentValues.SetValues(feedback);
                await _context.SaveChangesAsync();

                return await GetFeedbackByIdAsync(feedback.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating feedback with ID {FeedbackId}", feedback.Id);
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
        public async Task<IEnumerable<Feedback>> GetUnresolvedFeedbackAsync()
        {
            try
            {
                return await _context.Feedback
                    .Include(f => f.User)
                    .Include(f => f.Reservation)
                    .Where(f => !f.IsResolved)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unresolved feedback");
                throw;
            }
        }

        public async Task<IEnumerable<Feedback>> GetFeedbackByRatingAsync(int rating)
        {
            try
            {
                if (rating < 1 || rating > 5)
                {
                    throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5");
                }

                return await _context.Feedback
                    .Include(f => f.User)
                    .Include(f => f.Reservation)
                    .Include(f => f.ResolvedBy)
                    .Where(f => f.Rating == rating)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback with rating {Rating}", rating);
                throw;
            }
        }

        public async Task<IEnumerable<Feedback>> GetFeedbackByCategoryAsync(string category)
        {
            try
            {
                return await _context.Feedback
                    .Include(f => f.User)
                    .Include(f => f.Reservation)
                    .Include(f => f.ResolvedBy)
                    .Where(f => f.Category == category)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback with category {Category}", category);
                throw;
            }
        }

        public async Task<IEnumerable<Feedback>> GetFeedbackByDateRangeAsync(DateTime start, DateTime end)
        {
            return await _context.Feedback
                .Include(f => f.User)
                .Include(f => f.Reservation)
                .Where(f => f.CreatedAt >= start && f.CreatedAt <= end)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Feedback>> GetFeedbackByUserAsync(int userId)
        {
            try
            {
                return await _context.Feedback
                    .Include(f => f.Reservation)
                    .Include(f => f.ResolvedBy)
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback for user with ID {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Feedback>> GetFeedbackByReservationAsync(int reservationId)
        {
            try
            {
                return await _context.Feedback
                    .Include(f => f.User)
                    .Include(f => f.ResolvedBy)
                    .Where(f => f.ReservationId == reservationId)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback for reservation with ID {ReservationId}", reservationId);
                throw;
            }
        }

        // Business operations
        public async Task<Feedback> ResolveFeedbackAsync(int id, string resolutionNotes, int resolvedById)
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

                return await GetFeedbackByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving feedback with ID {FeedbackId}", id);
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

        public async Task<Dictionary<string, int>> GetFeedbackStatsByCategoryAsync()
        {
            try
            {
                var categoryCounts = await _context.Feedback
                    .Where(f => !string.IsNullOrEmpty(f.Category))
                    .GroupBy(f => f.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Category, x => x.Count);

                return categoryCounts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback statistics by category");
                throw;
            }
        }

        public async Task<Dictionary<int, int>> GetFeedbackStatsByRatingAsync()
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

                return ratingCounts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback statistics by rating");
                throw;
            }
        }
    }
}
