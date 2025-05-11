using HotelManagementApp.Controllers;
using HotelManagementApp.Models;
using HotelManagementApp.Models.DTOs;
using HotelManagementApp.Models.Enums;
using HotelManagementApp.Services.CleaningTaskSpace;
using HotelManagementApp.Services.InvoiceSpace;
using HotelManagementApp.Services.ReservationServiceSpace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace HotelManagementApp.Tests.IntegrationTests.Controllers
{
    public class ReservationControllerTests
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly Mock<ILogger<ReservationController>> _mockLogger;
        private readonly Mock<ILogger<ReservationService>> _mockServiceLogger;
        private readonly Mock<ICleaningService> _mockCleaningService;
        private readonly Mock<IInvoiceService> _mockInvoiceService;
        private readonly ReservationService _reservationService;
        private readonly ReservationController _controller;

        public ReservationControllerTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"ReservationControllerTests_{Guid.NewGuid()}")
                .Options;

            _mockLogger = new Mock<ILogger<ReservationController>>();
            _mockServiceLogger = new Mock<ILogger<ReservationService>>();
            _mockCleaningService = new Mock<ICleaningService>();
            _mockInvoiceService = new Mock<IInvoiceService>();

            // Configure the mock to return a valid InvoiceNumber
            _mockInvoiceService
                .Setup(service => service.GenerateInvoiceNumberAsync())
                .ReturnsAsync("INV-20230501-0001");

            // Seed the test database
            SeedDatabase();

            using var context = new AppDbContext(_options);
            _reservationService = new ReservationService(
                context,
                _mockServiceLogger.Object,
                _mockCleaningService.Object,
                _mockInvoiceService.Object
            );

            _controller = new ReservationController(_reservationService, _mockLogger.Object);

            // Setup HttpContext with authenticated user claims for a manager
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "manager@hotel.com"),
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("sub", "1"),
                new Claim(ClaimTypes.Role, "Manager")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
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
        public async Task GetAllReservations_ReturnsAllReservations()
        {
            // Act
            var result = await _controller.GetAllReservations();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var reservations = Assert.IsAssignableFrom<IEnumerable<ReservationDto>>(okResult.Value);
            Assert.Equal(4, reservations.Count());
            Assert.Contains(reservations, r => r.ReservationNumber == "RES-20250501-1234");
            Assert.Contains(reservations, r => r.ReservationNumber == "RES-20250502-5678");
        }

        [Fact]
        public async Task GetReservationById_ExistingId_ReturnsReservation()
        {
            // Act
            var result = await _controller.GetReservationById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var reservation = Assert.IsType<ReservationDto>(okResult.Value);
            Assert.Equal("RES-20250501-1234", reservation.ReservationNumber);
            Assert.Equal(1, reservation.UserId);
            Assert.Equal(1, reservation.RoomId);
            Assert.Equal(nameof(ReservationStatus.CheckedIn), reservation.Status);
        }

        [Fact]
        public async Task GetReservationById_NonExistingId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.GetReservationById(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetReservationsByStatus_ReturnsReservationsWithStatus()
        {
            // Act
            var result = await _controller.GetReservationsByStatus(ReservationStatus.Confirmed);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var reservations = Assert.IsAssignableFrom<IEnumerable<ReservationDto>>(okResult.Value);
            Assert.Equal(3, reservations.Count());
            Assert.All(reservations, r => Assert.Equal(nameof(ReservationStatus.Confirmed), r.Status));
        }

        [Fact]
        public async Task GetReservationsByDateRange_ReturnsReservationsInRange()
        {
            // Arrange
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddDays(5);

            // Act
            var result = await _controller.GetReservationsByDateRange(startDate, endDate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var reservations = Assert.IsAssignableFrom<IEnumerable<ReservationDto>>(okResult.Value);
            Assert.Equal(3, reservations.Count());
        }

        [Fact]
        public async Task GetTodayArrivals_ReturnsTodayArrivals()
        {
            // Act
            var result = await _controller.GetTodayArrivals();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var arrivals = Assert.IsAssignableFrom<IEnumerable<ReservationDto>>(okResult.Value);
            Assert.Single(arrivals);
            Assert.Equal(3, arrivals.First().Id);
        }

        [Fact]
        public async Task CreateReservation_ValidData_ReturnsCreatedReservation()
        {
            // Arrange
            var createDto = new CreateReservationDto
            {
                RoomTypeId = 1,
                CheckInDate = DateTime.Today.AddDays(20),
                CheckOutDate = DateTime.Today.AddDays(25),
                NumberOfGuests = 2,
                PaymentMethod = PaymentMethod.CreditCard,
                SpecialRequests = "Extra pillows"
            };

            // Act
            var result = await _controller.CreateReservation(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var reservation = Assert.IsType<ReservationDto>(createdResult.Value);
            Assert.Equal(1, reservation.RoomTypeId);
            Assert.Equal(2, reservation.NumberOfGuests);
            Assert.Equal("Extra pillows", reservation.SpecialRequests);
            Assert.Equal(nameof(PaymentMethod.CreditCard), reservation.PaymentMethod);
        }

        [Fact]
        public async Task CancelReservation_ValidReservation_ReturnsOk()
        {
            // Arrange
            var cancelDto = new CancelReservationDto
            {
                CancellationReason = "Change of plans"
            };

            // Act
            var result = await _controller.CancelReservation(2, cancelDto);

            // Assert
            Assert.IsType<OkResult>(result);

            // Verify the reservation was cancelled
            using var context = new AppDbContext(_options);
            var dbReservation = await context.Reservations.FindAsync(2);
            Assert.Equal(ReservationStatus.Cancelled, dbReservation.Status);
            Assert.NotNull(dbReservation.CancelledAt);
            Assert.Contains(cancelDto.CancellationReason, dbReservation.CancellationReason);
        }

        [Fact]
        public async Task CancelReservation_AlreadyCheckedIn_ReturnsBadRequest()
        {
            // Arrange
            var cancelDto = new CancelReservationDto
            {
                CancellationReason = "Cannot cancel"
            };

            // Act
            var result = await _controller.CancelReservation(1, cancelDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetReservationStats_ReturnsCorrectStats()
        {
            // Act
            var result = await _controller.GetReservationStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var stats = Assert.IsType<Dictionary<string, int>>(okResult.Value);
            Assert.Equal(4, stats["Total"]);
            Assert.Equal(1, stats["CheckedIn"]);
            Assert.Equal(3, stats["Confirmed"]);
        }
    }
}

