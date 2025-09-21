using FluentValidation;
using FluentValidation.Results;
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
    public class OTPControllerTests
    {
        private readonly OtpController _otpController;
        private readonly Mock<IOtpService> _otpServiceMock = new();
        private readonly Mock<IOtpVerificationService> _otpVerificationServiceMock = new();
        private readonly Mock<IValidator<VerifyOtpRequest>> _validatorMock = new();
        private readonly Mock<ILogger<OtpController>> _loggerMock = new();

        public OTPControllerTests()
        {
            _otpController = new OtpController(
                        _otpServiceMock.Object,
                        _otpVerificationServiceMock.Object,
                        _validatorMock.Object,
                        _loggerMock.Object
                );
        }

        [Fact]
        public async Task GenerateOtp_ReturnsBadRequest_WhenReceiverIsEmpty()
        {
            var request = new OtpRequest { receiver = "" };
            var result = await _otpController.GenerateOtp(request);

            var badRequestResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task GenerateOtp_ReturnsSuccess_WhenOtpIsGenerated()
        {
            // Arrange
            var request = new OtpRequest
            {
                receiver = "test@example.com",
                Subject = "Login Verification"
            };

            var otpResult = new ServiceResponse<OtpResult>
            {
                Success = true,
                Data = new OtpResult { OtpId = 12345 }
            };

            _otpServiceMock.Setup(s => s.GenerateOtpAsync(request.receiver, request.Subject))
                .ReturnsAsync(otpResult);

            // Act
            var result = await _otpController.GenerateOtp(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GenerateOtp_ReturnsError_WhenServiceFails()
        {
            // Arrange
            var request = new OtpRequest
            {
                receiver = "test@example.com",
                Subject = "Login Verification"
            };

            var otpResult = new ServiceResponse<OtpResult>
            {
                Success = false,
                ErrorMessage = "Error generating OTP",
                StatusCode = 500
            };

            _otpServiceMock.Setup(s => s.GenerateOtpAsync(request.receiver, request.Subject))
                .ReturnsAsync(otpResult);

            // Act
            var result = await _otpController.GenerateOtp(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task VerifyOtp_ReturnsBadRequest_WhenValidationFails()
        {
            var request = new VerifyOtpRequest { OtpId = 0, OtpNumber = 123456 };
            var validationResult = new ValidationResult(new List<ValidationFailure>
        {
            new ValidationFailure("OtpId", "OtpId is required.")
        });

            _validatorMock.Setup(v => v.ValidateAsync(request, default))
                .ReturnsAsync(validationResult);

            var result = await _otpController.VerifyOtp(request);
            var badRequestResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task VerifyOtp_ReturnsSuccess_WhenOtpIsValid()
        {
            var request = new VerifyOtpRequest { OtpId = 1, OtpNumber = 123456 };
            var validationResult = new ValidationResult();
            var verificationResult = new ServiceResponse<string>
            {
                Success = true,
                Data = "Success"
            };

            // Mock the validator
            _validatorMock.Setup(v => v.ValidateAsync(request, default))
                .ReturnsAsync(validationResult);

            // Mock the verification service
            _otpVerificationServiceMock.Setup(s => s.VerifyOtpAsync(request.OtpId, request.OtpNumber))
                .ReturnsAsync(verificationResult);

            var result = await _otpController.VerifyOtp(request);
            var okResult = Assert.IsType<OkObjectResult>(result); // Expecting OkObjectResult here
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GenerateOtp_ReturnsBadRequest_WhenReceiverIsMissing()
        {
            var request = new OtpRequest
            {
                receiver = null,
                Subject = "Test Subject"
            };

            var result = await _otpController.GenerateOtp(request);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);
        }

        [Fact]
        public async Task GenerateOtp_ReturnsBadRequest_WhenSubjectIsMissing()
        {
            var request = new OtpRequest
            {
                receiver = "test@example.com",
                Subject = null
            };

            var result = await _otpController.GenerateOtp(request);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);
        }

        [Fact]
        public async Task GenerateOtp_ReturnsBadRequest_WhenReceiverIsInvalid()
        {
            var request = new OtpRequest
            {
                receiver = "invalid_receiver",
                Subject = "Login"
            };

            var result = await _otpController.GenerateOtp(request);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);
        }

    }
}
