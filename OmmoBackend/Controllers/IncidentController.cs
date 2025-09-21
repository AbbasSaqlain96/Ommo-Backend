using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/incident")]
    public class IncidentController : Controller
    {
        private readonly IIncidentService _incidentService;
        private readonly ILogger<IncidentController> _logger;

        public IncidentController(IIncidentService incidentService, ILogger<IncidentController> logger)
        {
            _incidentService = incidentService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        [Route("details")]
        public async Task<IActionResult> GetIncidentInfo([FromQuery] int eventId)
        {
            _logger.LogInformation("Received request to get incident details for Event ID: {EventId}", eventId);

            if (eventId <= 0)
            {
                _logger.LogWarning("Invalid Event ID provided: {EventId}", eventId);
                return ApiResponse.Error("Invalid Event ID provided.", 400);
            }

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                // Fetch incident details
                var response = await _incidentService.GetIncidentDetailsAsync(eventId, companyId);

                if (!response.Success)
                {
                    _logger.LogWarning("Incident retrieval failed: {ErrorMessage}", response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Successfully retrieved incident details for Event ID: {EventId}", eventId);
                return ApiResponse.Success(response.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while retrieving incident details for Event ID: {EventId}", eventId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost("create")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateIncident([FromForm] CreateIncidentRequest request)
        {
            _logger.LogInformation("Received request to create an incident.");

            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .Select(ms => ms.Value.Errors.First().ErrorMessage)
                    .FirstOrDefault();

                return ApiResponse.Error(firstError, 400);
            }

            // Extract Company_ID from the authenticated user's token
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            // Validate event date
            if (request.EventInfo?.EventDate > DateTime.UtcNow)
            {
                _logger.LogWarning("Event date {EventDate} is in the future.", request.EventInfo.EventDate);
                return ApiResponse.Error("Event date cannot be in the future.", 400);
            }

            try
            {
                // Call service method to handle the logic
                var response = await _incidentService.CreateIncidentAsync(companyId, request);

                // Check if the service response was successful
                if (!response.Success)
                {
                    _logger.LogWarning("Incident creation failed: {ErrorMessage}", response.ErrorMessage);
                    // Return error response from service
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Incident created successfully for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(response.Data, response.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating an incident.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPut("update")]
        [Authorize]
        public async Task<IActionResult> UpdateIncident([FromForm] UpdateIncidentRequest request)
        {
            _logger.LogInformation("Received request to update an incident.");

            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .Select(ms => ms.Value.Errors.First().ErrorMessage)
                    .FirstOrDefault();

                return ApiResponse.Error(firstError, 400);
            }

            // Extract Company_ID from the authenticated user's token
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            if (request == null || request.EventId <= 0)
                return ApiResponse.Error("No incident found for the provided Event ID.", 400);

            try
            {
                // Call service method to handle the logic
                var response = await _incidentService.UpdateIncidentAsync(companyId, request);

                // Check if the service response was successful
                if (!response.Success)
                {
                    _logger.LogWarning("Incident update failed: {ErrorMessage}", response.ErrorMessage);
                    // Return error response from service
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Incident updated successfully for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(response.Data, response.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating an incident.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}
