using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/otp")]
    public class OtpController : ControllerBase
    {
        private readonly IOtpService _otpService;
        private readonly IOtpVerificationService _otpVerificationService;
        private readonly IValidator<VerifyOtpRequest> _verifyOtpRequestValidator;
        private readonly ILogger<OtpController> _logger;

        public OtpController(
            IOtpService otpService,
            IOtpVerificationService otpVerificationService,
            IValidator<VerifyOtpRequest> verifyOtpRequestValidator,
            ILogger<OtpController> logger)
        {
            _otpService = otpService;
            _otpVerificationService = otpVerificationService;
            _verifyOtpRequestValidator = verifyOtpRequestValidator;
            _logger = logger;
        }

        [HttpPost("generate")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateOtp([FromBody] OtpRequest request)
        {
            _logger.LogInformation("OTP generation request received for receiver: {Receiver}", request.receiver);

            if (string.IsNullOrWhiteSpace(request.receiver) && string.IsNullOrWhiteSpace(request.Subject))
                return ApiResponse.Error("Receiver and Subject are required.", 400);

            if (string.IsNullOrWhiteSpace(request.receiver))
                return ApiResponse.Error("Receiver is required.", 400);

            if (string.IsNullOrWhiteSpace(request.Subject))
                return ApiResponse.Error("Subject is required.", 400);

            // Validate receiver format
            //if (!IsValidEmail(request.receiver) && !IsValidPhoneNumber(request.receiver))
            if (!ValidationHelper.IsValidEmail(request.receiver) && !ValidationHelper.IsValidPhoneNumber(request.receiver))
                return ApiResponse.Error("Receiver must be a valid email address or phone number.", 400);

            try
            {
                // Generate the OTP and store it in the OTP table
                var otpResult = await _otpService.GenerateOtpAsync(request.receiver, request.Subject!);

                if (!otpResult.Success)
                {
                    _logger.LogError("OTP generation failed: {ErrorMessage}", otpResult.ErrorMessage);
                    var statusCode = otpResult.StatusCode != 0 ? otpResult.StatusCode : 503;
                    return ApiResponse.Error(otpResult.ErrorMessage, statusCode);
                }

                _logger.LogInformation("OTP generated successfully for receiver: {Receiver}", request.receiver);
                //return Ok(new { otp_id = otpResult.Data.OtpId });
                return ApiResponse.Success(new { otp_id = otpResult.Data.OtpId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while generating the OTP.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost("verify")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            _logger.LogInformation("OTP verification request received for OtpId: {OtpId}", request.OtpId);

            // Validate the input
            var validationResult = await _verifyOtpRequestValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("OTP verification failed due to validation errors: {Errors}",
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return ApiResponse.Error(validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid request.", 400);
            }

            try
            {
                var result = await _otpVerificationService.VerifyOtpAsync(request.OtpId, request.OtpNumber);

                if (!result.Success)
                {
                    _logger.LogWarning("OTP verification failed for OtpId: {OtpId}. Reason: {ErrorMessage}", request.OtpId, result.ErrorMessage);

                    // Return appropriate status codes based on response
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                }

                _logger.LogInformation("OTP verified successfully for OtpId: {OtpId}");
                return ApiResponse.Success(result.Data, "OTP verified successfully.");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during OTP verification for OtpId: {OtpId}", request.OtpId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost("generate-otp-signup")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateOTPSignup(string receiver)
        {
            _logger.LogInformation("OTP signup request received for receiver: {Receiver}", receiver);

            try
            {
                if (string.IsNullOrWhiteSpace(receiver))
                {
                    _logger.LogWarning("OTP signup request failed: receiver is empty.");
                    return ApiResponse.Error("Receiver cannot be empty.", 400);
                }

                // Call service to generate OTP for signup
                var response = await _otpService.GenerateOtpForSignupAsync(receiver);

                if (!response.Success)
                {
                    _logger.LogWarning("OTP generation failed for receiver: {Receiver}. Reason: {ErrorMessage}",
                        receiver, response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("OTP generated successfully for receiver: {Receiver}, OtpId: {OtpId}",
                    receiver, response.Data.OtpId);
                return ApiResponse.Success(new { otp_id = response.Data.OtpId }, "OTP generated successfully.");
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Server timeout occurred during OTP generation for receiver: {Receiver}", receiver);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during OTP generation for receiver: {Receiver}", receiver);
                return ApiResponse.Error("Unexpected error occurred. Please try again later.", 503);
            }
        }
    }
}