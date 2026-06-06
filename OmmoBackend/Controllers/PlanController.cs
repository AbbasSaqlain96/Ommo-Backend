using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Middlewares;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [Route("api/plan")]
    [ApiController]
    public class PlanController : ControllerBase
    {
        private readonly ILogger<PlanController> _logger;
        private readonly IPlansService _plansService;
        private readonly IBillingService _billingService;
        public PlanController(ILogger<PlanController> logger, IPlansService plansService, IBillingService billingService)
        {
            _logger = logger;
            _plansService = plansService;
            _billingService = billingService;
        }

        [HttpPost("request-custom-package")]
        [Authorize]
        [RequireAuthenticationOnly]
        public async Task<IActionResult> RequestCustomPackage([FromBody] CustomPackageRequestDto request)
        {
            _logger.LogInformation("Received custom package request from {Email} with estimated minutes {EstMinutes}, concurrency {Concurrency}, and allowed users {AllowedUsers}.",
                request.Email, request.EstMinutes, request.Concurrency, request.AllowedUsers);

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error!;

            _logger.LogInformation("Extracted CompanyId {CompanyId} from token for custom package request.", companyId);

            var result = await _plansService.RequestCustomPackageAsync(companyId, request);

            if (!result.Success)
            {
                _logger.LogError("Failed to process custom package request for CompanyId {CompanyId}. Error: {ErrorMessage}", companyId, result.ErrorMessage);
                return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
            }

            _logger.LogInformation("Successfully processed custom package request for CompanyId {CompanyId}. Message: {Message}", companyId, result.Message);
            return ApiResponse.Success(null, result.Message);
        }

        [HttpGet]
        [Authorize]
        [RequireAuthenticationOnly]
        public async Task< IActionResult> GetPlans()
        {
            _logger.LogInformation("Received request to get plans.");

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error!;

            _logger.LogInformation("Extracted CompanyId {CompanyId} from token for get plans request.", companyId);

            var result = await _plansService.GetPlansAsync(companyId);

            if (!result.Success)
            {
                _logger.LogError("Failed to retrieve plans for CompanyId {CompanyId}. Error: {ErrorMessage}", companyId, result.ErrorMessage);
                return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
            }

            _logger.LogInformation("Successfully retrieved plans for CompanyId {CompanyId}. Message: {Message}", companyId, result.Message);
            return ApiResponse.Success(result.Data, result.Message);
        }

        [HttpPost("checkout")]
        [Authorize]
        [RequireAuthenticationOnly]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
        {
            _logger.LogInformation("Received request to create checkout session for PlanId {PlanId}.", request.PlanId);

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error!;

            var result = await _billingService.CreateCheckoutSessionAsync(companyId, request);

            if (!result.Success)
            {
                _logger.LogError("Checkout failed. CompanyId={CompanyId}, Error={Error}", companyId, result.ErrorMessage);
                return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
            }

            _logger.LogInformation("Checkout success. CompanyId={CompanyId}", companyId);
            return ApiResponse.Success(result.Data, result.Message);
        }
    }
}
