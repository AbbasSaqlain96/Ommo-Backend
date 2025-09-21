using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [Route("api/ticket")]
    [ApiController]
    public class TicketController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly ILogger<TicketController> _logger;
        public TicketController(ITicketService ticketService, ILogger<TicketController> logger)
        {
            _ticketService = ticketService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        [Route("details")]
        public async Task<IActionResult> GetTicketDetail([FromQuery] int eventId)
        {
            _logger.LogInformation("Processing request to retrieve ticket details for Event ID: {EventId}", eventId);

            if (eventId <= 0)
            {
                _logger.LogWarning("Bad request: Event ID is missing or invalid.");
                return ApiResponse.Error("Event Id is required.", 400);
            }

            // Extract Company_ID from the authenticated user's token
            int companyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");
            if (companyId <= 0)
            {
                _logger.LogWarning("Bad request: Invalid Company ID.");
                return ApiResponse.Error("Invalid Company ID.", 400);
            }

            try
            {
                _logger.LogInformation("Fetching ticket details for Event ID: {EventId} and Company ID: {CompanyId}", eventId, companyId);

                var response = await _ticketService.GetTicketDetailsAsync(eventId, companyId);

                if (response.StatusCode == 400)
                {
                    _logger.LogWarning("Bad request: {Message}", response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, 400);
                }
                if (response.StatusCode == 401)
                {
                    _logger.LogWarning("Unauthorized access: {Message}", response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, 401);
                }
                if (response.StatusCode == 503)
                {
                    _logger.LogError("Service unavailable: {Message}", response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, 503);
                }

                if (response.Data == null)
                {
                    _logger.LogWarning("No ticket details found for Event ID: {EventId} and Company ID: {CompanyId}", eventId, companyId);
                    return ApiResponse.Error(response.ErrorMessage ?? "No ticket details found for the given event ID.", 400);
                }

                _logger.LogInformation("Successfully retrieved ticket details for Event ID: {EventId}", eventId);
                return ApiResponse.Success(response.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving ticket details for Event ID: {EventId}", eventId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost("create")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateTicket(
            [FromForm] CreateTicketRequest ticketRequest)
        {
            _logger.LogInformation("Processing request to create a new ticket.");

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
                _logger.LogInformation("Creating ticket for Company ID: {CompanyId}", companyId);

                var ticketCreationResult = await _ticketService.CreateTicketAsync(companyId, ticketRequest);

                // Check if the service response was successful
                if (!ticketCreationResult.Success)
                {
                    _logger.LogWarning("Ticket creation failed: {ErrorMessage}", ticketCreationResult.ErrorMessage);
                    return ApiResponse.Error(ticketCreationResult.ErrorMessage, ticketCreationResult.StatusCode);
                }

                _logger.LogInformation("Ticket created successfully for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(null, ticketCreationResult.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access.");
                return ApiResponse.Error("You do not have permission to access this resource", 401);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a ticket.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPut("update")]
        [Authorize]
        public async Task<IActionResult> UpdateTicket([FromForm] UpdateTicketRequest request) 
        {
            _logger.LogInformation("Received request to update a ticket.");

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
                if (request == null || request.EventId <= 0)
                    return ApiResponse.Error("No ticket found for the provided Event ID.", 400);

                // Call service method to handle the logic
                var response = await _ticketService.UpdateTicketAsync(companyId, request);

                // Check if the service response was successful
                if (!response.Success)
                {
                    _logger.LogWarning("Ticket update failed: {ErrorMessage}", response.ErrorMessage);
                    // Return error response from service
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Ticket updated successfully for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(response.Data, response.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating a ticket.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}
