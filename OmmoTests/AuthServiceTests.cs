using Microsoft.Extensions.Logging;
using Moq;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Implementations;
using OmmoBackend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OmmoTests
{
    public class AuthServiceTests
    {
        private readonly AuthService _authService;
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
        private readonly Mock<ILogger<AuthService>> _loggerMock = new();

        public AuthServiceTests()
        {
            _authService = new AuthService(
                _userRepositoryMock.Object,
                _jwtTokenGeneratorMock.Object,
                _refreshTokenRepositoryMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task AuthenticateAsync_UserNotFound_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginRequest = new LoginRequest { EmailOrPhone = "test@example.com", Password = "password123" };
            _userRepositoryMock.Setup(repo => repo.FindByEmailOrPhoneAsync(It.IsAny<string>())).ReturnsAsync((User)null);

            // Act
            var result = await _authService.AuthenticateAsync(loginRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task AuthenticateAsync_UserNotActive_ShouldReturnForbidden()
        {
            var user = new User { status = UserStatus.in_active };
            _userRepositoryMock.Setup(repo => repo.FindByEmailOrPhoneAsync(It.IsAny<string>())).ReturnsAsync(user);

            var loginRequest = new LoginRequest { EmailOrPhone = "test@example.com", Password = "password123" };
            var result = await _authService.AuthenticateAsync(loginRequest);

            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task AuthenticateAsync_InvalidPassword_ShouldReturnUnauthorized()
        {
            var user = new User { status = UserStatus.active, password_hash = Encoding.UTF8.GetBytes("hashed_password"), password_salt = Encoding.UTF8.GetBytes("salt") };
            _userRepositoryMock.Setup(repo => repo.FindByEmailOrPhoneAsync(It.IsAny<string>())).ReturnsAsync(user);

            var loginRequest = new LoginRequest { EmailOrPhone = "test@example.com", Password = "wrongpassword" };
            var result = await _authService.AuthenticateAsync(loginRequest);

            Assert.False(result.Success);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task AuthenticateAsync_ValidUser_ShouldReturnToken()
        {
            var loginPassword = "password123";
            var user = new User { status = UserStatus.active, user_id = 1 };
            // Simulate password hashing logic
            using (var hmac = new HMACSHA512())
            {
                user.password_salt = hmac.Key;
                user.password_hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginPassword));
            }

            _userRepositoryMock.Setup(repo => repo.FindByEmailOrPhoneAsync(It.IsAny<string>())).ReturnsAsync(user);
            _jwtTokenGeneratorMock.Setup(tokenGen => tokenGen.GenerateToken(user)).Returns("generated_token");
            _refreshTokenRepositoryMock.Setup(repo => repo.SaveRefreshTokenAsync(It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var loginRequest = new LoginRequest { EmailOrPhone = "test@example.com", Password = "password123" };
            var result = await _authService.AuthenticateAsync(loginRequest);

            Assert.True(result.Success);
            Assert.NotNull(result.Data.Token);
        }

        [Fact]
        public async Task AuthenticateAsync_ExceptionThrown_ShouldReturnServerError()
        {
            _userRepositoryMock.Setup(repo => repo.FindByEmailOrPhoneAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Database error"));

            var loginRequest = new LoginRequest { EmailOrPhone = "test@example.com", Password = "password123" };
            var result = await _authService.AuthenticateAsync(loginRequest);

            Assert.False(result.Success);
            Assert.Equal(503, result.StatusCode);
        }
    }
}
