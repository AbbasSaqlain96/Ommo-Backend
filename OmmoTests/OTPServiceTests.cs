using FluentValidation;
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
using System.Text;
using System.Threading.Tasks;

namespace OmmoTests
{
    public class OTPServiceTests
    {
        private readonly OtpService _otpService;
        private readonly Mock<IOtpRepository> _otpRepositoryMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<ISMSService> _smsServiceMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<ILogger<OtpService>> _loggerMock = new();

        public OTPServiceTests()
        {
            _otpService = new OtpService(
                _otpRepositoryMock.Object,
                _emailServiceMock.Object,
                _userRepositoryMock.Object,
                _smsServiceMock.Object,
                _loggerMock.Object
                );
        }

        [Fact]
        public async Task GenerateOtpAsync_ShouldReturnError_WhenUserNotFound()
        {
            // Arrange
            string receiver = "test@example.com";
            string subject = OtpSubject.forget_password.ToString();

            _userRepositoryMock
                .Setup(x => x.FindByEmailOrPhoneAsync(receiver))
                .ReturnsAsync((User)null);  // User not found

            // Act
            var result = await _otpService.GenerateOtpAsync(receiver, subject);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Receiver not found. Please enter a registered email or phone number.", result.ErrorMessage);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GenerateOtpAsync_ShouldReturnError_WhenUserIsInactive()
        {
            // Arrange
            string receiver = "test@example.com";
            string subject = OtpSubject.forget_password.ToString();
            var user = new User { status = UserStatus.in_active }; // Inactive user

            _userRepositoryMock
                .Setup(x => x.FindByEmailOrPhoneAsync(receiver))
                .ReturnsAsync(user);

            // Act
            var result = await _otpService.GenerateOtpAsync(receiver, subject);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User is not active.", result.ErrorMessage);
        }

        [Fact]
        public async Task GenerateOtpAsync_ShouldGenerateOtpAndSendEmail_WhenValidUser()
        {
            // Arrange
            string receiver = "test@example.com";
            string subject = OtpSubject.forget_password.ToString();
            var user = new User { status = UserStatus.active, company_id = 1 };

            _userRepositoryMock
                .Setup(x => x.FindByEmailOrPhoneAsync(receiver))
                .ReturnsAsync(user);

            _otpRepositoryMock
                .Setup(x => x.SaveOtpAsync(It.IsAny<int>(), receiver, It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<OtpSubject>()))
                .ReturnsAsync(1); // Simulate successful OTP save

            _emailServiceMock
                .Setup(x => x.SendOtpEmailAsync(receiver, It.IsAny<int>()))
                .Returns(Task.CompletedTask); // Simulate successful email send

            // Act
            var result = await _otpService.GenerateOtpAsync(receiver, subject);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.Data.OtpId);
        }

        [Fact]
        public async Task GenerateOtpAsync_ShouldReturnError_WhenSubjectIsInvalid()
        {
            // Arrange
            string receiver = "test@example.com";
            string subject = "InvalidSubject";

            // Act
            var result = await _otpService.GenerateOtpAsync(receiver, subject);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid subject provided. Please select a valid subject.", result.ErrorMessage);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task GenerateOtpForSignupAsync_ShouldReturnError_WhenReceiverExists()
        {
            // Arrange
            string receiver = "test@example.com";
            bool isEmail = true;

            _userRepositoryMock
                .Setup(x => x.CheckIfUserOrCompanyExistsAsync(receiver, isEmail))
                .ReturnsAsync(true); // Receiver already exists

            // Act
            var result = await _otpService.GenerateOtpForSignupAsync(receiver);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("The provided email or phone number is already associated with an existing account.", result.ErrorMessage);
        }

        [Fact]
        public async Task GenerateOtpForSignupAsync_ShouldGenerateOtpAndSendSms_WhenValidReceiver()
        {
            // Arrange
            string receiver = "1234567890"; // Phone number
            bool isEmail = false;

            _userRepositoryMock
                .Setup(x => x.CheckIfUserOrCompanyExistsAsync(receiver, isEmail))
                .ReturnsAsync(false); // Receiver does not exist

            _otpRepositoryMock
                .Setup(x => x.SaveOtpAsync(It.IsAny<int>(), receiver, It.IsAny<DateTime>(), It.IsAny<int?>(), It.IsAny<OtpSubject>()))
                .ReturnsAsync(1); // Simulate successful OTP save

            _smsServiceMock
                .Setup(x => x.SendOtpSMSAsync(receiver, It.IsAny<int>()))
                .Returns(Task.CompletedTask); // Simulate successful SMS send

            // Act
            var result = await _otpService.GenerateOtpForSignupAsync(receiver);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, result.Data.OtpId);
        }
    }
}
