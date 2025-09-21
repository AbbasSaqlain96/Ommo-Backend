using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/units")]
    public class UnitController : ControllerBase
    {
        private readonly IUnitService _unitService;
        private readonly ILogger<OtpController> _logger;

        /// <summary>
        /// Initializes a new instance of the UnitController class with the specified unit service.
        /// </summary>
        public UnitController(IUnitService unitService, ILogger<OtpController> logger)
        {
            _unitService = unitService;
            _logger = logger;
        }

        [HttpGet]
        [Route("get-unit-info")]
        [Authorize]
        public async Task<IActionResult> GetUnitInfo([FromQuery] string? driverName, [FromQuery] string? unitStatus, [FromQuery] int? truckId, [FromQuery] int? trailerId)
        {
            _logger.LogInformation("Fetching unit info. Driver: {Driver}, Status: {Status}, TruckId: {TruckId}, TrailerId: {TrailerId}",
                driverName, unitStatus, truckId, trailerId);

            // Extract CompanyId from token
            int companyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");
            if (companyId <= 0)
            {
                _logger.LogWarning("Invalid Company ID in token.");
                return ApiResponse.Error("Invalid Company ID.", 400);
            }

            try
            {
                // Call the service layer to fetch units with optional filtering
                var result = await _unitService.GetUnitsAsync(companyId, driverName, unitStatus, truckId, trailerId);

                if (!result.Success)
                {
                    _logger.LogWarning("No units found for CompanyId: {CompanyId}. Error: {ErrorMessage}", companyId, result.ErrorMessage);
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                }

                _logger.LogInformation("Successfully retrieved unit info for CompanyId: {CompanyId}.", companyId);
                // Return 200 with empty array if no data
                return ApiResponse.Success(result.Data, string.IsNullOrEmpty(result.Message) ? "Units retrieved successfully." : result.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input parameters provided.");
                return ApiResponse.Error("Invalid input. Please check the provided data and try again.", 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while retrieving unit info.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}