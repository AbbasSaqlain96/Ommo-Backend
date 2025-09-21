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
    [Route("api/accident")]
    public class AccidentController : Controller
    {
        private readonly IAccidentDetailsService _accidentDetailsService;
        private readonly IAccidentService _accidentService;
        private readonly ILogger<AccidentController> _logger;

        public AccidentController(IAccidentDetailsService accidentDetailsService, IAccidentService accidentService, ILogger<AccidentController> logger)
        {
            _accidentDetailsService = accidentDetailsService;
            _accidentService = accidentService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        [Route("details")]
        public async Task<IActionResult> GetAccidentDetails([FromQuery] int eventId)
        {
            _logger.LogInformation("Fetching accident details for EventID: {EventID}", eventId);

            if (eventId <= 0)
            {
                _logger.LogWarning("Invalid EventID received: {EventID}", eventId);
                return ApiResponse.Error("Invalid Event ID.", 400);
            }

            // Validate user permissions for accessing the driver's documents
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                // Fetch accident details
                var response = await _accidentDetailsService.GetAccidentDetailsAsync(eventId, companyId);
                if (!response.Success)
                {
                    if (response.StatusCode == 401)
                    {
                        _logger.LogWarning("User does not have permission to access event. EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);
                        return ApiResponse.Error("You do not have permission to access this resource.", 401);
                    }

                    if (response.StatusCode == 503)
                    {
                        _logger.LogWarning("Server temporarily unavailable while fetching accident details. EventId: {EventId}, CompanyId: {CompanyId}", eventId, companyId);
                        return ApiResponse.Error("Server is temporarily unavailable. Please try again later.", 503);
                    }

                    _logger.LogWarning("Failed to fetch accident details for EventID: {EventID}, CompanyID: {CompanyID}. Error: {ErrorMessage}", eventId, companyId, response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, 400);
                }

                _logger.LogInformation("Successfully retrieved accident details for EventID: {EventID}, CompanyID: {CompanyID}.", eventId, companyId);
                return ApiResponse.Success(response.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching accident details for EventID: {EventID}.", eventId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost]
        [Authorize]
        [Route("create")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateAccident(
           [FromForm] CreateAccidentRequest createAccidentRequest)
        {
            _logger.LogInformation("Received request to create accident.");

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
                _logger.LogInformation("Creating accident for Company ID: {CompanyID}", companyId);

                var accidentCreationResult = await _accidentService.CreateAccidentAsync(companyId, createAccidentRequest);

                // Check if the service response was successful
                if (!accidentCreationResult.Success)
                {
                    _logger.LogWarning("Failed to create accident. Error: {ErrorMessage}", accidentCreationResult.ErrorMessage);

                    // Return error response from service
                    return ApiResponse.Error(accidentCreationResult.ErrorMessage, accidentCreationResult.StatusCode);
                }

                _logger.LogInformation("Successfully created accident for Company ID: {CompanyID}", companyId);
                return ApiResponse.Success(null, accidentCreationResult.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating an accident for Company ID: {CompanyID}", companyId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPut]
        [Authorize]
        [Route("update")]
        public async Task<IActionResult> UpdateAccident([FromForm] UpdateAccidentRequest request)
        {
            _logger.LogInformation("Received request to update an accident.");

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
                var result = await _accidentService.UpdateAccidentAsync(request, companyId);

                if (!result.Success)
                {
                    _logger.LogWarning("Accident update failed: {ErrorMessage}", result.ErrorMessage);
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                }

                _logger.LogInformation("Accident updated successfully for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(result.Data, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating an accident.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}