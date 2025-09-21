using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Middlewares;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/issue-ticket")]
    public class IssueTicketController : ControllerBase
    {
        private readonly IIssueTicketService _issueTicketService;
        private readonly ILogger<IssueTicketController> _logger;
        public IssueTicketController(IIssueTicketService issueTicketService, ILogger<IssueTicketController> logger)
        {
            _issueTicketService = issueTicketService;
            _logger = logger;
        }

        [HttpGet("get")]
        [Authorize]
        public async Task<IActionResult> GetIssueTickets()
        {
            // Extract CompanyId from token
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                _logger.LogInformation("Fetching issue tickets for Company ID: {CompanyId}", companyId);

                var response = await _issueTicketService.GetIssueTicketsAsync(companyId);
                if (!response.Success)
                {
                    _logger.LogWarning("No issue tickets found for Company ID: {CompanyId}. Reason: {Reason}", companyId, response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Successfully retrieved issue tickets for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(response.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving issue tickets for Company ID");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreateIssueTicket(
            [FromForm] CreateIssueTicketRequest request)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .Select(ms => ms.Value.Errors.First().ErrorMessage)
                    .FirstOrDefault();

                return ApiResponse.Error(firstError, 400);
            }

            // Extract CompanyId from token
            int companyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");
            if (companyId <= 0)
            {
                _logger.LogWarning("Invalid Company ID: {CompanyId}", companyId);
                return ApiResponse.Error("Invalid Company ID.", 400);
            }

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"); // Extract User ID from token
            if (userId <= 0)
            {
                _logger.LogWarning("Invalid User ID: {UserId}", userId);
                return ApiResponse.Error("You do not have permission to access this resource", 401);
            }

            try
            {
                _logger.LogInformation("Creating issue ticket for Company ID: {CompanyId}, User ID: {UserId}", companyId, userId);

                var response = await _issueTicketService.CreateIssueTicketAsync(request, companyId, userId);

                if (!response.Success)
                {
                    _logger.LogWarning("Failed to create issue ticket for Company ID: {CompanyId}, User ID: {UserId}", companyId, userId);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Successfully created issue ticket for Company ID: {CompanyId}, User ID: {UserId}", companyId, userId);
                return ApiResponse.Success(response.Data, response.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while creating issue ticket");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPut("update")]
        [Authorize]
        public async Task<IActionResult> UpdateIssueTicket(
            [FromForm] UpdateIssueTicketRequest request)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .Select(ms => ms.Value.Errors.First().ErrorMessage)
                    .FirstOrDefault();

                return ApiResponse.Error(firstError, 400);
            }

            // Extract CompanyId from token
            int companyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");
            if (companyId <= 0)
            {
                _logger.LogWarning("Invalid Company ID: {CompanyId}", companyId);
                return ApiResponse.Error("Invalid Company ID.", 400);
            }

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"); // Extract User ID from token
            if (userId <= 0)
            {
                _logger.LogWarning("Invalid User ID: {UserId}", userId);
                return ApiResponse.Error("You do not have permission to access this resource", 401);
            }

            try
            {
                _logger.LogInformation("Updating issue ticket for Company ID: {CompanyId}, User ID: {UserId}", companyId, userId);
                var result = await _issueTicketService.UpdateIssueTicketAsync(request, userId, companyId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to update issue ticket for Company ID: {CompanyId}, User ID: {UserId}", companyId, userId);
                    return ApiResponse.Error(result.ErrorMessage ?? result.Message, result.StatusCode);
                }

                _logger.LogInformation("Successfully updated issue ticket for Company ID: {CompanyId}, User ID: {UserId}", companyId, userId);
                return ApiResponse.Success(result.Data, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error while updating issue ticket for Company ID: {CompanyId}, User ID: {UserId}", companyId, userId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}