// HotelManagementApp/Tests/UnitTests/Services/ReservationServiceTests.cs
using HotelManagementApp.Models;
using HotelManagementApp.Models.DTOs;
using HotelManagementApp.Models.Enums;
using HotelManagementApp.Services.CleaningTaskSpace;
using HotelManagementApp.Services.InvoiceSpace;
using HotelManagementApp.Services.ReservationServiceSpace;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HotelManagementApp.Tests.UnitTests.Services
{
    public class ReservationServiceTests
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly Mock<ILogger<ReservationService>> _mockLogger;
        private readonly Mock<ICleaningService> _mockCleaningService;
        private readonly Mock<IInvoiceService> _mockInvoiceService;

        public ReservationServiceTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _mockLogger = new Mock<ILogger<ReservationService>>();
            _mockCleaningService = new Mock<ICleaningService>();
            _mockInvoiceService = new Mock<IInvoiceService>();
            // Configure the mock to return a valid InvoiceNumber
            _mockInvoiceService
                .Setup(service => service.GenerateInvoiceNumberAsync())
                .ReturnsAsync("INV-20230501-0001");

            // Seed the test database
            SeedDatabase();
        }

        private void SeedDatabase()
        {
            using var context = new AppDbContext(_options);

            // Create room types
            var roomTypes = new List<RoomType>
    {
        new RoomType
        {
            Id = 1,
            Name = "Standard",
            BasePrice = 100m,
            Capacity = 2,
            Description = "Standard room",
            Amenities = "WiFi,TV,Air conditioning",
            ImageUrl = "img/room-standard.jpg"
        },
        new RoomType
        {
            Id = 2,
            Name = "Deluxe",
            BasePrice = 150m,
            Capacity = 2,
            Description = "Deluxe room",
            Amenities = "WiFi,TV,Mini-bar,Air conditioning",
            ImageUrl = "img/room-deluxe.jpg"
        }
    };
            context.RoomTypes.AddRange(roomTypes);

            // Create rooms
            var rooms = new List<Room>
    {
        new Room {
            Id = 1,
            RoomNumber = "101",
            Floor = 1,
            RoomTypeId = 1,
            Status = RoomStatus.Available,
            BasePrice = 100m,
            Notes = "Standard room on the first floor"
        },
        new Room {
            Id = 2,
            RoomNumber = "102",
            Floor = 1,
            RoomTypeId = 2,
            Status = RoomStatus.Available,
            BasePrice = 150m,
            Notes = "Deluxe room on the first floor"
        },
        new Room {
            Id = 3,
            RoomNumber = "201",
            Floor = 2,
            RoomTypeId = 1,
            Status = RoomStatus.Maintenance,
            BasePrice = 100m,
            Notes = "Standard room under maintenance"
        }
    };
            context.Rooms.AddRange(rooms);

            // Create users
            var users = new List<ApplicationUser>
    {
        new ApplicationUser
        {
            Id = 1,
            UserName = "guest1@example.com",
            Email = "guest1@example.com",
            FirstName = "John",
            LastName = "Doe",
            PasswordResetRequired = false,
            LastLogin = null,
            IdType = "Passport",
            IdNumber = "A12345678"
        },
        new ApplicationUser
        {
            Id = 2,
            UserName = "guest2@example.com",
            Email = "guest2@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            PasswordResetRequired = false,
            LastLogin = null,
            IdType = "Driver's License",
            IdNumber = "D98765432"
        },
        new ApplicationUser
        {
            Id = 3,
            UserName = "staff1@example.com",
            Email = "staff1@example.com",
            FirstName = "Staff",
            LastName = "Member",
            PasswordResetRequired = true,
            LastLogin = DateTime.Today.AddDays(-1),
            IdType = "Employee ID",
            IdNumber = "EMP12345"
        }
    };
            context.Users.AddRange(users);

            // Create reservations
            var reservations = new List<Reservation>
    {
        new Reservation
        {
            Id = 1,
            ReservationNumber = "RES-20250501-1234",
            UserId = 1,
            RoomId = 1,
            RoomTypeId = 1,
            CheckInDate = DateTime.Today.AddDays(-2),
            CheckOutDate = DateTime.Today.AddDays(3),
            Status = ReservationStatus.CheckedIn,
            ReservationDate = DateTime.Today.AddDays(-5),
            NumberOfGuests = 2,
            TotalPrice = 500m,
            PaymentMethod = PaymentMethod.CreditCard,
            PaymentStatus = PaymentStatus.Paid,
            SpecialRequests = "Early check-in requested",
            CheckedInTime = DateTime.Today.AddDays(-2).AddHours(14),
            CheckedInBy = 3,
            CreatedBy = 2,
            ServiceOrders = new List<ServiceOrder>(),
            Payments = new List<Payment>(),
            Invoices = new List<Invoice>()
        },
        new Reservation
        {
            Id = 2,
            ReservationNumber = "RES-20250502-5678",
            UserId = 2,
            RoomId = null,
            RoomTypeId = 2,
            CheckInDate = DateTime.Today.AddDays(5),
            CheckOutDate = DateTime.Today.AddDays(10),
            Status = ReservationStatus.Confirmed,
            ReservationDate = DateTime.Today.AddDays(-1),
            NumberOfGuests = 2,
            TotalPrice = 750m,
            PaymentMethod = PaymentMethod.DebitCard,
            PaymentStatus = PaymentStatus.Pending,
            SpecialRequests = null,
            CreatedBy = 2,
            ServiceOrders = new List<ServiceOrder>(),
            Payments = new List<Payment>(),
            Invoices = new List<Invoice>()
        },
        new Reservation
        {
            Id = 3,
            ReservationNumber = "RES-20250503-9012",
            UserId = 1,
            RoomId = 2,
            RoomTypeId = 2,
            CheckInDate = DateTime.Today,
            CheckOutDate = DateTime.Today.AddDays(3),
            Status = ReservationStatus.Confirmed,
            ReservationDate = DateTime.Today.AddDays(-3),
            NumberOfGuests = 1,
            TotalPrice = 450m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentStatus = PaymentStatus.Pending,
            SpecialRequests = "Late check-out if possible",
            CreatedBy = 3,
            ServiceOrders = new List<ServiceOrder>(),
            Payments = new List<Payment>(),
            Invoices = new List<Invoice>()
        },
        new Reservation
        {
            Id = 4,
            ReservationNumber = "RES-20250504-3456",
            UserId = 2,
            RoomId = null,
            RoomTypeId = 1,
            CheckInDate = DateTime.Today.AddDays(10),
            CheckOutDate = DateTime.Today.AddDays(15),
            Status = ReservationStatus.Confirmed,
            ReservationDate = DateTime.Today.AddDays(-2),
            NumberOfGuests = 3,
            TotalPrice = 600m,
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentStatus = PaymentStatus.Pending,
            SpecialRequests = "Additional bed needed",
            CreatedBy = 3,
            ServiceOrders = new List<ServiceOrder>(),
            Payments = new List<Payment>(),
            Invoices = new List<Invoice>()
        }
    };
            context.Reservations.AddRange(reservations);

            // Create invoices
            var invoices = new List<Invoice>
    {
        new Invoice
        {
            Id = 1,
            InvoiceNumber = "INV-20250501-1",
            ReservationId = 1,
            Amount = 500m,
            Tax = 50m,
            Total = 550m,
            CreatedAt = DateTime.Today.AddDays(-5),
            DueDate = DateTime.Today.AddDays(3),
            Notes = "Standard room invoice"
        },
        new Invoice
        {
            Id = 2,
            InvoiceNumber = "INV-20250502-2",
            ReservationId = 2,
            Amount = 750m,
            Tax = 75m,
            Total = 825m,
            CreatedAt = DateTime.Today.AddDays(-1),
            DueDate = DateTime.Today.AddDays(10),
            Notes = "Deluxe room invoice"
        }
    };
            context.Invoices.AddRange(invoices);

            // Associate invoices with reservations
            reservations[0].Invoices = new List<Invoice> { invoices[0] };
            reservations[1].Invoices = new List<Invoice> { invoices[1] };

            context.SaveChanges();
        }


        [Fact]
        public async Task GetAllReservationsAsync_ReturnsAllReservations()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);

            // Act
            var result = await service.GetAllReservationsAsync();

            // Assert
            var reservationList = result.ToList();
            Assert.Equal(4, reservationList.Count);
            Assert.Contains(reservationList, r => r.ReservationNumber == "RES-20250501-1234");
            Assert.Contains(reservationList, r => r.ReservationNumber == "RES-20250502-5678");
            Assert.Contains(reservationList, r => r.ReservationNumber == "RES-20250503-9012");
        }

        [Fact]
        public async Task GetReservationByIdAsync_ExistingId_ReturnsReservation()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);

            // Act
            var result = await service.GetReservationByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("RES-20250501-1234", result.ReservationNumber);
            Assert.Equal(1, result.UserId);
            Assert.Equal(1, result.RoomId);
            Assert.Equal(ReservationStatus.CheckedIn, result.Status);
        }

        [Fact]
        public async Task GetReservationByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);

            // Act
            var result = await service.GetReservationByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateReservationAsync_ValidReservation_ReturnsCreatedReservation()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);

            var reservation = new Reservation
            {
                UserId = 2,
                RoomTypeId = 1,
                CheckInDate = DateTime.Today.AddDays(20),
                CheckOutDate = DateTime.Today.AddDays(25),
                NumberOfGuests = 2,
                Status = ReservationStatus.Confirmed
            };

            // Act
            var result = await service.CreateReservationAsync(reservation);

            // Assert
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.ReservationNumber)); // Ensure ReservationNumber is not null or empty
            Assert.Equal(2, result.UserId);
            Assert.Equal(1, result.RoomTypeId);

            // Verify it was added to the database
            var dbReservation = await context.Reservations.FindAsync(result.Id);
            Assert.NotNull(dbReservation);
            Assert.False(string.IsNullOrEmpty(dbReservation.ReservationNumber)); // Ensure ReservationNumber is not null or empty
        }


        [Fact]
        public async Task CreateReservationAsync_UnavailableRoomType_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);

            var reservation = new Reservation
            {
                ReservationNumber = "RES-20250505-7890",
                UserId = 2,
                RoomTypeId = 1,
                CheckInDate = DateTime.Today,  // Overlaps with existing reservation for Room 1 (RoomTypeId 1)
                CheckOutDate = DateTime.Today.AddDays(5),
                NumberOfGuests = 2,
                Status = ReservationStatus.Confirmed
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateReservationAsync(reservation));
        }

        [Fact]
        public async Task UpdateReservationAsync_ValidReservation_ReturnsUpdatedReservation()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);

            var reservation = await context.Reservations.FindAsync(2);
            reservation.NumberOfGuests = 3;
            reservation.SpecialRequests = "Late check-in requested";

            // Act
            var result = await service.UpdateReservationAsync(reservation);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.NumberOfGuests);
            Assert.Equal("Late check-in requested", result.SpecialRequests);

            // Verify it was updated in the database
            var dbReservation = await context.Reservations.FindAsync(2);
            Assert.Equal(3, dbReservation.NumberOfGuests);
            Assert.Equal("Late check-in requested", dbReservation.SpecialRequests);
        }

        [Fact]
        public async Task UpdateReservationAsync_NonExistingReservation_ThrowsKeyNotFoundException()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);

            var reservation = new Reservation
            {
                Id = 999,
                ReservationNumber = "RES-20250506-1111",
                UserId = 1,
                RoomTypeId = 1
            };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateReservationAsync(reservation));
        }

        [Fact]
        public async Task DeleteReservationAsync_ExistingId_ReturnsTrue()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);

            // Act
            var result = await service.DeleteReservationAsync(3);

            // Assert
            Assert.True(result);

            // Verify it was deleted from the database
            var dbReservation = await context.Reservations.FindAsync(3);
            Assert.Null(dbReservation);
        }

        [Fact]
        public async Task DeleteReservationAsync_NonExistingId_ReturnsFalse()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);

            // Act
            var result = await service.DeleteReservationAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetReservationsByUserAsync_ReturnsUserReservations()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);

            // Act
            var result = await service.GetReservationsByUserAsync(1);

            // Assert
            var reservations = result.ToList();
            Assert.Equal(2, reservations.Count);
            Assert.All(reservations, r => Assert.Equal(1, r.UserId));
        }

        [Fact]
        public async Task GetReservationsByStatusAsync_ReturnsReservationsWithStatus()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);

            // Act
            var result = await service.GetReservationsByStatusAsync(ReservationStatus.Confirmed);

            // Assert
            var reservations = result.ToList();
            Assert.Equal(3, reservations.Count);
            Assert.All(reservations, r => Assert.Equal(ReservationStatus.Confirmed, r.Status));
        }

        [Fact]
        public async Task GetReservationsByDateRangeAsync_ReturnsReservationsInRange()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddDays(5);

            // Act
            var result = await service.GetReservationsByDateRangeAsync(startDate, endDate);

            // Assert
            var reservations = result.ToList();
            Assert.Equal(3, reservations.Count); // All reservations overlap with this date range
        }

        [Fact]
        public async Task GetTodayArrivalsAsync_ReturnsTodayArrivals()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);

            // Act
            var result = await service.GetTodayArrivalsAsync();

            // Assert
            var arrivals = result.ToList();
            Assert.Single(arrivals);
            Assert.Equal(3, arrivals[0].Id); // Reservation 3 is arriving today
        }

        [Fact]
        public async Task CancelReservationAsync_ValidReservation_ReturnsTrue()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);
            string cancellationReason = "Change of plans";

            // Act
            var result = await service.CancelReservationAsync(2, cancellationReason);

            // Assert
            Assert.True(result);

            // Verify the reservation was cancelled
            var dbReservation = await context.Reservations.FindAsync(2);
            Assert.Equal(ReservationStatus.Cancelled, dbReservation.Status);
            Assert.NotNull(dbReservation.CancelledAt);
            Assert.Contains(cancellationReason, dbReservation.CancellationReason);
        }

        [Fact]
        public async Task CancelReservationAsync_AlreadyCheckedIn_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CancelReservationAsync(1, "Cannot cancel")); // Reservation 1 is already checked in
        }

        [Fact]
        public async Task IsRoomAvailableAsync_AvailableRoom_ReturnsTrue()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);
            var checkIn = DateTime.Today.AddDays(15);
            var checkOut = DateTime.Today.AddDays(20);

            // Act
            var result = await service.IsRoomAvailableAsync(1, checkIn, checkOut);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsRoomAvailableAsync_UnavailableRoom_ReturnsFalse()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);
            var checkIn = DateTime.Today; // Overlaps with reservation 1
            var checkOut = DateTime.Today.AddDays(2);

            // Act
            var result = await service.IsRoomAvailableAsync(1, checkIn, checkOut);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetReservationStatsAsync_ReturnsCorrectStats()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ReservationService(context, _mockLogger.Object, _mockCleaningService.Object, _mockInvoiceService.Object);

            // Act
            var result = await service.GetReservationStatsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result["Total"]); // 3 total reservations
            Assert.Equal(1, result["CheckedIn"]); // 1 checked in
            Assert.Equal(3, result["Confirmed"]); // 2 confirmed/reserved
        }
    }
}
