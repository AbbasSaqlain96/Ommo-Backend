using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Services.Interfaces;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace OmmoBackend.Services.Implementations
{
    public class SMSService : ISMSService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SMSService> _logger;

        public SMSService(IConfiguration configuration, ILogger<SMSService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        public async Task SendOtpSMSAsync(string phoneNumber, int otpCode)
        {
            try
            {
                _logger.LogInformation("Initializing Twilio client for sending OTP SMS to {PhoneNumber}", phoneNumber);

                // Twilio initialization with your credentials from appsettings.json
                TwilioClient.Init(_configuration["Twilio:AccountSid"], _configuration["Twilio:AuthToken"]);

                var message = await MessageResource.CreateAsync(
                    body: $"Your OTP code for Ommo is {otpCode}",
                    from: new PhoneNumber(_configuration["Twilio:FromPhoneNumber"]),
                    to: new PhoneNumber(phoneNumber)
                );

                _logger.LogInformation("OTP SMS sent successfully to {PhoneNumber}, Message SID: {MessageSid}", phoneNumber, message.Sid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP SMS to {PhoneNumber}", phoneNumber);
                throw;
            }
        }
    }
}