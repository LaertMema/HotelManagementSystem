// Fix for ServiceOrderControllerTests.cs
using HotelManagementApp.Controllers;
using HotelManagementApp.Models;
using HotelManagementApp.Models.DTOs.ServiceOrder;
using HotelManagementApp.Models.Enums;
using HotelManagementApp.Services.ReservationServiceSpace;
using HotelManagementApp.Services.ServiceOrder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HotelManagementApp.Tests.IntegrationTests.Controllers
{
    public class ServiceOrderControllerTests
    {
        private readonly ServiceOrderService _mockServiceOrderService;
        private readonly ReservationService _mockReservationService;
        private readonly Mock<ILogger<ServiceOrderController>> _mockLogger;
        private readonly ServiceOrderController _controller;

        public ServiceOrderControllerTests()
        {
            // Create mocks for dependencies needed by ServiceOrderService and ReservationService
            var mockAppDbContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            var mockServiceOrderLogger = new Mock<ILogger<ServiceOrderService>>();
            var mockReservationLogger = new Mock<ILogger<ReservationService>>();
            var mockCleaningService = new Mock<Services.CleaningTaskSpace.ICleaningService>();
            var mockInvoiceService = new Mock<Services.InvoiceSpace.IInvoiceService>();

            // Create a mock for ServiceOrderService that wraps the real implementation
            var serviceOrderServiceMock = new Mock<ServiceOrderService>(
                mockAppDbContext.Object,
                mockServiceOrderLogger.Object)
            { CallBase = true };

            // Create a mock for ReservationService that wraps the real implementation
            var reservationServiceMock = new Mock<ReservationService>(
                mockAppDbContext.Object,
                mockReservationLogger.Object,
                mockCleaningService.Object,
                mockInvoiceService.Object)
            { CallBase = true };

            // Use the mocked service instances
            _mockServiceOrderService = serviceOrderServiceMock.Object;
            _mockReservationService = reservationServiceMock.Object;
            _mockLogger = new Mock<ILogger<ServiceOrderController>>();

            _controller = new ServiceOrderController(
                _mockServiceOrderService,
                _mockLogger.Object,
                _mockReservationService);
        }

        // Now we need to update all the setup methods to use Moq's SetupGet and SetupMethod instead of Setup
        // Because we're using concrete types with CallBase=true

        [Fact]
        public async Task GetAllServiceOrders_ReturnsOk()
        {
            // Arrange
            var serviceOrders = new List<ServiceOrderDto>
            {
                new ServiceOrderDto { Id = 1, ServiceName = "Room Cleaning", Status = "Pending" },
                new ServiceOrderDto { Id = 2, ServiceName = "Breakfast", Status = "Completed" }
            };

            // Setup using method replacement
            Mock.Get(_mockServiceOrderService)
                .Setup(s => s.GetAllServiceOrdersAsync())
                .ReturnsAsync(serviceOrders);

            // Act
            var result = await _controller.GetAllServiceOrders();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsAssignableFrom<IEnumerable<ServiceOrderDto>>(okResult.Value);
            Assert.Equal(2, returnValue.Count());
        }

        [Fact]
        public async Task CreateServiceOrder_ReturnsBadRequest_WhenReservationDoesNotExist()
        {
            // Arrange
            var createDto = new CreateServiceOrderDto
            {
                ReservationId = 999,
                ServiceId = 1,
                Quantity = 2
            };

            Mock.Get(_mockServiceOrderService)
                .Setup(s => s.CreateServiceOrderAsync(It.IsAny<CreateServiceOrderDto>()))
                .ThrowsAsync(new KeyNotFoundException("Reservation with ID 999 not found"));

            // Act
            var result = await _controller.CreateServiceOrder(createDto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateServiceOrder_ReturnsNotFound_ForNonExistingOrder()
        {
            // Arrange
            var updateDto = new UpdateServiceOrderDto
            {
                Quantity = 3,
                Status = ServiceOrderStatus.Completed
            };

            Mock.Get(_mockServiceOrderService)
                .Setup(s => s.UpdateServiceOrderAsync(999, It.IsAny<UpdateServiceOrderDto>()))
                .ThrowsAsync(new KeyNotFoundException("Service order with ID 999 not found"));

            // Act
            var result = await _controller.UpdateServiceOrder(999, updateDto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task CompleteServiceOrder_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var completeDto = new CompleteServiceOrderDto { Notes = "Service completed successfully" };
            int orderId = 1;
            int staffId = 2;

            // Setup the security context mock
            var mockSub = "2"; // Staff ID 2
            var mockUser = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                new[] { new System.Security.Claims.Claim("sub", mockSub) }
            ));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = mockUser }
            };

            Mock.Get(_mockServiceOrderService)
                .Setup(s => s.CompleteServiceOrderAsync(orderId, completeDto.Notes, staffId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CompleteServiceOrder(orderId, completeDto);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task CreateServiceOrder_ValidDto_ReturnsCreatedAtAction()
        {
            // Arrange
            var createDto = new CreateServiceOrderDto
            {
                ReservationId = 1,
                ServiceId = 1,
                Quantity = 2,
                SpecialInstructions = "Please deliver by 9am",
                ScheduledTime = DateTime.UtcNow.AddHours(2),
                DeliveryLocation = "Room 101"
            };

            var createdService = new ServiceOrderDto
            {
                Id = 1,
                ReservationId = 1,
                ReservationNumber = "RES-123456",
                ServiceId = 1,
                ServiceName = "Room Cleaning",
                ServiceDescription = "Standard room cleaning",
                ServiceCategory = "Housekeeping",
                OrderDateTime = DateTime.UtcNow,
                Quantity = 2,
                PricePerUnit = 25.00m,
                TotalPrice = 50.00m,
                Status = "Pending",
                SpecialInstructions = "Please deliver by 9am",
                DeliveryLocation = "Room 101",
                ScheduledTime = DateTime.UtcNow.AddHours(2),
                // Guest information would be populated too
                GuestId = 1,
                GuestName = "John Doe",
                RoomNumber = "101"
            };

            Mock.Get(_mockServiceOrderService)
                .Setup(s => s.CreateServiceOrderAsync(It.IsAny<CreateServiceOrderDto>()))
                .ReturnsAsync(createdService);

            // Act
            var result = await _controller.CreateServiceOrder(createDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(ServiceOrderController.GetServiceOrderById), createdAtActionResult.ActionName);
            var returnValue = Assert.IsType<ServiceOrderDto>(createdAtActionResult.Value);
            Assert.Equal(1, returnValue.Id);
            Assert.Equal(createDto.ReservationId, returnValue.ReservationId);
            Assert.Equal(createDto.ServiceId, returnValue.ServiceId);
            Assert.Equal(createDto.Quantity, returnValue.Quantity);
            Assert.Equal(createDto.SpecialInstructions, returnValue.SpecialInstructions);
            Assert.Equal(createDto.DeliveryLocation, returnValue.DeliveryLocation);
            Assert.Equal(createDto.ScheduledTime, returnValue.ScheduledTime);
        }
    }
}

