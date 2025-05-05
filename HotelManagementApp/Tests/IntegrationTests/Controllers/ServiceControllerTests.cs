// HotelManagementApp.Tests/IntegrationTests/Controllers/ServiceControllerTests.cs
using HotelManagementApp.Controllers;
using HotelManagementApp.Models.DTOs.Service;
using HotelManagementApp.Services.ServiceService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HotelManagementApp.Tests.IntegrationTests.Controllers
{
    public class ServiceControllerTests
    {
        private readonly Mock<IServiceService> _mockServiceService;
        private readonly Mock<ILogger<ServiceController>> _mockLogger;
        private readonly ServiceController _controller;

        public ServiceControllerTests()
        {
            _mockServiceService = new Mock<IServiceService>();
            _mockLogger = new Mock<ILogger<ServiceController>>();
            _controller = new ServiceController(_mockServiceService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllServices_ReturnsOkWithServices()
        {
            // Arrange
            var services = new List<ServiceDto>
            {
                new ServiceDto { Id = 1, ServiceName = "Room Cleaning", ServiceType = "Housekeeping" },
                new ServiceDto { Id = 2, ServiceName = "Breakfast", ServiceType = "Dining" }
            };

            _mockServiceService
                .Setup(s => s.GetAllServicesAsync())
                .ReturnsAsync(services);

            // Act
            var result = await _controller.GetAllServices();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedServices = Assert.IsAssignableFrom<IEnumerable<ServiceDto>>(okResult.Value);
            Assert.Equal(2, returnedServices.Count());
        }

        [Fact]
        public async Task GetServiceById_ExistingId_ReturnsOkWithService()
        {
            // Arrange
            var service = new ServiceDto
            {
                Id = 1,
                ServiceName = "Room Cleaning",
                ServiceType = "Housekeeping"
            };

            _mockServiceService
                .Setup(s => s.GetServiceByIdAsync(1))
                .ReturnsAsync(service);

            // Act
            var result = await _controller.GetServiceById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedService = Assert.IsType<ServiceDto>(okResult.Value);
            Assert.Equal(1, returnedService.Id);
            Assert.Equal("Room Cleaning", returnedService.ServiceName);
        }

        [Fact]
        public async Task GetServiceById_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockServiceService
                .Setup(s => s.GetServiceByIdAsync(999))
                .ReturnsAsync((ServiceDto)null);

            // Act
            var result = await _controller.GetServiceById(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateService_ValidDto_ReturnsCreatedAtAction()
        {
            // Arrange
            var createDto = new CreateServiceDto
            {
                ServiceName = "Airport Shuttle",
                Description = "Transportation to/from airport",
                Price = 50.00m,
                ServiceType = "Transportation",
                IsActive = true
            };

            var createdService = new ServiceDto
            {
                Id = 4,
                ServiceName = "Airport Shuttle",
                Description = "Transportation to/from airport",
                Price = 50.00m,
                ServiceType = "Transportation",
                IsActive = true
            };

            _mockServiceService
                .Setup(s => s.CreateServiceAsync(It.IsAny<CreateServiceDto>()))
                .ReturnsAsync(createdService);

            // Act
            var result = await _controller.CreateService(createDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(ServiceController.GetServiceById), createdAtActionResult.ActionName);
            var returnValue = Assert.IsType<ServiceDto>(createdAtActionResult.Value);
            Assert.Equal(4, returnValue.Id);
        }

        [Fact]
        public async Task UpdateService_ValidDto_ReturnsOkWithUpdatedService()
        {
            // Arrange
            var updateDto = new UpdateServiceDto
            {
                ServiceName = "Deluxe Room Cleaning",
                Price = 35.00m
            };

            var updatedService = new ServiceDto
            {
                Id = 1,
                ServiceName = "Deluxe Room Cleaning",
                Price = 35.00m,
                ServiceType = "Housekeeping",
                IsActive = true
            };

            _mockServiceService
                .Setup(s => s.UpdateServiceAsync(1, It.IsAny<UpdateServiceDto>()))
                .ReturnsAsync(updatedService);

            // Act
            var result = await _controller.UpdateService(1, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<ServiceDto>(okResult.Value);
            Assert.Equal("Deluxe Room Cleaning", returnValue.ServiceName);
            Assert.Equal(35.00m, returnValue.Price);
        }

        [Fact]
        public async Task UpdateService_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var updateDto = new UpdateServiceDto
            {
                ServiceName = "Updated Name"
            };

            _mockServiceService
                .Setup(s => s.UpdateServiceAsync(999, It.IsAny<UpdateServiceDto>()))
                .ThrowsAsync(new KeyNotFoundException("Service with ID 999 not found"));

            // Act
            var result = await _controller.UpdateService(999, updateDto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task DeleteService_ExistingId_ReturnsNoContent()
        {
            // Arrange
            _mockServiceService
                .Setup(s => s.DeleteServiceAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteService(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteService_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockServiceService
                .Setup(s => s.DeleteServiceAsync(999))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteService(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}