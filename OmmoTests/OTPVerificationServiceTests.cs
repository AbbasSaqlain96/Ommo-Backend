using FluentAssertions.Common;
using Microsoft.Extensions.Logging;
using Moq;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmmoTests
{
    public class OTPVerificationServiceTests
    {
        private readonly OtpVerificationService _otpVerificationService;
        private readonly Mock<IOtpRepository> _otpRepositoryMock = new();
        private readonly Mock<ILogger<OtpVerificationService>> _loggerMock = new();

        public OTPVerificationServiceTests()
        {
            _otpVerificationService = new OtpVerificationService(
                _otpRepositoryMock.Object,
                _loggerMock.Object
                );
        }

        [Fact]
        public async Task VerifyOtpAsync_ValidOtp_ReturnsSuccess()
        {
            // Arrange
            var otpId = 1;
            var otpCode = 123456;
            var otp = new Otp
            {
                otp_id = otpId,
                otp_code = otpCode,
                generate_time = DateTime.Now
            };

            _otpRepositoryMock.Setup(repo => repo.GetOtpByIdAsync(otpId)).ReturnsAsync(otp);

            // Act
            var result = await _otpVerificationService.VerifyOtpAsync(otpId, otpCode);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("OTP verified successfully.", result.Message);
        }

        [Fact]
        public async Task VerifyOtpAsync_OtpNotFound_Returns404()
        {
            // Arrange
            var otpId = 1;
            _otpRepositoryMock.Setup(repo => repo.GetOtpByIdAsync(otpId)).ReturnsAsync((Otp)null);

            // Act
            var result = await _otpVerificationService.VerifyOtpAsync(otpId, 123456);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal("OTP request not found. Please generate a new OTP.", result.ErrorMessage);
        }

        [Fact]
        public async Task VerifyOtpAsync_OtpMismatch_Returns400()
        {
            // Arrange
            var otpId = 1;
            var otp = new Otp
            {
                otp_id = otpId,
                otp_code = 111111,
                generate_time = DateTime.Now
            };

            _otpRepositoryMock.Setup(repo => repo.GetOtpByIdAsync(otpId)).ReturnsAsync(otp);

            // Act
            var result = await _otpVerificationService.VerifyOtpAsync(otpId, 123456);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Incorrect OTP. Please try again.", result.ErrorMessage);
        }

        [Fact]
        public async Task VerifyOtpAsync_OtpExpired_Returns400()
        {
            // Arrange
            var otpId = 1;
            var otp = new Otp
            {
                otp_id = otpId,
                otp_code = 123456,
                generate_time = DateTime.Now.AddMinutes(-2) // OTP expired
            };

            _otpRepositoryMock.Setup(repo => repo.GetOtpByIdAsync(otpId)).ReturnsAsync(otp);

            // Act
            var result = await _otpVerificationService.VerifyOtpAsync(otpId, 123456);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("OTP has expired. Please request a new one.", result.ErrorMessage);
        }
    }
}
