using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Middlewares;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [Route("api/billing")]
    [ApiController]
    public class BillingController : ControllerBase
    {
        private readonly ILogger<BillingController> _logger;
        private readonly IBillingService _billingService;
        public BillingController(ILogger<BillingController> logger, IBillingService billingService)
        {
            _logger = logger;
            _billingService = billingService;
        }

        [HttpGet("company-profile")]
        [Authorize]
        [RequireAuthenticationOnly]
        public async Task<IActionResult> GetCompanyProfile()
        {
            _logger.LogInformation("Received request to get company profile for user {UserId}", TokenHelper.GetUserIdFromClaims(User));

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            _logger.LogInformation("Fetching company profile for company {CompanyId}", companyId);

            var result = await _billingService.GetCompanyProfileAsync(companyId);

            if (!result.Success)
            {
                _logger.LogError("Failed to get company profile for company {CompanyId}: {ErrorMessage}", companyId, result.ErrorMessage);
                return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
            }

            _logger.LogInformation("Successfully retrieved company profile for company {CompanyId}", companyId);
            return ApiResponse.Success(result.Data, result.Message);
        }

        [HttpGet("history")]
        [Authorize]
        [RequireAuthenticationOnly]
        public async Task<IActionResult> GetBillingHistory()
        {
            _logger.LogInformation("Received request to get billing history for user {UserId}", TokenHelper.GetUserIdFromClaims(User));

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            _logger.LogInformation("Fetching billing history for company {CompanyId}", companyId);

            var result = await _billingService.GetBillingHistoryAsync(companyId);

            if (!result.Success)
            {
                _logger.LogError("Failed to get billing history for company {CompanyId}: {ErrorMessage}", companyId, result.ErrorMessage);
                return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
            }

            _logger.LogInformation("Successfully retrieved billing history for company {CompanyId}", companyId);
            return ApiResponse.Success(result.Data, result.Message);
        }

        [HttpPost("checkout")]
        [Authorize]
        [RequireAuthenticationOnly]
        public async Task<IActionResult> Checkout()
        {
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error!;

            var response = await _billingService.DummyCheckoutAsync(companyId);

            if (!response.Success)
                return ApiResponse.Error(response.ErrorMessage, response.StatusCode);

            return ApiResponse.Success(null, response.Message);
        }

        [HttpPost("buy-number")]
        [Authorize]
        [RequireAuthenticationOnly]
        public async Task<IActionResult> BuyNumber()
        {
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error!;

            var response = await _billingService.DummyBuyNumberAsync(companyId);

            if (!response.Success)
                return ApiResponse.Error(response.ErrorMessage, response.StatusCode);

            return ApiResponse.Success(null, response.Message);
        }
    }
}
