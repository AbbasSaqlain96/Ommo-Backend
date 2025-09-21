using OmmoBackend.Services.Interfaces;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Twilio;
using Twilio.Rest.Api.V2010.Account.AvailablePhoneNumberCountry;

namespace OmmoBackend.Services.Implementations
{
    public class TwilioService : ITwilioService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TwilioService> _logger;

        public TwilioService(IConfiguration config, ILogger<TwilioService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<string> BuyNumberAsync()
        {
            var accountSid = _config["Twilio:AccountSid"];
            var authToken = _config["Twilio:AuthToken"];
            var webhookUrl = _config["Twilio:WebhookUrl"];

            TwilioClient.Init(accountSid, authToken);

            try
            {
                // ✅ Step 1: Search for available local numbers in the US
                var availableNumbers = await LocalResource.ReadAsync(
                    pathCountryCode: "US",
                    limit: 1,
                    smsEnabled: true
                // You can filter by areaCode, contains, voiceEnabled, etc.
                );

                var number = availableNumbers.FirstOrDefault();
                if (number == null)
                {
                    _logger.LogError("No available Twilio phone numbers found.");
                    return null;
                }

                // ✅ Step 2: Purchase the number
                var purchasedNumber = await IncomingPhoneNumberResource.CreateAsync(
                    phoneNumber: new PhoneNumber(number.PhoneNumber.ToString()),
                    smsUrl: !string.IsNullOrEmpty(webhookUrl) ? new Uri(webhookUrl) : null,
                    voiceUrl: !string.IsNullOrEmpty(webhookUrl) ? new Uri(webhookUrl) : null
                );

                _logger.LogInformation("Successfully purchased Twilio number: {Number}", purchasedNumber.PhoneNumber.ToString());

                return purchasedNumber.PhoneNumber.ToString(); // e.g. +14155552671
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Twilio API error during number purchase.");
                return null;
            }
        }
    }
}
