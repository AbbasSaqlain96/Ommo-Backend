
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/role")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly IUserService _userService;
        private readonly ILogger<RoleController> _logger;

        /// <summary>
        /// Initializes a new instance of the RoleController class with the specified role and user service.
        /// </summary>
        public RoleController(
            IRoleService roleService,
            IUserService userService,
            ILogger<RoleController> logger)
        {
            _roleService = roleService;
            _userService = userService;
            _logger = logger;
        }

        [HttpPost]
        [Route("create-role")]
        [Authorize]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto createRoleDto)
        {
            try
            {
                _logger.LogInformation("CreateRole API called.");

                // Extract Company_ID from the authenticated user's token
                int companyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");

                if (companyId <= 0)
                {
                    _logger.LogWarning("Invalid Company ID extracted from token.");
                    return ApiResponse.Error("Invalid Company ID.", 400);
                }

                _logger.LogInformation("Creating role for CompanyID: {CompanyId} with RoleName: {RoleName}", companyId, createRoleDto.RoleName);

                var response = await _roleService.CreateRoleAsync(createRoleDto, companyId);

                if (!response.Success)
                {
                    _logger.LogWarning("Role creation failed for CompanyID: {CompanyId}. Error: {ErrorMessage}", companyId, response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Role created successfully for CompanyID: {CompanyId}. RoleID: {RoleId}", companyId, response.Data?.role_id);
                return ApiResponse.Success(response.Data, "Role created successfully.");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogWarning(uaEx, "Unauthorized access while creating role.");
                return ApiResponse.Error("You do not have permission to access this resource", 401);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while creating role.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpGet]
        [Route("get-roles")]
        [Authorize]
        public async Task<IActionResult> GetRole()
        {
            try
            {
                // Retrieve the logged-in user's Id from the JWT claims
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                // Retrieve the company Id from the JWT claims
                var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value;

                _logger.LogInformation("GetRole request received. UserId: {UserId}, CompanyId: {CompanyId}", userId, companyIdClaim);

                // Validate if the user Id is present and parse it; return Unauthorized if the user is not authenticated properly
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyIdClaim) ||
                    !int.TryParse(userId, out var loggedInUserId) ||
                    !int.TryParse(companyIdClaim, out var companyId))
                {
                    _logger.LogWarning("Unauthorized: Missing or invalid UserId/CompanyId.");
                    return ApiResponse.Error("You do not have permission to access this resource", 401);
                }

                _logger.LogInformation("Validating if user {UserId} belongs to company {CompanyId}.", loggedInUserId, companyId);

                // Check if the logged-in user belongs to the specified company; return Forbidden if they do not have permission
                var userBelongsToCompany = await _userService.UserBelongsToCompanyAsync(loggedInUserId, companyId);

                if (!userBelongsToCompany)
                {
                    _logger.LogWarning("Access denied: User {UserId} does not belong to Company {CompanyId}", loggedInUserId, companyId);
                    return ApiResponse.Error("You do not have permission to access this resource", 401);
                }

                _logger.LogInformation("Fetching roles for CompanyId: {CompanyId}.", companyId);

                // Fetch the roles with optional filtering
                var serviceResponse = await _roleService.GetRolesAsync(companyId);

                if (!serviceResponse.Success)
                {
                    _logger.LogWarning("Failed to fetch roles for CompanyId: {CompanyId}. Error: {ErrorMessage}", companyId, serviceResponse.Message);
                    return ApiResponse.Error(serviceResponse.ErrorMessage, serviceResponse.StatusCode);
                }

                _logger.LogInformation("Roles fetched successfully for CompanyId: {CompanyId}. Returning {Count} roles.", companyId, serviceResponse.Data?.Count());

                return ApiResponse.Success(serviceResponse.Data, "Roles fetched successfully.");
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "CompanyId not found.");
                return ApiResponse.Error("The specified company does not exist.", 404);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt by UserId.");
                return ApiResponse.Error("You do not have permission to access this resource", 401);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during role retrieval.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }


        [HttpDelete("delete-role")]
        [Authorize]
        public async Task<IActionResult> DeleteRole(int roleId)
        {
            _logger.LogInformation("DeleteRole request received for RoleId: {RoleId}", roleId);

            try
            {
                // Retrieve company and role information from token claims
                var companyIdClaim = User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value;
                var userRoleIdClaim = User.Claims.FirstOrDefault(r => r.Type == "Role_ID")?.Value;

                if (string.IsNullOrEmpty(companyIdClaim) || !int.TryParse(companyIdClaim, out var companyId) || companyId <= 0)
                {
                    _logger.LogWarning("Invalid or missing Company ID in token.");
                    return ApiResponse.Error("Invalid Company ID.", 400);
                }

                if (string.IsNullOrEmpty(userRoleIdClaim) || !int.TryParse(userRoleIdClaim, out var userRoleId) || userRoleId <= 0)
                {
                    _logger.LogWarning("Invalid or missing User Role ID in token.");
                    return ApiResponse.Error("Invalid Role ID.", 400);
                }

                _logger.LogInformation("Attempting to delete RoleId: {RoleId} for CompanyId: {CompanyId} by UserRoleId: {UserRoleId}", roleId, companyId, userRoleId);

                var serviceResponse = await _roleService.DeleteRoleAsync(roleId, companyId, userRoleId);

                if (!serviceResponse.Success)
                {
                    return ApiResponse.Error(serviceResponse.ErrorMessage, serviceResponse.StatusCode);
                }

                _logger.LogInformation("RoleId: {RoleId} successfully deleted for CompanyId: {CompanyId} by UserRoleId: {UserRoleId}", roleId, companyId, userRoleId);

                return ApiResponse.Success(null, serviceResponse.Message ?? "Role deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while deleting RoleId: {RoleId}.", roleId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}