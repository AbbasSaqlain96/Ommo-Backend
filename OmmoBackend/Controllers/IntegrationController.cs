using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [Route("api/integration")]
    [ApiController]
    public class IntegrationController : ControllerBase
    {
        private readonly IIntegrationService _integrationService;
        private readonly ILogger<IntegrationController> _logger;
        public IntegrationController(IIntegrationService integrationService, ILogger<IntegrationController> logger)
        {
            _integrationService = integrationService;
            _logger = logger;
        }

        [HttpGet("get-integrations")]
        [Authorize]
        public async Task<IActionResult> GetIntegrations()
        {
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                var response = await _integrationService.GetIntegrationsAsync(companyId);

                if (!response.Success)
                {
                    _logger.LogWarning("Failed to fetch integrations for Company ID: {CompanyId}", companyId);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Successfully fetched integrations for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(response.Data, "Integrations fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the integrations.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpGet("default-integration")]
        [Authorize]
        public async Task<IActionResult> GetDefaultIntegration()
        {
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                var response = await _integrationService.GetDefaultIntegrationsAsync(companyId);

                if (!response.Success)
                {
                    _logger.LogWarning("Failed to fetch default integrations for Company ID: {CompanyId}", companyId);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Successfully fetched default integrations for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(response.Data, "Default Integrations fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the default integrations.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost("send-integration-request")]
        public async Task<IActionResult> SendIntegrationRequest([FromBody] IntegrationRequestDto request)
        {
            // Validate model state
            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                                  .Where(ms => ms.Value.Errors.Any())
                                  .Select(ms => ms.Value.Errors.First().ErrorMessage)
                                  .FirstOrDefault();

                return ApiResponse.Error(firstError, 400);
            }

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            int userId = TokenHelper.GetUserIdFromClaims(User);

            try
            {
               var result = await _integrationService.SendIntegrationRequestAsync(userId, companyId, request);

                if (!result.Success)
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);

                return ApiResponse.Success(result.Data, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending the integration request.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPatch("toggle-integration-status")]
        public async Task<IActionResult> ToggleStatus([FromBody] ToggleIntegrationStatusRequest request)
        {
            var serviceResponse = await _integrationService.ToggleStatusAsync(request);

            if (!serviceResponse.Success)
                return ApiResponse.Error(serviceResponse.ErrorMessage, serviceResponse.StatusCode);

            return ApiResponse.Success(serviceResponse.Data, serviceResponse.Message);
        }
    }
}
