using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class OtpService : IOtpService
    {
        private readonly IOtpRepository _otpRepository;
        private readonly IEmailService _emailService;
        private readonly ISMSService _smsService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<OtpService> _logger;
        private static readonly Random _random = new Random();

        public OtpService(IOtpRepository otpRepository, IEmailService emailService, IUserRepository userRepository
        , ISMSService smsService, ILogger<OtpService> logger
        )
        {
            _otpRepository = otpRepository;
            _emailService = emailService;
            _userRepository = userRepository;
            _smsService = smsService;
            _logger = logger;
        }

        public async Task<ServiceResponse<OtpResult>> GenerateOtpAsync(string receiver, string subject)
        {
            try
            {
                _logger.LogInformation("Generating OTP for receiver: {Receiver} with subject: {Subject}", receiver, subject);

                // Validate subject
                if (!Enum.TryParse(typeof(OtpSubject), subject, true, out var parsedSubject))
                {
                    return ServiceResponse<OtpResult>.ErrorResponse(
                        "Invalid subject provided. Please select a valid subject.", 422);
                }

                // Check if receiver is email or phone
                bool isEmail = receiver.Contains("@");
                bool isPhone = !isEmail;

                var companyId = 0;

                // Verify if user exists and is active for ForgetPassword
                //if (subject == "ForgetPassword")
                if ((OtpSubject)parsedSubject == OtpSubject.forget_password)
                {
                    _logger.LogInformation("Checking user existence for ForgetPassword.");

                    var user = await _userRepository.FindByEmailOrPhoneAsync(receiver);
                    if (user == null)
                    {
                        _logger.LogWarning("User not found for receiver: {Receiver}", receiver);
                        return ServiceResponse<OtpResult>.ErrorResponse("Receiver not found. Please enter a registered email or phone number.", 404);
                    }

                    if (user.status != UserStatus.active)
                    {
                        _logger.LogWarning("User is not active for receiver: {Receiver}", receiver);
                        return ServiceResponse<OtpResult>.ErrorResponse("User is not active.", 422);
                    }

                    // Fetch Company_ID for the user
                    companyId = user.company_id;
                }

                // Generate OTP
                var otpCode = GenerateRandomOtp();
                var _receiver = !string.IsNullOrEmpty(receiver) ? receiver : string.Empty;
                var generateTime = DateTime.Now;

                _logger.LogInformation("Saving OTP for receiver: {Receiver}", receiver);

                // Save OTP to the database
                var otpId = await _otpRepository.SaveOtpAsync(otpCode, _receiver, generateTime, companyId, OtpSubject.forget_password);
                if (otpId < 0)
                {
                    _logger.LogWarning("Failed to save OTP for receiver: {Receiver}", receiver);
                        return ServiceResponse<OtpResult>.ErrorResponse("Failed to save OTP to the database.", 503);
                }

                // Send OTP via email or SMS
                var otpResult = isPhone
                ? await SendOtpSmsAsync(_receiver, otpCode)
                : await SendOtpEmailAsync(_receiver, otpCode);

                if (!otpResult.Success)
                {
                    _logger.LogWarning("Failed to send OTP for receiver: {Receiver}. Error: {ErrorMessage}", receiver, otpResult.ErrorMessage);
                    return ServiceResponse<OtpResult>.ErrorResponse(otpResult.ErrorMessage, 503);
                }

                _logger.LogInformation("OTP successfully generated and sent to {Receiver}", receiver);
                // If OTP sending is successful, return the success response
                return ServiceResponse<OtpResult>.SuccessResponse(new OtpResult { Success = true, OtpId = otpId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating OTP for receiver: {Receiver}", receiver);
                return ServiceResponse<OtpResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<OtpResult>> GenerateOtpAsync(string email, string phoneNumber, string subject)
        {
            try
            {
                _logger.LogInformation("Generating OTP for email: {Email} or phone: {PhoneNumber} with subject: {Subject}", email, phoneNumber, subject);

                var companyId = 0;

                // Verify if user exists and is active for ForgetPassword
                if (subject == "ForgetPassword")
                {
                    _logger.LogInformation("Checking user existence for ForgetPassword.");

                    var user = await _userRepository.FindByEmailOrPhoneAsync(email, phoneNumber);
                    if (user == null)
                    {
                        _logger.LogWarning("User not found for email: {Email} or phone: {PhoneNumber}", email, phoneNumber);
                        return ServiceResponse<OtpResult>.ErrorResponse("User does not exist for the provided email or phone number.");
                    }

                    if (user.status != UserStatus.active)
                    {
                        _logger.LogWarning("User is not active for email: {Email} or phone: {PhoneNumber}", email, phoneNumber);
                        return ServiceResponse<OtpResult>.ErrorResponse("User is not active.");
                    }

                    // Fetch Company_ID for the user
                    companyId = user.company_id;
                }

                // Generate OTP
                var otpCode = GenerateRandomOtp();
                var receiver = !string.IsNullOrEmpty(email) ? email : phoneNumber;
                var generateTime = DateTime.Now;

                _logger.LogInformation("Saving OTP for receiver: {Receiver}", receiver);
                // Save OTP to the database
                var otpId = await _otpRepository.SaveOtpAsync(otpCode, receiver, generateTime, companyId, OtpSubject.forget_password);
                if (otpId < 0)
                {
                    _logger.LogWarning("Failed to save OTP for receiver: {Receiver}", receiver);
                    return ServiceResponse<OtpResult>.ErrorResponse("Failed to save OTP to the database");
                }

                // Send OTP via email or SMS
                var otpResult = !string.IsNullOrEmpty(email)
                   ? await SendOtpEmailAsync(receiver, otpCode)
                   : await SendOtpSmsAsync(receiver, otpCode);
                _logger.LogInformation("OTP successfully generated and sent to {Receiver}", receiver);

                return ServiceResponse<OtpResult>.SuccessResponse(new OtpResult
                {
                    Success = true,
                    OtpId = otpId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating OTP for email: {Email} or phone: {PhoneNumber}", email, phoneNumber);
                throw;
            }
        }

        private int GenerateRandomOtp()
        {
            return _random.Next(100000, 999999);
        }

        private async Task<OtpResult> SendOtpEmailAsync(string email, int otpCode)
        {
            try
            {
                _logger.LogInformation("Sending OTP email to {Email}", email);
                await _emailService.SendOtpEmailAsync(email, otpCode);
                return new OtpResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending OTP email to {Email}", email);
                return new OtpResult { Success = false, ErrorMessage = "An error occurred while sending the OTP email" };
            }
        }

        private async Task<OtpResult> SendOtpSmsAsync(string phoneNumber, int otpCode)
        {
            try
            {
                _logger.LogInformation("Sending OTP SMS to {PhoneNumber}", phoneNumber);
                await _smsService.SendOtpSMSAsync(phoneNumber, otpCode);
                return new OtpResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending OTP SMS to {PhoneNumber}", phoneNumber);
                return new OtpResult { Success = false, ErrorMessage = "An error occurred while sending the OTP sms" };
            }
        }

        public async Task<ServiceResponse<OtpResult>> GenerateOtpForSignupAsync(string receiver)
        {
            // Check if receiver is email or phone
            bool isEmail = receiver.Contains("@");
            bool isPhone = !isEmail;

            try
            {
                _logger.LogInformation("Generating OTP for signup with receiver: {Receiver}", receiver);

                // Validate that receiver (email/phone) is unique in user or company records
                bool exists = await _userRepository.CheckIfUserOrCompanyExistsAsync(receiver, isEmail);
                if (exists)
                {
                    _logger.LogWarning("Signup attempt failed. Receiver {Receiver} already exists.", receiver);
                    return ServiceResponse<OtpResult>.ErrorResponse("The provided email or phone number is already associated with an existing account.", 409);
                }
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Database timeout while checking if receiver {Receiver} exists.", receiver);
                return ServiceResponse<OtpResult>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if receiver {Receiver} exists.", receiver);
                return ServiceResponse<OtpResult>.ErrorResponse("An error occurred while checking for duplicate email or phone.", 500);
            }

            // Generate OTP
            var otpCode = GenerateRandomOtp();
            var generateTime = DateTime.Now;

            _logger.LogInformation("Saving OTP for signup for receiver: {Receiver}", receiver);
            
            // Save OTP to the database
            var otpId = await _otpRepository.SaveOtpAsync(otpCode, receiver, generateTime, null, OtpSubject.sign_up);
            if (otpId < 0)
            {
                _logger.LogWarning("Failed to save OTP for signup for receiver: {Receiver}", receiver);
                return ServiceResponse<OtpResult>.ErrorResponse("Failed to save OTP to the database", 400);
            }

            // Send OTP (email or SMS)
            var otpResult = isEmail
                ? await SendOtpEmailAsync(receiver, otpCode)
                : await SendOtpSmsAsync(receiver, otpCode);

            if (!otpResult.Success)
            {
                _logger.LogWarning("Failed to send OTP for signup to {Receiver}", receiver);
                return ServiceResponse<OtpResult>.ErrorResponse("Failed to send OTP", 400);
            }

            _logger.LogInformation("Signup OTP successfully generated and sent to {Receiver}", receiver);
            return ServiceResponse<OtpResult>.SuccessResponse(new OtpResult { Success = true, OtpId = otpId });
        }
    }
}