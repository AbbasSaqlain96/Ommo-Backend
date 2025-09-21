using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OmmoBackend.Controllers;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmmoTests
{
    public class AuthControllerTests
    {
        private readonly AuthController _authController;
        private readonly Mock<IAuthService> _authServiceMock = new();
        private readonly Mock<ILogger<AuthController>> _loggerMock = new();

        public AuthControllerTests()
        {
            _authController = new AuthController(_authServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Login_InvalidModel_ShouldReturnBadRequest()
        {
            // Arrange
            _authController.ModelState.AddModelError("EmailOrPhone", "Required");

            // Act
            var result = await _authController.Login(new LoginRequest());

            // Assert: Verify that the result is exactly BadRequestObjectResult
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

            // Assert: Verify that the status code is 400
            Assert.Equal(400, badRequestResult.StatusCode);

            // Extract the anonymous object properly
            var responseContent = badRequestResult.Value?.GetType().GetProperty("message")?.GetValue(badRequestResult.Value, null);
            
            // Assert: Verify response content
            Assert.NotNull(responseContent);
            Assert.Equal("Invalid request format.", responseContent);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ShouldReturnUnauthorized()
        {
            var loginRequest = new LoginRequest { EmailOrPhone = "test@example.com", Password = "wrongpassword" };
            _authServiceMock.Setup(service => service.AuthenticateAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(ServiceResponse<AuthResult>.ErrorResponse("Invalid credentials", 401));

            var result = await _authController.Login(loginRequest);

            var unauthorizedResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
        }

        [Fact]
        public async Task Login_InactiveUser_ShouldReturnForbidden()
        {
            var loginRequest = new LoginRequest { EmailOrPhone = "test@example.com", Password = "password123" };
            _authServiceMock.Setup(service => service.AuthenticateAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(ServiceResponse<AuthResult>.ErrorResponse("User is not active", 403));

            var result = await _authController.Login(loginRequest);

            var forbiddenResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, forbiddenResult.StatusCode);
        }

        [Fact]
        public async Task Login_Successful_ShouldReturnToken()
        {
            var loginRequest = new LoginRequest { EmailOrPhone = "test@example.com", Password = "password123" };
            var authResult = new AuthResult { Token = "valid_token", RefreshToken = "refresh_token" };
            _authServiceMock.Setup(service => service.AuthenticateAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(ServiceResponse<AuthResult>.SuccessResponse(authResult));

            var result = await _authController.Login(loginRequest);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task Login_Exception_ShouldReturnServerError()
        {
            var loginRequest = new LoginRequest { EmailOrPhone = "test@example.com", Password = "password123" };
            _authServiceMock.Setup(service => service.AuthenticateAsync(It.IsAny<LoginRequest>()))
                .ThrowsAsync(new System.Exception("Server error"));

            var result = await _authController.Login(loginRequest);

            var serverErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, serverErrorResult.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_ValidToken_ReturnsSuccess()
        {
            // Arrange
            var request = new RefreshTokenRequest { RefreshToken = "valid_refresh_token" };
            var authResult = new AuthResult { Token = "new_access_token", RefreshToken = "new_refresh_token" };
            var response = ServiceResponse<AuthResult>.SuccessResponse(authResult);

            _authServiceMock.Setup(s => s.RefreshTokenAsync(request.RefreshToken)).ReturnsAsync(response);

            // Act
            var result = await _authController.RefreshToken(request);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var responseObject = actionResult.Value;
            Assert.NotNull(responseObject);

            // Extract "data" from the anonymous type
            var dataProperty = responseObject.GetType().GetProperty("data");
            Assert.NotNull(dataProperty);

            var dataValue = dataProperty.GetValue(responseObject);
            Assert.NotNull(dataValue);

            // Convert data to expected AuthResult type
            var authData = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthResult>(
                Newtonsoft.Json.JsonConvert.SerializeObject(dataValue));

            Assert.NotNull(authData);
            Assert.Equal("new_access_token", authData.Token);
            Assert.Equal("new_refresh_token", authData.RefreshToken);
        }

        [Fact]
        public async Task RefreshToken_InvalidToken_ReturnsError()
        {
            // Arrange
            var request = new RefreshTokenRequest { RefreshToken = "invalid_refresh_token" };
            var response = new ServiceResponse<AuthResult>
            {
                Success = false,
                ErrorMessage = "Invalid token",
                StatusCode = 401
            };

            _authServiceMock.Setup(s => s.RefreshTokenAsync(request.RefreshToken)).ReturnsAsync(response);

            // Act
            var result = await _authController.RefreshToken(request);

            // Assert
            var actionResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(401, actionResult.StatusCode);
        }

        [Fact]
        public async Task Logout_ValidToken_ReturnsSuccess()
        {
            // Arrange
            var request = new LogoutRequest { RefreshToken = "valid_refresh_token" };
            var response = new ServiceResponse<bool> { Success = true };

            _authServiceMock.Setup(s => s.RevokeRefreshTokenAsync(request.RefreshToken)).ReturnsAsync(response);

            // Act
            var result = await _authController.Logout(request);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result);
        }
    }
}
