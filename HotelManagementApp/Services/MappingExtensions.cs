namespace HotelManagementApp.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using HotelManagementApp.Models.DTOs.HotelManagement.Models.DTOs;
    using HotelManagementApp.Models.DTOs;
    using HotelManagementApp.Models.Enums;
    using HotelManagementApp.Models;

    
        public static class MappingExtensions
        {
            // Feedback mappings
            public static Feedback ToFeedbackModel(this CreateFeedbackDto dto, int? userId = null)
            {
                return new Feedback
                {
                    UserId = userId,
                    ReservationId = dto.ReservationId,
                    GuestName = dto.GuestName,
                    GuestEmail = dto.GuestEmail,
                    Rating = dto.Rating,
                    Subject = dto.Subject,
                    Comments = dto.Comments,
                    Category = dto.Category,
                    IsResolved = false,
                    CreatedAt = System.DateTime.UtcNow
                };
            }

            public static void ApplyUpdateFeedbackDto(this Feedback feedback, UpdateFeedbackDto dto)
            {
                feedback.Rating = dto.Rating;
                feedback.Subject = dto.Subject;
                feedback.Comments = dto.Comments;
                feedback.Category = dto.Category;
            }

            public static FeedbackDto ToFeedbackDto(this Feedback feedback)
            {
                return new FeedbackDto
                {
                    Id = feedback.Id,
                    UserId = feedback.UserId,
                    UserName = feedback.User?.FirstName + " " + feedback.User?.LastName,
                    ReservationId = feedback.ReservationId,
                    ReservationNumber = feedback.Reservation?.ReservationNumber,
                    GuestName = feedback.GuestName,
                    GuestEmail = feedback.GuestEmail,
                    Rating = feedback.Rating,
                    Subject = feedback.Subject,
                    Comments = feedback.Comments,
                    Category = feedback.Category,
                    IsResolved = feedback.IsResolved,
                    ResolutionNotes = feedback.ResolutionNotes,
                    ResolvedById = feedback.ResolvedById,
                    ResolvedByName = feedback.ResolvedBy?.FirstName + " " + feedback.ResolvedBy?.LastName,
                    CreatedAt = feedback.CreatedAt,
                    ResolvedAt = feedback.ResolvedAt
                };
            }

            public static FeedbackSummaryDto ToFeedbackSummaryDto(this Feedback feedback)
            {
                return new FeedbackSummaryDto
                {
                    Id = feedback.Id,
                    GuestName = feedback.GuestName ?? feedback.User?.FirstName + " " + feedback.User?.LastName,
                    Rating = feedback.Rating,
                    Subject = feedback.Subject,
                    Category = feedback.Category,
                    IsResolved = feedback.IsResolved,
                    CreatedAt = feedback.CreatedAt
                };
            }

            public static IEnumerable<FeedbackDto> ToFeedbackDtos(this IEnumerable<Feedback> feedbacks)
            {
                return feedbacks.Select(f => f.ToFeedbackDto());
            }

            public static IEnumerable<FeedbackSummaryDto> ToFeedbackSummaryDtos(this IEnumerable<Feedback> feedbacks)
            {
                return feedbacks.Select(f => f.ToFeedbackSummaryDto());
            }

            public static FeedbackStatsByCategoryDto ToFeedbackStatsByCategoryDto(
                this Dictionary<string, int> categoryCounts)
            {
                return new FeedbackStatsByCategoryDto
                {
                    CategoryCounts = categoryCounts,
                    TotalFeedbackCount = categoryCounts.Values.Sum()
                };
            }

            public static FeedbackStatsByRatingDto ToFeedbackStatsByRatingDto(
                this Dictionary<int, int> ratingCounts, double averageRating)
            {
                return new FeedbackStatsByRatingDto
                {
                    RatingCounts = ratingCounts,
                    AverageRating = averageRating,
                    TotalFeedbackCount = ratingCounts.Values.Sum()
                };
            }

        // User mappings
        public static ApplicationUser ToApplicationUser(this RegisterDto registerDto)
        {
            if (registerDto == null)
                return null;

            return new ApplicationUser
            {
                UserName = registerDto.Username,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                PhoneNumber = registerDto.PhoneNumber,
                IsActive = true,
                AccountStatus = Models.Enums.AccountStatus.Active,
                PasswordResetRequired = false,
                RegistrationDate = DateTime.UtcNow,
                Created = DateTime.UtcNow
            };
        }

        public static UserDto ToUserDto(this ApplicationUser user, string role)
        {
            if (user == null)
                return null;

            return new UserDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Role = role,
                IsActive = user.IsActive,
                Created = user.Created,
                LastLogin = user.LastLogin
            };
        }
        public static string ToRoleString(this IList<string> roles)
        {
            if (roles == null || !roles.Any())
                return string.Empty;

            return string.Join(",", roles);
        }
        public static (string Username, string Password) ToCredentials(this LoginDto loginDto)
        {
            if (loginDto == null)
                return (null, null);

            return (loginDto.Username, loginDto.Password);
        }
        public static IEnumerable<UserDto> ToUserDtos(this IEnumerable<ApplicationUser> users, Dictionary<int, string> userRoles)
        {
            return users.Select(u => u.ToUserDto(userRoles.GetValueOrDefault(u.Id, string.Empty)));
        }
        public static AuthResponseDto ToAuthResponseDto(this ApplicationUser user, string token, DateTime expiration, string role)
        {
            if (user == null)
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Authentication failed: User not found"
                };

            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Authentication successful",
                Token = token,
                Expiration = expiration,
                User = user.ToUserDto(role)
            };
        }
        public static AuthResponseDto ToFailedAuthResponseDto(string errorMessage)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = $"Authentication failed: {errorMessage}"
            };
        }
        public static ApplicationUser UpdateFromDto(this ApplicationUser user, UpdateProfileDto updateProfileDto)
        {
            if (updateProfileDto == null || user == null)
                return user;

            // Only update properties that are provided in the DTO
            if (!string.IsNullOrEmpty(updateProfileDto.FirstName))
                user.FirstName = updateProfileDto.FirstName;

            if (!string.IsNullOrEmpty(updateProfileDto.LastName))
                user.LastName = updateProfileDto.LastName;

            if (!string.IsNullOrEmpty(updateProfileDto.Email))
                user.Email = updateProfileDto.Email;

            if (!string.IsNullOrEmpty(updateProfileDto.PhoneNumber))
                user.PhoneNumber = updateProfileDto.PhoneNumber;

            return user;
        }

        // Reservation mappings
        public static Reservation ToReservationModel(this CreateReservationDto dto, int userId, string reservationNumber, decimal totalPrice)
            {
                return new Reservation
                {
                    ReservationNumber = reservationNumber,
                    UserId = userId,
                    ReservationDate = DateTime.UtcNow,
                    CheckInDate = dto.CheckInDate,
                    CheckOutDate = dto.CheckOutDate,
                    Status = ReservationStatus.Pending,
                    TotalPrice = totalPrice,
                    PaymentMethod = dto.PaymentMethod,
                    PaymentStatus = PaymentStatus.Pending,
                    NumberOfGuests = dto.NumberOfGuests,
                    SpecialRequests = dto.SpecialRequests,
                    RoomId = null, // Room will be assigned later
                    RoomTypeId = dto.RoomTypeId,
                    CreatedBy = userId
                };
            }

            public static void ApplyUpdateReservationDto(this Reservation reservation, UpdateReservationDto dto)
            {
                if (dto.CheckInDate.HasValue)
                    reservation.CheckInDate = dto.CheckInDate.Value;

                if (dto.CheckOutDate.HasValue)
                    reservation.CheckOutDate = dto.CheckOutDate.Value;

                if (dto.RoomTypeId.HasValue)
                    reservation.RoomTypeId = dto.RoomTypeId.Value;

                if (dto.NumberOfGuests.HasValue)
                    reservation.NumberOfGuests = dto.NumberOfGuests.Value;

                if (!string.IsNullOrEmpty(dto.SpecialRequests))
                    reservation.SpecialRequests = dto.SpecialRequests;

                if (dto.PaymentMethod.HasValue)
                    reservation.PaymentMethod = dto.PaymentMethod.Value;
            }

            public static ReservationDto ToReservationDto(this Reservation reservation)
            {
                var userFullName = (reservation.User != null)
                    ? $"{reservation.User.FirstName} {reservation.User.LastName}"
                    : string.Empty;

                var checkedInByName = (reservation.CheckedInByUser != null)
                    ? $"{reservation.CheckedInByUser.FirstName} {reservation.CheckedInByUser.LastName}"
                    : string.Empty;

                var checkedOutByName = (reservation.CheckedOutByUser != null)
                    ? $"{reservation.CheckedOutByUser.FirstName} {reservation.CheckedOutByUser.LastName}"
                    : string.Empty;

                return new ReservationDto
                {
                    Id = reservation.Id,
                    ReservationNumber = reservation.ReservationNumber,
                    UserId = reservation.UserId,
                    UserName = userFullName,
                    UserEmail = reservation.User?.Email,
                    ReservationDate = reservation.ReservationDate,
                    CheckInDate = reservation.CheckInDate,
                    CheckOutDate = reservation.CheckOutDate,
                    Status = reservation.Status.ToString(),
                    TotalPrice = reservation.TotalPrice,
                    PaymentMethod = reservation.PaymentMethod.ToString(),
                    PaymentStatus = reservation.PaymentStatus.ToString(),
                    NumberOfGuests = reservation.NumberOfGuests,
                    SpecialRequests = reservation.SpecialRequests,

                    RoomId = reservation.RoomId,
                    RoomNumber = reservation.Room?.RoomNumber,
                    RoomTypeId = reservation.RoomTypeId,
                    RoomTypeName = reservation.RoomType?.Name,

                    CheckedInTime = reservation.CheckedInTime,
                    CheckedOutTime = reservation.CheckedOutTime,
                    CheckedInByUserName = checkedInByName,
                    CheckedOutByUserName = checkedOutByName,

                    CancelledAt = reservation.CancelledAt,
                    CancellationReason = reservation.CancellationReason,

                    ServiceOrdersCount = reservation.ServiceOrders?.Count ?? 0,
                    InvoicesCount = reservation.Invoices?.Count ?? 0,
                    FeedbackCount = reservation.Feedback?.Count ?? 0,
                    HasUnpaidInvoices = reservation.Invoices?.Any(i => i.Status != PaymentStatus.Paid) ?? false
                };
            }

            public static ReservationSummaryDto ToReservationSummaryDto(this Reservation reservation)
            {
                var guestName = (reservation.User != null)
                    ? $"{reservation.User.FirstName} {reservation.User.LastName}"
                    : string.Empty;

                return new ReservationSummaryDto
                {
                    Id = reservation.Id,
                    ReservationNumber = reservation.ReservationNumber,
                    GuestName = guestName,
                    CheckInDate = reservation.CheckInDate,
                    CheckOutDate = reservation.CheckOutDate,
                    Status = reservation.Status.ToString(),
                    RoomTypeName = reservation.RoomType?.Name,
                    RoomNumber = reservation.Room?.RoomNumber,
                    TotalPrice = reservation.TotalPrice,
                    PaymentStatus = reservation.PaymentStatus.ToString()
                };
            }

            public static IEnumerable<ReservationDto> ToReservationDtos(this IEnumerable<Reservation> reservations)
            {
                return reservations.Select(r => r.ToReservationDto());
            }

            public static IEnumerable<ReservationSummaryDto> ToReservationSummaryDtos(this IEnumerable<Reservation> reservations)
            {
                return reservations.Select(r => r.ToReservationSummaryDto());
            }
        }
    
}
