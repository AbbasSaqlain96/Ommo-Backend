using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [Route("api/loadboard")]
    [ApiController]
    public class LoadBoardController : ControllerBase
    {
        private readonly ILoadBoardService _loadBoardService;
        private readonly ILogger<LoadBoardController> _logger;
        public LoadBoardController(ILoadBoardService loadBoardService, ILogger<LoadBoardController> logger)
        {
            _loadBoardService = loadBoardService;
            _logger = logger;
        }

        [HttpGet("get-loads")]
        [Authorize]
        public async Task<IActionResult> GetLoads([FromQuery] LoadFiltersDto filters)
        {
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error!;

            try
            {
                var response = await _loadBoardService.GetLoadsAsync(companyId, filters);

                if (!response.Success)
                {
                    _logger.LogWarning("Failed to fetch loads for Company ID: {CompanyId}", companyId);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Successfully fetched loads for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(response.Data, response.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the loads.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}
