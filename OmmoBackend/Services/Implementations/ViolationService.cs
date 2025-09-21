using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class ViolationService : IViolationService
    {
        private readonly IViolationRepository _violationRepository;
        private readonly ILogger<ViolationService> _logger;
        public ViolationService(IViolationRepository violationRepository, ILogger<ViolationService> logger)
        {
            _violationRepository = violationRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse<List<ViolationDto>>> GetViolationsAsync()
        {
            _logger.LogInformation("Fetching all violations.");

            try
            {
                var violations = await _violationRepository.GetAllAsync();

                List<ViolationDto> list = violations.Select(violation => new ViolationDto
                {
                    ViolationId = violation.violation_id,
                    ViolationType = violation.violation_type,
                    Description = violation.description
                }).ToList();

                _logger.LogInformation("Successfully retrieved {Count} violations.", list.Count);
                return ServiceResponse<List<ViolationDto>>.SuccessResponse(list, "Violations fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving violations.");
                return ServiceResponse<List<ViolationDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    }
}
