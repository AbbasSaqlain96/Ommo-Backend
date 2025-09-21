using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [Route("api/performance")]
    [ApiController]
    public class PerformanceController : ControllerBase
    {
        private readonly IPerformanceService _performanceService;
        private readonly ILogger<PerformanceController> _logger;
        public PerformanceController(IPerformanceService performanceService, ILogger<PerformanceController> logger)
        {
            _performanceService = performanceService;
            _logger = logger;
        }

        [HttpGet("get-performance")]
        [Authorize]
        public async Task<IActionResult> GetPerformance()
        {
            _logger.LogInformation("Received request to fetch performance data.");

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                var response = await _performanceService.GetPerformanceAsync(companyId);

                if (!response.Success)
                {
                    _logger.LogWarning("Failed to retrieve performance data: {ErrorMessage}", response.ErrorMessage);
                    return ApiResponse.Success(response.Data ?? new PerformanceDto(), response.ErrorMessage);
                }

                _logger.LogInformation("Successfully retrieved performance data for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(response.Data, "Performance data retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while retrieving performance data.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}
