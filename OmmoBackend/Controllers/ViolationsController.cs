using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Middlewares;
using OmmoBackend.Services.Interfaces;
using System.Security.Claims;
using Twilio.Http;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/violations")]
    public class ViolationsController : ControllerBase
    {
        private readonly IViolationService _violationService;
        private readonly ILogger<ViolationsController> _logger;
        public ViolationsController(IViolationService violationService, ILogger<ViolationsController> logger)
        {
            _violationService = violationService;
            _logger = logger;
        }

        [HttpGet]
        [Route("get-violations")]
        [Authorize]
        [RequireAuthenticationOnly]
        public async Task<IActionResult> GetViolations()
        {
            _logger.LogInformation("Received request to fetch violations.");

            try
            {
                var response = await _violationService.GetViolationsAsync();

                if (!response.Success)
                {
                    _logger.LogWarning("Failed to fetch violations: {Error}", response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Successfully retrieved {Count} violations.", response.Data?.Count ?? 0);
                return ApiResponse.Success(response.Data, "Violations retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while retrieving violations.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}
