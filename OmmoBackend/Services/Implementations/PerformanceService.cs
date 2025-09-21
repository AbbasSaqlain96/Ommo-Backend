using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class PerformanceService : IPerformanceService
    {
        private readonly IPerformanceRepository _performanceRepository;
        private readonly ILogger<PerformanceService> _logger;

        public PerformanceService(IPerformanceRepository performanceRepository, ILogger<PerformanceService> logger)
        {
            _performanceRepository = performanceRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse<PerformanceDto>> GetPerformanceAsync(int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching performance data for company ID {CompanyId}", companyId);
                var performance = await _performanceRepository.GetPerformanceAsync(companyId);

                if (performance == null)
                {
                    _logger.LogWarning("No performance data found for company ID {CompanyId}", companyId);
                    return ServiceResponse<PerformanceDto>.SuccessResponse(new PerformanceDto(), "No performance data found. Returning default performance.");
                }

                _logger.LogInformation("Successfully fetched performance data for company ID {CompanyId}", companyId);
                return ServiceResponse<PerformanceDto>.SuccessResponse(performance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching performance data for company ID {CompanyId}", companyId);
                return ServiceResponse<PerformanceDto>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    }
}
