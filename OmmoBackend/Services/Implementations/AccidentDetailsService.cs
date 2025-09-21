using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class AccidentDetailsService : IAccidentDetailsService
    {
        private readonly IAccidentDetailsRepository _accidentDetailsRepository;
        private readonly ILogger<AccidentDetailsService> _logger;

        public AccidentDetailsService(IAccidentDetailsRepository accidentDetailsRepository, ILogger<AccidentDetailsService> logger)
        {
            _accidentDetailsRepository = accidentDetailsRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse<AccidentDetailsResponse>> GetAccidentDetailsAsync(int eventId, int companyId)
        {
            _logger.LogInformation("Fetching accident details for EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);

            try
            {
                // Validate if the event belongs to the correct company
                bool isValidEvent = await _accidentDetailsRepository.IsEventValidForCompany(eventId, companyId);
                if (!isValidEvent)
                {
                    _logger.LogWarning("Invalid event access attempt for EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);
                    return ServiceResponse<AccidentDetailsResponse>.ErrorResponse("Event does not belong to the specified company.", 401);
                }

                // Fetch accident details
                var accidentDetails = await _accidentDetailsRepository.GetAccidentDetailsAsync(eventId, companyId);
                if (accidentDetails == null)
                {
                    _logger.LogWarning("No accident details found for EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);
                    return ServiceResponse<AccidentDetailsResponse>.ErrorResponse("No accident details found for the provided event.", 400);
                }

                // Combine details and claims
                var response = new AccidentDetailsResponse
                {
                    accidentDetailDto = accidentDetails.accidentDetailDto,
                    accidentClaimDtos = accidentDetails.accidentClaimDtos
                };

                _logger.LogInformation("Successfully retrieved accident details for EventId: {EventId}", eventId);
                return ServiceResponse<AccidentDetailsResponse>.SuccessResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching accident details for EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);
                return ServiceResponse<AccidentDetailsResponse>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    }
}
