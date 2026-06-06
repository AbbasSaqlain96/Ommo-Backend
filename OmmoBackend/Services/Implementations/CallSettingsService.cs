using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OmmoBackend.Services.Implementations
{
    public class CallSettingsService : ICallSettingsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CallSettingsService> _logger;
        private readonly AppDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICompanyRepository _companyRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        public CallSettingsService(HttpClient httpClient, IConfiguration configuration, ILogger<CallSettingsService> logger, AppDbContext dbContext, IUnitOfWork unitOfWork, ICompanyRepository companyRepository, IOnboardingRepository onboardingRepository)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _companyRepository = companyRepository;
            _onboardingRepository = onboardingRepository;
        }

        public async Task<ServiceResponse<object>> GetAvailableNumbersAsync()
        {
            try
            {
                _logger.LogInformation("Fetching available phone numbers from Twilio...");

                var accountSid = _configuration["Twilio:AccountSid"];
                var authToken = _configuration["Twilio:AuthToken"];

                if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
                    return ServiceResponse<object>.ErrorResponse("Twilio configuration missing.", 500);

                var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/AvailablePhoneNumbers/US/Local.json";

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                // Basic Auth
                var authBytes = Encoding.ASCII.GetBytes($"{accountSid}:{authToken}");
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(authBytes)
                );

                _logger.LogInformation("Sending request to Twilio API: {Url}", url);

                var response = await _httpClient.SendAsync(request);

                _logger.LogInformation("Received response from Twilio API. Status: {Status}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();

                    _logger.LogError("Twilio API failed. Status: {Status}, Response: {Response}",
                        response.StatusCode, error);

                    return ServiceResponse<object>.ErrorResponse("Failed to fetch available numbers from Twilio.", 502);
                }

                var content = await response.Content.ReadAsStringAsync();

                var twilioResponse = JsonSerializer.Deserialize<TwilioAvailableNumbersResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (twilioResponse?.available_phone_numbers == null)
                    return ServiceResponse<object>.ErrorResponse("Invalid response from Twilio.", 500);

                var filtered = twilioResponse.available_phone_numbers
                    .Where(x =>
                        x.capabilities != null &&
                        x.capabilities.voice == true &&
                        string.Equals(x.address_requirements, "none", StringComparison.OrdinalIgnoreCase) &&
                        x.beta == false
                    )
                    .Select(x => new
                    {
                        phone_number = x.phone_number
                    })
                    .ToList();

                _logger.LogInformation("Filtered available numbers. Count: {Count}", filtered.Count);

                return ServiceResponse<object>.SuccessResponse(filtered,
                    "Available numbers fetched successfully."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Twilio available numbers");
                return ServiceResponse<object>.ErrorResponse("Server error while fetching numbers.", 503);
            }
        }

        /*  public async Task<ServiceResponse<object>> BuyNumberAsync(int companyId, BuyNumberRequest request)
          {
              try
              {
                  // AC-7: Validate phone number
                  if (string.IsNullOrWhiteSpace(request.PhoneNumber) || !request.PhoneNumber.StartsWith("+1"))
                      return ServiceResponse<object>.ErrorResponse(
                          "Invalid phone number. Must be a US number starting with +1.", 400);

                  var accountSid = _configuration["Twilio:AccountSid"];
                  var authToken = _configuration["Twilio:AuthToken"];

                  if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
                      return ServiceResponse<object>.ErrorResponse("Twilio configuration missing.", 500);

                  var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/IncomingPhoneNumbers.json";

                  // Build request
                  var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);

                  var authBytes = Encoding.ASCII.GetBytes($"{accountSid}:{authToken}");
                  httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
                      "Basic",
                      Convert.ToBase64String(authBytes)
                  );

                  // x-www-form-urlencoded body
                  httpRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                  {
                      { "PhoneNumber", request.PhoneNumber }
                  });

                  // Call Twilio FIRST
                  var response = await _httpClient.SendAsync(httpRequest);

                  var responseContent = await response.Content.ReadAsStringAsync();

                  if (!response.IsSuccessStatusCode)
                  {
                      _logger.LogError("Twilio purchase failed. Status: {Status}, Raw: {Response}",
                          response.StatusCode, responseContent);

                      string cleanMessage = "Failed to purchase number.";

                      try
                      {
                          var errorObj = JsonSerializer.Deserialize<TwilioErrorResponse>(
                              responseContent,
                              new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                          if (!string.IsNullOrWhiteSpace(errorObj?.message))
                          {
                              cleanMessage = $"Failed to purchase number. {errorObj.message}";
                          }
                      }
                      catch
                      {
                          // fallback if parsing fails
                          cleanMessage = "Failed to purchase number due to an unexpected error.";
                      }

                      return ServiceResponse<object>.ErrorResponse(cleanMessage, 502);
                  }

                  // Twilio success → now DB transaction
                  var strategy = _dbContext.Database.CreateExecutionStrategy();

                  return await strategy.ExecuteAsync(async () =>
                  {
                      await using var transaction = await _unitOfWork.BeginTransactionAsync();

                      try
                      {
                          // STEP 1: Update company phone number
                          await _companyRepository.UpdatePhoneNumberAsync(companyId, request.PhoneNumber);

                          // STEP 2: Update onboarding (guarded)
                          await _onboardingRepository.MarkCallSettingsCompletedAsync(companyId);

                          await transaction.CommitAsync();

                          return ServiceResponse<object>.SuccessResponse(
                              new { phone_number = request.PhoneNumber },
                              "Phone number purchased successfully."
                          );
                      }
                      catch (Exception ex)
                      {
                          await transaction.RollbackAsync();

                          _logger.LogError(ex,
                              "DB failure after Twilio success for CompanyId {CompanyId}",
                              companyId);

                          return ServiceResponse<object>.ErrorResponse(
                              "Number purchased but failed to save. Contact support.", 500);
                      }
                  });
              }
              catch (Exception ex)
              {
                  _logger.LogError(ex, "Unhandled error in BuyNumberAsync");
                  return ServiceResponse<object>.ErrorResponse("Server error.", 503);
              }
          }*/
        public async Task<ServiceResponse<object>> BuyNumberAsync(int companyId, BuyNumberRequest request)
        {
            try
            {
                var onboarding = await _dbContext.company_onboarding
                    .FirstOrDefaultAsync(o => o.company_id == companyId);

                if (onboarding == null)
                    return ServiceResponse<object>.ErrorResponse("Onboarding record not found.", 404);

                onboarding.call_settings_completed_at = DateTime.UtcNow;
                onboarding.updated_at = DateTime.UtcNow;
                onboarding.is_completed = true;
                onboarding.current_step = OnboardingStep.completed;


                await _dbContext.SaveChangesAsync();

                return ServiceResponse<object>.SuccessResponse(null, "Call settings completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BuyNumberAsync for CompanyId {CompanyId}", companyId);
                return ServiceResponse<object>.ErrorResponse("Server error.", 500);
            }
        }
    }
}
