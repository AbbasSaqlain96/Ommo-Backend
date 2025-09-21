using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Services.Interfaces;
using System.Diagnostics.Tracing;

namespace OmmoBackend.Controllers
{
    [Route("api/events")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ILogger<EventsController> _logger;

        public EventsController(IEventService eventService, ILogger<EventsController> logger)
        {
            _eventService = eventService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        [Route("get-events")]
        public async Task<IActionResult> GetEvents(
            [FromQuery] int? driverId,
            [FromQuery] int? truckId,
            [FromQuery] int? trailerId,
            [FromQuery] int? loadId,
            [FromQuery] string? authority,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? eventType)
        {
            _logger.LogInformation("Fetching events with parameters: DriverId={DriverId}, TruckId={TruckId}, StartDate={StartDate}, EndDate={EndDate}, EventType={EventType}", driverId, truckId, startDate, endDate, eventType);

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            _logger.LogInformation("Company ID extracted: {CompanyId}", companyId);

            try
            {
                // Extract company ID and access level from the user token
                var response = await _eventService.GetEventsAsync(driverId, companyId, truckId, trailerId, loadId, authority, startDate, endDate, eventType);
                if (!response.Success)
                {
                    _logger.LogWarning("Failed to fetch events: {ErrorMessage}", response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                if (response.Data?.Count() == 0)
                    return ApiResponse.Success(response.Data, "No events found for the given filters.");

                _logger.LogInformation("Successfully fetched {EventCount} events.", response.Data?.Count() ?? 0);
                return ApiResponse.Success(response.Data, "Events retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error occurred while fetching events.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}
