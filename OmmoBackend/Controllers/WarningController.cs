using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [Route("api/warning")]
    [ApiController]
    public class WarningController : Controller
    {
        private readonly IWarningService _warningService;
        private readonly ILogger<WarningController> _logger;
        public WarningController(IWarningService warningService, ILogger<WarningController> logger)
        {
            _warningService = warningService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        [Route("create")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateWarningRequest request)
        {
            _logger.LogInformation("Processing request to create warning.");

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

            try
            {
                _logger.LogInformation("Creating warning for Company ID: {CompanyId}", companyId);

                var warningCreationResult = await _warningService.CreateWarningAsync(companyId, request);

                // Check if the service response was successful
                if (!warningCreationResult.Success)
                {
                    _logger.LogWarning("Warning creation failed: {ErrorMessage}", warningCreationResult.ErrorMessage);
                    return ApiResponse.Error(warningCreationResult.ErrorMessage, warningCreationResult.StatusCode);
                }

                _logger.LogInformation("Warning created successfully for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(null, warningCreationResult.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating warning.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpGet]
        [Authorize]
        [Route("details")]
        public async Task<IActionResult> Get([FromQuery] int eventId)
        {
            _logger.LogInformation("Fetching warning details for EventID: {EventID}", eventId);

            if (eventId <= 0)
            {
                _logger.LogWarning("Invalid EventID received: {EventID}", eventId);
                return ApiResponse.Error("Invalid Event ID.", 400);
            }

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                // Fetch warning details
                var response = await _warningService.GetWarningDetailsAsync(eventId, companyId);
                if (!response.Success)
                {
                    _logger.LogWarning("Failed to fetch warning details for EventID: {EventID}, CompanyID: {CompanyID}. Error: {ErrorMessage}", eventId, companyId, response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, 400);
                }

                _logger.LogInformation("Successfully retrieved warning details for EventID: {EventID}, CompanyID: {CompanyID}.", eventId, companyId);
                return ApiResponse.Success(response.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching warning details for EventID: {EventID}.", eventId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPut("update")]
        [Authorize]
        public async Task<IActionResult> Update([FromForm] UpdateWarningRequest request)
        {
            _logger.LogInformation("Received request to update warning.");

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

            if (request == null || request.EventId <= 0)
                return ApiResponse.Error("No warning found for the provided Event ID.", 400);

            try
            {
                // Call service method to handle the logic
                var response = await _warningService.UpdateWarningAsync(companyId, request);

                // Check if the service response was successful
                if (!response.Success)
                {
                    _logger.LogWarning("Warning update failed: {ErrorMessage}", response.ErrorMessage);
                    // Return error response from service
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Warning updated successfully for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(response.Data, response.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating warning.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}
