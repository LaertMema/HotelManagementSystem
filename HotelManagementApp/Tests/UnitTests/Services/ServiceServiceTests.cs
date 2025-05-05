// HotelManagementApp.Tests/UnitTests/Services/ServiceServiceTests.cs
using HotelManagementApp.Models;
using HotelManagementApp.Models.DTOs.Service;
using HotelManagementApp.Services.ServiceService;
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
    public class ServiceServiceTests
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly Mock<ILogger<ServiceService>> _mockLogger;

        public ServiceServiceTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _mockLogger = new Mock<ILogger<ServiceService>>();

            // Seed the test database
            using var context = new AppDbContext(_options);
            context.Services.AddRange(new List<Service>
            {
                new Service { Id = 1, ServiceName = "Room Cleaning", Description = "Standard room cleaning", Price = 25.00m, ServiceType = "Housekeeping", IsActive = true },
                new Service { Id = 2, ServiceName = "Breakfast", Description = "Breakfast in room", Price = 15.00m, ServiceType = "Dining", IsActive = true },
                new Service { Id = 3, ServiceName = "Massage", Description = "Spa massage", Price = 80.00m, ServiceType = "Wellness", IsActive = false }
            });
            context.SaveChanges();
        }

        [Fact]
        public async Task GetAllServicesAsync_ReturnsAllServices()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ServiceService(context, _mockLogger.Object);

            // Act
            var result = await service.GetAllServicesAsync();

            // Assert
            var serviceList = result.ToList();
            Assert.Equal(3, serviceList.Count);
        }

        [Fact]
        public async Task GetServiceByIdAsync_ExistingId_ReturnsService()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ServiceService(context, _mockLogger.Object);

            // Act
            var result = await service.GetServiceByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Room Cleaning", result.ServiceName);
            Assert.Equal("Housekeeping", result.ServiceType);
        }

        [Fact]
        public async Task GetServiceByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ServiceService(context, _mockLogger.Object);

            // Act
            var result = await service.GetServiceByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateServiceAsync_ValidDto_ReturnsCreatedService()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ServiceService(context, _mockLogger.Object);
            var createDto = new CreateServiceDto
            {
                ServiceName = "Airport Shuttle",
                Description = "Transportation to/from airport",
                Price = 50.00m,
                ServiceType = "Transportation",
                IsActive = true
            };

            // Act
            var result = await service.CreateServiceAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.ServiceName, result.ServiceName);
            Assert.Equal(createDto.ServiceType, result.ServiceType);
            Assert.Equal(createDto.Price, result.Price);

            // Verify it was added to the database
            var dbService = await context.Services.FindAsync(result.Id);
            Assert.NotNull(dbService);
        }

        [Fact]
        public async Task UpdateServiceAsync_ValidDto_ReturnsUpdatedService()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ServiceService(context, _mockLogger.Object);
            var updateDto = new UpdateServiceDto
            {
                ServiceName = "Deluxe Room Cleaning",
                Price = 35.00m
            };

            // Act
            var result = await service.UpdateServiceAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.ServiceName, result.ServiceName);
            Assert.Equal(updateDto.Price, result.Price);

            // Original properties should be preserved
            Assert.Equal("Housekeeping", result.ServiceType);

            // Verify it was updated in the database
            var dbService = await context.Services.FindAsync(1);
            Assert.Equal(updateDto.ServiceName, dbService.ServiceName);
            Assert.Equal(updateDto.Price, dbService.Price);
        }

        [Fact]
        public async Task UpdateServiceAsync_NonExistingId_ThrowsKeyNotFoundException()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ServiceService(context, _mockLogger.Object);
            var updateDto = new UpdateServiceDto
            {
                ServiceName = "Updated Name"
            };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await service.UpdateServiceAsync(999, updateDto));
        }

        [Fact]
        public async Task DeleteServiceAsync_ExistingId_ReturnsTrue()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ServiceService(context, _mockLogger.Object);

            // Act
            var result = await service.DeleteServiceAsync(1);

            // Assert
            Assert.True(result);

            // Verify it was deleted from the database
            var dbService = await context.Services.FindAsync(1);
            Assert.Null(dbService);
        }

        [Fact]
        public async Task DeleteServiceAsync_NonExistingId_ReturnsFalse()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ServiceService(context, _mockLogger.Object);

            // Act
            var result = await service.DeleteServiceAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetServicesByTypeAsync_ReturnsServicesOfType()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ServiceService(context, _mockLogger.Object);

            // Act
            var result = await service.GetServicesByTypeAsync("Housekeeping");

            // Assert
            var serviceList = result.ToList();
            Assert.Single(serviceList);
            Assert.Equal("Room Cleaning", serviceList[0].ServiceName);
        }

        [Fact]
        public async Task GetActiveServicesAsync_ReturnsActiveServices()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var service = new ServiceService(context, _mockLogger.Object);

            // Act
            var result = await service.GetActiveServicesAsync();

            // Assert
            var serviceList = result.ToList();
            Assert.Equal(2, serviceList.Count);
            Assert.All(serviceList, s => Assert.True(s.IsActive));
        }
    }
}
