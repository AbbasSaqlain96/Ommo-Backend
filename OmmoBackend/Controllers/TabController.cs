using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Middlewares;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/tab")]
    public class TabController : ControllerBase
    {
        private readonly ITabService _tabService;
        private readonly ILogger<TabController> _logger;
        /// <summary>
        /// Initializes a new instance of the TabController class with the specified tab service.
        /// </summary>
        public TabController(ITabService tabService, ILogger<TabController> logger)
        {
            _tabService = tabService;
            _logger = logger;
        }

        [HttpGet]
        [Route("get-tabs")]
        [Authorize]
        [RequireAuthenticationOnly]
        public async Task<IActionResult> GetTabs()
        {
            _logger.LogInformation("Processing request to retrieve tabs.");

            try
            {
                // Extract Role_ID from JWT token
                var roleIdClaim = User.FindFirst("Role_ID")?.Value;
                if (string.IsNullOrEmpty(roleIdClaim) || !int.TryParse(roleIdClaim, out var roleId))
                {
                    _logger.LogWarning("Unauthorized access attempt due to missing or invalid Role_ID.");
                    return ApiResponse.Error("You do not have permission to access this resource", 401);
                }

                _logger.LogInformation("Fetching tabs for Role_ID: {RoleId}", roleId);

                // Call the service method to get tabs
                var result = await _tabService.GetTabsAsync(roleId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to retrieve tabs for Role_ID: {RoleId}, Reason: {Reason}", roleId, result.ErrorMessage);
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                }

                if (result.Data.Tabs == null || !result.Data.Tabs.Any())
                {
                    _logger.LogWarning("No tabs found for Role_ID: {RoleId}", roleId);
                    return ApiResponse.Error("No tabs found.", 404);
                }

                _logger.LogInformation("Successfully retrieved {TabCount} tabs for Role_ID: {RoleId}", result.Data.Tabs.Count(), roleId);
                // Return the tabs
                return ApiResponse.Success(result.Data, "Tabs retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error occurred while retrieving tabs.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}