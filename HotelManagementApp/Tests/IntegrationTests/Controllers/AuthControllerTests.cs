// HotelManagementApp.Tests/IntegrationTests/Controllers/AuthControllerTests.cs
using HotelManagementApp.Controllers;
using HotelManagementApp.Models;
using HotelManagementApp.Services.Authentication;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace HotelManagementApp.Tests.IntegrationTests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthenticationService> _mockAuthService;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthenticationService>();
            _controller = new AuthController(_mockAuthService.Object);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginModel = new LoginModel
            {
                Username = "testuser",
                Password = "Password123!"
            };

            var user = new ApplicationUser { Id = 1, UserName = "testuser" };
            _mockAuthService
                .Setup(x => x.LoginAsync(loginModel.Username, loginModel.Password))
                .ReturnsAsync(user);

            _mockAuthService
                .Setup(x => x.GenerateJwtTokenAsync(user))
                .ReturnsAsync("jwt-token-here");

            // Act
            var result = await _controller.Login(loginModel);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic tokenResult = okResult.Value;
            Assert.Equal("jwt-token-here", tokenResult.Token);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginModel = new LoginModel
            {
                Username = "testuser",
                Password = "wrongpassword"
            };

            _mockAuthService
                .Setup(x => x.LoginAsync(loginModel.Username, loginModel.Password))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.Login(loginModel);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Register_ValidData_ReturnsOk()
        {
            // Arrange
            var registerModel = new RegisterModel
            {
                Username = "newuser",
                Email = "newuser@example.com",
                FirstName = "New",
                LastName = "User",
                Password = "Password123!"
            };

            _mockAuthService
                .Setup(x => x.RegisterUserAsync(It.IsAny<ApplicationUser>(), registerModel.Password))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Register(registerModel);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Register_InvalidData_ReturnsBadRequest()
        {
            // Arrange
            var registerModel = new RegisterModel
            {
                Username = "newuser",
                Email = "newuser@example.com",
                FirstName = "New",
                LastName = "User",
                Password = "Password123!"
            };

            _mockAuthService
                .Setup(x => x.RegisterUserAsync(It.IsAny<ApplicationUser>(), registerModel.Password))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Register(registerModel);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}