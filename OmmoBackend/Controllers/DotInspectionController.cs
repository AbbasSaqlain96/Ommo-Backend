using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Services.Implementations;
using OmmoBackend.Services.Interfaces;
using Twilio.Http;

namespace OmmoBackend.Controllers
{
    [Route("api/dot-inspection")]
    [ApiController]
    public class DotInspectionController : Controller
    {
        private readonly IDotInspectionService _dotInspectionService;
        private readonly ILogger<DotInspectionController> _logger;
        public DotInspectionController(IDotInspectionService dotInspectionService, ILogger<DotInspectionController> logger)
        {
            _dotInspectionService = dotInspectionService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        [Route("create")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateDotInspectionRequest request)
        {
            _logger.LogInformation("Processing request to create dot inspection.");

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
                _logger.LogInformation("Creating dot inspection for Company ID: {CompanyId}", companyId);

                var dotInspectionCreationResult = await _dotInspectionService.CreateDotInspectionAsync(companyId, request);

                // Check if the service response was successful
                if (!dotInspectionCreationResult.Success)
                {
                    _logger.LogWarning("Dot Inspection creation failed: {ErrorMessage}", dotInspectionCreationResult.ErrorMessage);
                    return ApiResponse.Error(dotInspectionCreationResult.ErrorMessage, dotInspectionCreationResult.StatusCode);
                }

                _logger.LogInformation("Dot inspection created successfully for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(null, dotInspectionCreationResult.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating dot inspection.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpGet]
        [Authorize]
        [Route("details")]
        public async Task<IActionResult> Get([FromQuery] int eventId)
        {
            _logger.LogInformation("Fetching dot inspection details for EventID: {EventID}", eventId);

            if (eventId <= 0)
            {
                _logger.LogWarning("Invalid EventID received: {EventID}", eventId);
                return ApiResponse.Error("Invalid Event ID.", 400);
            }

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                // Fetch dot inspection details
                var response = await _dotInspectionService.GetDotInspectionDetailsAsync(eventId, companyId);
                if (!response.Success)
                {
                    _logger.LogWarning("Failed to fetch dot inspection details for EventID: {EventID}, CompanyID: {CompanyID}. Error: {ErrorMessage}", eventId, companyId, response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, 400);
                }

                _logger.LogInformation("Successfully retrieved dot inspection details for EventID: {EventID}, CompanyID: {CompanyID}.", eventId, companyId);
                return ApiResponse.Success(response.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching dot inspection details for EventID: {EventID}.", eventId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPut("update")]
        [Authorize]
        public async Task<IActionResult> Update([FromForm] UpdateDotInspectionRequest request)
        {
            _logger.LogInformation("Received request to update dot inspection.");

            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .Select(ms => ms.Value.Errors.First().ErrorMessage)
                    .FirstOrDefault();

                return ApiResponse.Error(firstError, 400);
            }

            if (request == null || request.EventId <= 0)
                return ApiResponse.Error("No dot inspection found for the provided Event ID.", 400);

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                // Call service method to handle the logic
                var response = await _dotInspectionService.UpdateDotInspectionAsync(companyId, request);

                // Check if the service response was successful
                if (!response.Success)
                {
                    _logger.LogWarning("Dot inspection update failed: {ErrorMessage}", response.ErrorMessage);
                    // Return error response from service
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Dot inspection updated successfully for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(response.Data, response.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating dot inspection.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}
