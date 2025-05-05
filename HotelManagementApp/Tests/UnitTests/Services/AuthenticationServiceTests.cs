// HotelManagementApp.Tests/UnitTests/Services/AuthenticationServiceTests.cs
using HotelManagementApp.Controllers;
using HotelManagementApp.Models;
using HotelManagementApp.Models.DTOs.Service;
using HotelManagementApp.Services.Authentication;
using HotelManagementApp.Services.ServiceService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace HotelManagementApp.Tests.UnitTests.Services
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
        private readonly AuthenticationService _authService;

        public AuthenticationServiceTests()
        {
            // Setup UserManager mock
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // Setup SignInManager mock
            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            var userPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object,
                contextAccessorMock.Object,
                userPrincipalFactoryMock.Object,
                null, null, null, null);

            // Setup RoleManager mock
            var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
            _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
                roleStoreMock.Object, null, null, null, null);

            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("YourSecretKeyForTestingLongEnoughToBeSecure");
            _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockLogger = new Mock<ILogger<AuthenticationService>>();

            // Create service instance with mocks
            _authService = new AuthenticationService(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockRoleManager.Object,
                _mockConfiguration.Object,
                _mockHttpContextAccessor.Object);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsUser()
        {
            // Arrange
            var username = "testuser";
            var password = "password";
            var user = new ApplicationUser { UserName = username, Id = 1 };

            _mockSignInManager
                .Setup(x => x.PasswordSignInAsync(username, password, false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            _mockUserManager
                .Setup(x => x.FindByNameAsync(username))
                .ReturnsAsync(user);

            _mockUserManager
                .Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.LoginAsync(username, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(username, result.UserName);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task LoginAsync_InvalidCredentials_ReturnsNull()
        {
            // Arrange
            var username = "testuser";
            var password = "wrongpassword";

            _mockSignInManager
                .Setup(x => x.PasswordSignInAsync(username, password, false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _authService.LoginAsync(username, password);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RegisterUserAsync_ValidUser_ReturnsTrue()
        {
            // Arrange
            var user = new ApplicationUser
            {
                UserName = "newuser",
                Email = "newuser@example.com",
                FirstName = "New",
                LastName = "User"
            };
            var password = "Password123!";

            _mockUserManager
                .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password))
                .ReturnsAsync(IdentityResult.Success);

            _mockRoleManager
                .Setup(x => x.RoleExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            _mockUserManager
                .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.RegisterUserAsync(user, password);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GenerateJwtTokenAsync_ValidUser_ReturnsToken()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = 1,
                UserName = "testuser",
                Email = "testuser@example.com"
            };

            _mockUserManager
                .Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Guest" });

            // Act
            var token = await _authService.GenerateJwtTokenAsync(user);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }
    }
}






