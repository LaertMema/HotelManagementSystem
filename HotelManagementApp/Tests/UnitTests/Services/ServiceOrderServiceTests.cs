// HotelManagementApp.Tests/UnitTests/Services/ServiceOrderServiceTests.cs
using HotelManagementApp.Models;
using HotelManagementApp.Models.DTOs.ServiceOrder;
using HotelManagementApp.Models.Enums;
using HotelManagementApp.Services.ServiceOrder;
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
    public class ServiceOrderServiceTests
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly Mock<ILogger<ServiceOrderService>> _mockLogger;

        public ServiceOrderServiceTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _mockLogger = new Mock<ILogger<ServiceOrderService>>();

            // Seed the test database
            SeedDatabase();
        }

        private void SeedDatabase()
        {
            using var context = new AppDbContext(_options);

            // Create test users
            var user1 = new ApplicationUser
            {
                Id = 1,
                UserName = "guest1@example.com",
                Email = "guest1@example.com",
                FirstName = "John",
                LastName = "Doe"
            };

            var staffMember = new ApplicationUser
            {
                Id = 2,
                UserName = "staff1@example.com",
                Email = "staff1@example.com",
                FirstName = "Staff",
                LastName = "Member"
            };

            context.Users.Add(user1);
            context.Users.Add(staffMember);

            // Create test room type and room
            var roomType = new RoomType
            {
                Id = 1,
                Name = "Standard",
                BasePrice = 100m
            };
            context.RoomTypes.Add(roomType);

            var room = new Room
            {
                Id = 1,
                RoomNumber = "101",
                RoomTypeId = 1,
                Status = RoomStatus.Occupied
            };
            context.Rooms.Add(room);

            // Create services
            var services = new List<Service>
            {
                new Service
                {
                    Id = 1,
                    ServiceName = "Room Cleaning",
                    Description = "Standard room cleaning service",
                    Price = 25.00m,
                    ServiceType = "Housekeeping",
                    IsActive = true
                },
                new Service
                {
                    Id = 2,
                    ServiceName = "Breakfast",
                    Description = "Breakfast delivered to room",
                    Price = 15.00m,
                    ServiceType = "Dining",
                    IsActive = true
                }
            };
            context.Services.AddRange(services);

            // Create a reservation
            var reservation = new Reservation
            {
                Id = 1,
                ReservationNumber = "RES-20250501-1234",
                UserId = 1,
                RoomId = 1,
                RoomTypeId = 1,
                CheckInDate = DateTime.UtcNow.AddDays(-1),
                CheckOutDate = DateTime.UtcNow.AddDays(3),
                Status = ReservationStatus.CheckedIn
            };
            context.Reservations.Add(reservation);

            // Create a service order
            var serviceOrder = new ServiceOrder
            {
                Id = 1,
                ReservationId = 1,
                ServiceId = 1,
                OrderDateTime = DateTime.UtcNow.AddHours(-2),
                Quantity = 1,
                PriceCharged = 25.00m,     // Important: Set the price charged
                TotalPrice = 25.00m,       // Important: Set the total price
                Status = ServiceOrderStatus.Pending,
                SpecialInstructions = "Please clean before noon",
                ScheduledTime = DateTime.UtcNow.AddHours(2),
                DeliveryLocation = "101"   // Room number
            };
            context.ServiceOrders.Add(serviceOrder);

            context.SaveChanges();
        }

        [Fact]
        public async Task CreateServiceOrderAsync_SetsCorrectPricesAndProperties()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ServiceOrderService(context, _mockLogger.Object);
            var createDto = new CreateServiceOrderDto
            {
                ReservationId = 1,
                ServiceId = 2,
                Quantity = 2,
                SpecialInstructions = "Extra toast please",
                ScheduledTime = DateTime.UtcNow.AddHours(3),
                DeliveryLocation = "101"
            };

            // Act
            var result = await service.CreateServiceOrderAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.ReservationId, result.ReservationId);
            Assert.Equal(createDto.ServiceId, result.ServiceId);
            Assert.Equal(createDto.Quantity, result.Quantity);
            Assert.Equal(15.00m, result.PricePerUnit);  // Price from the service
            Assert.Equal(30.00m, result.TotalPrice);    // 15.00 * 2
            Assert.Equal("Pending", result.Status);
            Assert.Equal(createDto.SpecialInstructions, result.SpecialInstructions);
            Assert.Equal(createDto.DeliveryLocation, result.DeliveryLocation);
            Assert.Equal(createDto.ScheduledTime, result.ScheduledTime);

            // Verify database record has the correct PriceCharged and TotalPrice
            var dbOrder = await context.ServiceOrders.FindAsync(result.Id);
            Assert.Equal(15.00m, dbOrder.PriceCharged);
            Assert.Equal(30.00m, dbOrder.TotalPrice);
        }

        [Fact]
        public async Task UpdateServiceOrderAsync_UpdatesAllProvidedFields()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ServiceOrderService(context, _mockLogger.Object);
            var updateDto = new UpdateServiceOrderDto
            {
                Quantity = 2,
                Status = ServiceOrderStatus.Completed,
                SpecialInstructions = "Updated instructions",
                CompletedById = 2,
                ScheduledTime = DateTime.UtcNow.AddHours(4),
                DeliveryLocation = "Front desk",
                CompletionNotes = "Service completed as requested"
            };

            // Act
            var result = await service.UpdateServiceOrderAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Quantity);
            Assert.Equal("Completed", result.Status);
            Assert.Equal(updateDto.SpecialInstructions, result.SpecialInstructions);
            Assert.Equal(updateDto.CompletedById, result.CompletedById);
            Assert.NotNull(result.CompletedAt);
            Assert.Equal(updateDto.ScheduledTime, result.ScheduledTime);
            Assert.Equal(updateDto.DeliveryLocation, result.DeliveryLocation);
            Assert.Equal(updateDto.CompletionNotes, result.CompletionNotes);

            // Verify database update
            var dbOrder = await context.ServiceOrders.FindAsync(1);
            Assert.Equal(2, dbOrder.Quantity);
            Assert.Equal(ServiceOrderStatus.Completed, dbOrder.Status);
            Assert.Equal(updateDto.SpecialInstructions, dbOrder.SpecialInstructions);
            Assert.Equal(updateDto.CompletedById, dbOrder.CompletedById);
            Assert.NotNull(dbOrder.CompletedAt);
            Assert.Equal(updateDto.ScheduledTime, dbOrder.ScheduledTime);
            Assert.Equal(updateDto.DeliveryLocation, dbOrder.DeliveryLocation);
            Assert.Equal(updateDto.CompletionNotes, dbOrder.CompletionNotes);
            Assert.Equal(50.00m, dbOrder.TotalPrice); // 25.00 * 2 (since we updated quantity to 2)
        }

        [Fact]
        public async Task CompleteServiceOrderAsync_SetsCompletionInformation()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ServiceOrderService(context, _mockLogger.Object);
            var notes = "Service completed on time";
            var staffId = 2;

            // Act
            var result = await service.CompleteServiceOrderAsync(1, notes, staffId);

            // Assert
            Assert.True(result);

            // Verify database update
            var dbOrder = await context.ServiceOrders.FindAsync(1);
            Assert.Equal(ServiceOrderStatus.Completed, dbOrder.Status);
            Assert.Equal(notes, dbOrder.CompletionNotes);
            Assert.Equal(staffId, dbOrder.CompletedById);
            Assert.NotNull(dbOrder.CompletedAt);
        }

        [Fact]
        public async Task CancelServiceOrderAsync_SetsCancellationInformation()
        {
            // Arrange - Create a new order to avoid conflicts with other tests
            using var context = new AppDbContext(_options);
            var newOrder = new ServiceOrder
            {
                ReservationId = 1,
                ServiceId = 2,
                OrderDateTime = DateTime.UtcNow,
                Quantity = 1,
                PriceCharged = 15.00m,
                TotalPrice = 15.00m,
                Status = ServiceOrderStatus.Pending
            };
            context.ServiceOrders.Add(newOrder);
            await context.SaveChangesAsync();

            var service = new ServiceOrderService(context, _mockLogger.Object);
            var reason = "Guest requested cancellation";

            // Act
            var result = await service.CancelServiceOrderAsync(newOrder.Id, reason);

            // Assert
            Assert.True(result);

            // Verify database update
            var dbOrder = await context.ServiceOrders.FindAsync(newOrder.Id);
            Assert.Equal(ServiceOrderStatus.Cancelled, dbOrder.Status);
            Assert.Contains(reason, dbOrder.CompletionNotes);
        }
    }
}

