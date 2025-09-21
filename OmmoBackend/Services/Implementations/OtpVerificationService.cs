using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class OtpVerificationService : IOtpVerificationService
    {
        private readonly IOtpRepository _otpRepository;
        private readonly TimeSpan _otpValidityDuration = TimeSpan.FromMinutes(1);
        private readonly ILogger<OtpVerificationService> _logger;

        public OtpVerificationService(IOtpRepository otpRepository, ILogger<OtpVerificationService> logger)
        {
            _otpRepository = otpRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse<string>> VerifyOtpAsync(int otpId, int otpNumber)
        {
            _logger.LogInformation("Verifying OTP. OTP ID: {OtpId}", otpId);

            var otp = await _otpRepository.GetOtpByIdAsync(otpId);
            if (otp == null)
            {
                _logger.LogWarning("OTP not found. OTP ID: {OtpId}", otpId);
                return ServiceResponse<string>.ErrorResponse("OTP request not found. Please generate a new OTP.", 404);
            }

            // Check if the entered OTP matches the one stored in the database
            if (otp.otp_code != otpNumber)
            {
                _logger.LogWarning("Invalid OTP entered. OTP ID: {OtpId}", otpId);
                return ServiceResponse<string>.ErrorResponse("Incorrect OTP. Please try again.", 400);
            }

            // Check if the OTP has expired (valid for 1 minute)
            if (otp.generate_time.Add(_otpValidityDuration) < DateTime.Now)
            {
                _logger.LogWarning("OTP expired. OTP ID: {OtpId}", otpId);
                return ServiceResponse<string>.ErrorResponse("OTP has expired. Please request a new one.", 400);
            }

            _logger.LogInformation("OTP verified successfully. OTP ID: {OtpId}", otpId);
            return ServiceResponse<string>.SuccessResponse(null, "OTP verified successfully.");
        }
    }
}