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
    [Route("api/onboarding")]
    [ApiController]
    public class OnboardingController : ControllerBase
    {
        private readonly ILogger<OnboardingController> _logger;
        private readonly IOnboardingService _onboardingService;
        private readonly IAuthService _authService;
        public OnboardingController(ILogger<OnboardingController> logger, IOnboardingService onboardingService, IAuthService authService)
        {
            _logger = logger;
            _onboardingService = onboardingService;
            _authService = authService;
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

                if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
                {
                    return ApiResponse.Error("Either email or phone is required.", 400);
                }

                LoginRequest loginRequest = new LoginRequest
                {
                    EmailOrPhone = !string.IsNullOrWhiteSpace(request.Email)
                        ? request.Email
                        : request.Phone!,
                    Password = request.Password
                };

                var onboardingResult = await _authService.AuthenticateAsync(loginRequest);

                return ApiResponse.Success(
                    new
                    {
                        token = onboardingResult.Data.Token,
                        refreshToken = onboardingResult.Data.RefreshToken,
                        onboarding = new
                        {
                            is_completed = result.Data.OnboardingDto.IsCompleted,
                            current_step = result.Data.OnboardingDto.CurrentStep
                        }
                    },
                    result.Message
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in SignupCompany. Email: {Email}, Phone: {Phone}",
                    request?.Email,
                    request?.Phone);

                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost("questionnaire/complete")]
        [Authorize]
        [RequireAuthenticationOnly]
        public async Task<IActionResult> CompleteQuestionnaire([FromBody] List<QuestionnaireAnswerRequest> request)
        {
            try
            {
                if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                    return error;

                var result = await _onboardingService.CompleteQuestionnaireAsync(companyId, request);

                if (!result.Success)
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);

                return ApiResponse.Success(null, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error completing questionnaire for CompanyId {CompanyId}",
                    User?.Identity?.Name);

                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}
