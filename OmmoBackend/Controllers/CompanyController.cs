using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    [Route("api/company")]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;
        private readonly IUserService _userService;
        private readonly ILogger<CompanyController> _logger;

        /// <summary>
        /// Initializes a new instance of the CompanyController class with the specified company service.
        /// </summary>
        public CompanyController(
            ICompanyService companyService,
            IUserService userService,
            ILogger<CompanyController> logger)
        {
            _companyService = companyService;
            _userService = userService;
            _logger = logger;
        }

        [HttpPost]
        [Route("create-company")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest createCompanyRequest)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .Select(ms => ms.Value.Errors.First().ErrorMessage)
                    .FirstOrDefault();

                return ApiResponse.Error(firstError, 400);
            }

            try
            {
                _logger.LogInformation("Creating company with name: {CompanyName}", createCompanyRequest.Name);

                // Call the service layer to create the company asynchronously
                var companyCreationResult = await _companyService.CreateCompanyAsync(createCompanyRequest);

                // Check if the company creation was successful; if not, return the error message
                if (!companyCreationResult.Success)
                {
                    _logger.LogWarning("Company creation failed: {ErrorMessage}", companyCreationResult.ErrorMessage);
                    return ApiResponse.Error(companyCreationResult.ErrorMessage, companyCreationResult.StatusCode);
                }

                _logger.LogInformation("Company created successfully with ID: {CompanyId}", companyCreationResult.Data.CompanyId);
                return ApiResponse.Success(new { companyId = companyCreationResult.Data.CompanyId }, companyCreationResult.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the company.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }


        // [HttpPost]
        // [Route("create-child-company")]
        // public async Task<IActionResult> CreateChildCompany([FromBody] CreateChildCompanyRequest createChildCompanyRequest)
        // {
        //     // Check if the model state is valid; return a bad request if any validation errors exist
        //     if (!ModelState.IsValid)
        //         return BadRequest(ModelState);

        //     // Validate the 'Type' property of the request; it must be either 1 (Carrier) or 2 (Dispatch Service)
        //     if (createChildCompanyRequest.Type != 1 && createChildCompanyRequest.Type != 2)
        //         return BadRequest(new { error = "Invalid Type. Type must be 1 (Carrier) or 2 (Dispatch Service)" });

        //     // Call the service layer to create the child company asynchronously
        //     var childCompanyCreationResult = await _companyService.CreateChildCompanyAsync(createChildCompanyRequest);

        //     // Check if the child company creation was unsuccessful
        //     if (!childCompanyCreationResult.Success)
        //         return BadRequest(new { error = "Child company creation failed: " + childCompanyCreationResult.ErrorMessage });

        //     // Return a success response if the child company was created successfully
        //     return Ok(new { message = "Child company created successfully" });
        // }

        // [HttpPut]
        // [Route("remove-child-company")]
        // public async Task<IActionResult> RemoveChildCompany([FromBody] RemoveChildCompanyRequest removeChildCompanyRequest)
        // {
        //     // Check if the model state is valid; return a bad request if any validation errors exist
        //     if (!ModelState.IsValid)
        //         return BadRequest(ModelState);

        //     // Validate the Type field; it must be either 1 (Carrier) or 2 (Dispatch Service)
        //     if (removeChildCompanyRequest.Type != 1 && removeChildCompanyRequest.Type != 2)
        //         return BadRequest("Invalid Type. Type must be 1 (Carrier) or 2 (Dispatch Service).");

        //     // Call the service layer to remove the child company asynchronously
        //     var childCompanyRemovalResult = await _companyService.RemoveChildCompanyAsync(removeChildCompanyRequest);

        //     // Check if the child company removal was unsuccessful
        //     if (!childCompanyRemovalResult.Success)
        //         return BadRequest(new { error = "Child company removal failed: " + childCompanyRemovalResult.ErrorMessage });

        //     // Return a success response if the child company was removed successfully
        //     return Ok(new { message = "Child company removed successfully" });
        // }


        [HttpGet]
        [Route("get-company-profile")]
        [Authorize]
        public async Task<IActionResult> GetCompanyProfile()
        {
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                _logger.LogInformation("Fetching company profile for Company ID: {CompanyId}", companyId);

                var response = await _companyService.GetCompanyProfileAsync(companyId);

                if (!response.Success)
                {
                    _logger.LogWarning("Company profile fetch failed: {ErrorMessage}", response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Successfully retrieved company profile for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(response.Data, response.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service unavailable while retrieving company profile.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPatch]
        [Route("update-company-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateCompanyProfile([FromForm] UpdateCompanyProfileDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .Select(ms => ms.Value.Errors.First().ErrorMessage)
                    .FirstOrDefault();

                return ApiResponse.Error(firstError, 400);
            }

            if (updateDto == null)
            {
                _logger.LogWarning("Update request failed: Request body is null.");
                return ApiResponse.Error("Request body cannot be null.", StatusCodes.Status400BadRequest);
            }

            int companyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");
            if (companyId <= 0)
            {
                _logger.LogWarning("Update request failed: Invalid company ID ({CompanyId}).", companyId);
                return ApiResponse.Error("Invalid company id.", StatusCodes.Status400BadRequest);
            }

            try
            {
                _logger.LogInformation("Updating company profile for Company ID: {CompanyId}", companyId);

                var response = await _companyService.UpdateCompanyProfileAsync(companyId, updateDto);

                if (!response.Success)
                {
                    _logger.LogWarning("Failed to update company profile: {ErrorMessage}", response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Company profile updated successfully for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(null, "Company profile updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the company profile for Company ID: {CompanyId}", companyId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}