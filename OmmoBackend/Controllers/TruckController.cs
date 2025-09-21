using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Middlewares;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/truck")]
    public class TruckController : ControllerBase
    {
        private readonly ITruckService _truckService;
        private readonly ILogger<TruckController> _logger;
        /// <summary>
        /// Initializes a new instance of the TruckController class with the specified truck service.
        /// </summary>
        public TruckController(ITruckService truckService, ILogger<TruckController> logger)
        {
            _truckService = truckService;
            _logger = logger;
        }

        [HttpGet("get-truck-info")]
        [Authorize]
        [RequireAuthenticationOnly]
        public async Task<IActionResult> GetTruckInfo(int unitId)
        {
            _logger.LogInformation("Received request to get truck info for Unit ID: {UnitId}", unitId);

            if (unitId <= 0)
            {
                _logger.LogWarning("Invalid Unit ID provided: {UnitId}", unitId);
                return BadRequest(new { errorMessage = "Invalid Unit ID provided" });
            }

            try
            {
                _logger.LogInformation("Fetching truck info for Unit ID: {UnitId}", unitId);
                // Call the service method to get truck info
                var result = await _truckService.GetTruckInfoAsync(unitId);

                if (result == null)
                {
                    _logger.LogWarning("No truck information found for Unit ID: {UnitId}", unitId);
                    return NotFound(new { errorMessage = "Truck information not found." });
                }

                _logger.LogInformation("Successfully retrieved truck info for Unit ID: {UnitId}", unitId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving truck info for Unit ID: {UnitId}", unitId);
                return StatusCode(500, new { errorMessage = ErrorMessages.InternalServerError });
            }
        }

        [HttpGet]
        [Authorize]
        [RequireAuthenticationOnly]
        [Route("get-truck-list")]
        public async Task<IActionResult> GetTruckList()
        {
            _logger.LogInformation("Received request to get truck list.");

            int companyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");
            if (companyId <= 0)
            {
                _logger.LogWarning("Invalid Company ID provided: {CompanyId}", companyId);
                return ApiResponse.Error("Invalid Company ID.", 400);
            }

            try
            {
                _logger.LogInformation("Fetching truck list for Company ID: {CompanyId}", companyId);

                var response = await _truckService.GetTruckListAsync(companyId);

                if (!response.Success)
                {
                    _logger.LogWarning("Error occurred in service: {ErrorMessage}", response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Successfully retrieved truck list for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(response.Data, "Truck list retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while retrieving the truck list.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}