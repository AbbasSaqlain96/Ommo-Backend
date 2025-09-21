using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/maintenance-issue")]
    public class MaintenanceIssueController : ControllerBase
    {
        private readonly IMaintenanceIssueService _maintenanceIssueService;
        private readonly IValidator<CreateMaintenanceIssueRequest> _createMaintenanceIssueValidator;
        private readonly ILogger<MaintenanceIssueController> _logger;
        public MaintenanceIssueController(IMaintenanceIssueService maintenanceIssueService, IValidator<CreateMaintenanceIssueRequest> createMaintenanceIssueValidator, ILogger<MaintenanceIssueController> logger)
        {
            _maintenanceIssueService = maintenanceIssueService;
            _createMaintenanceIssueValidator = createMaintenanceIssueValidator;
            _logger = logger;
        }

        [HttpPost]
        [Route("create-custom-maintenance-issue")]
        [Authorize]
        public async Task<IActionResult> CreateCustomMaintenanceIssue([FromBody] CreateMaintenanceIssueRequest createMaintenanceIssueRequest)
        {
            _logger.LogInformation("Received request to create custom maintenance issue: {@Request}", createMaintenanceIssueRequest);

            var validationResult = await _createMaintenanceIssueValidator.ValidateAsync(createMaintenanceIssueRequest);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for maintenance issue creation: {@Errors}", validationResult.Errors);
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            try
            {
                var result = await _maintenanceIssueService.CreateCustomMaintenanceIssueAsync(createMaintenanceIssueRequest);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to create maintenance issue: {ErrorMessage}", result.ErrorMessage);
                    return BadRequest(new { errorMessage = "Failed to create maintenance issue: " + result.ErrorMessage });
                }

                _logger.LogInformation("Custom maintenance issue created successfully.");
                return Ok(new { message = "Custom maintenance issue created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating maintenance issue.");
                return StatusCode(500, new { errorMessage = ErrorMessages.InternalServerError });
            }
        }

        [HttpGet]
        [Route("get-maintenance-issues")]
        [Authorize]
        public async Task<IActionResult> GetMaintenanceIssues([FromQuery] string? issueType = null, [FromQuery] string? issueCategory = null)
        {
            _logger.LogInformation("Fetching maintenance issues with filters - IssueType: {IssueType}, IssueCategory: {IssueCategory}", issueType, issueCategory);

            try
            {
                // Call the service to retrieve maintenance issues with optional filters
                var issues = await _maintenanceIssueService.GetMaintenanceIssuesAsync(issueType!, issueCategory!);

                if (issues == null)
                {
                    _logger.LogWarning("No maintenance issues found for given filters.");
                    return NotFound("No maintenance issues found.");
                }

                _logger.LogInformation("Successfully retrieved maintenance issues.");   
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching maintenance issues.");
                return StatusCode(500, new { errorMessage = ErrorMessages.InternalServerError });
            }
        }
    }
}