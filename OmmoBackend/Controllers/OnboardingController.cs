using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [Route("api/onboarding")]
    [ApiController]
    public class OnboardingController : ControllerBase
    {
        private readonly ILogger<OnboardingController> _logger;
        private readonly IOnboardingService _onboardingService;
        public OnboardingController(ILogger<OnboardingController> logger, IOnboardingService onboardingService)
        {
            _logger = logger;
            _onboardingService = onboardingService;
        }

        [HttpPost]
        [Route("signup-company")]
        [AllowAnonymous]
        public async Task<IActionResult> SignupCompany([FromForm] SignupCompanyRequest request)
        {
            // Check if the request model state is valid
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
                var result = await _onboardingService.SignupCompanyAsync(request);
                if (!result.Success)
                {
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                }

                return ApiResponse.Success(new { companyId = result.Data.CompanyId, userId = result.Data.UserId }, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the company and the user.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}
