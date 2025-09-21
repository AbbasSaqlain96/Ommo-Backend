using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class DriverPerformanceService : IDriverPerformanceService
    {
        private readonly IDriverPerformanceRepository _driverPerformanceRepository;
        private readonly ILogger<DriverPerformanceService> _logger;
        public DriverPerformanceService(IDriverPerformanceRepository driverPerformanceRepository, ILogger<DriverPerformanceService> logger)
        {
            _driverPerformanceRepository = driverPerformanceRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse<DriverPerformanceDto>> GetDriverPerformanceAsync(int driverId, int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching driver performance for DriverId: {DriverId}, CompanyId: {CompanyId}", driverId, companyId);

                // Ensure the driver belongs to the same company
                var driverBelongsToCompany = await _driverPerformanceRepository.CheckDriverCompany(driverId, companyId);
                if (!driverBelongsToCompany)
                {
                    _logger.LogWarning("DriverId: {DriverId} does not belong to CompanyId: {CompanyId}", driverId, companyId);
                    return ServiceResponse<DriverPerformanceDto>.ErrorResponse("No driver found for the provided Driver ID", 400);
                }

                // Fetch driver performance
                var performance = await _driverPerformanceRepository.GetDriverPerformanceAsync(driverId, companyId);
                _logger.LogInformation("Successfully retrieved driver performance for DriverId: {DriverId}", driverId);

                return ServiceResponse<DriverPerformanceDto>.SuccessResponse(performance, "Driver performance data fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching driver performance for DriverId: {DriverId}, CompanyId: {CompanyId}", driverId, companyId);
                return ServiceResponse<DriverPerformanceDto>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    }
}
