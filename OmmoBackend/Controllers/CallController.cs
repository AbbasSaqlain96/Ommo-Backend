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
    [Route("api/call")]
    [ApiController]
    public class CallController : ControllerBase
    {
        private readonly ICallService _callService;
        private readonly IUserService _userservice;
        private readonly ILogger<CallController> _logger;
        private readonly ICallSettingsService _callSettingsService;

        public CallController(ICallService callService,IUserService userservice, ILogger<CallController> logger, ICallSettingsService callSettingsService)
        {
            _callService = callService;
            _logger = logger;
            _userservice = userservice;
            _callSettingsService = callSettingsService;
        }

        [HttpGet("get-called-loads")]
        [Authorize]
        public async Task<IActionResult> GetCalledLoads()
        {
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                var result = await _callService.GetCalledLoadsAsync(companyId);

                if (!result.Success)
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);

                return ApiResponse.Success(result.Data, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while registering an AI agent for a company.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost("takeover")]
        public async Task<IActionResult> TakeoverCall([FromBody] TakeoverCallRequest request)
        {
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            var userId = TokenHelper.GetUserIdFromClaims(User);
            var user= await _userservice.GetUserByIdAsync(userId);
            //compare both user IDS ...user id from call table and this . latter.
            var userphone = user.Data.Phone;

            await _callService.TakeoverCallAsync(
                request.CallId,
                companyId,
                userId,
                userphone
            );

            return Ok(new
            {
                message = "Call takeover initiated successfully",
                call_id = request.CallId
            });
        }
        
        [HttpGet("get-calls")]
        public async Task<IActionResult> GetCalls([FromQuery] string? status)
        {
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                var result = await _callService.GetCallsAsync(companyId, status);

                if (!result.Success)
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);

                return ApiResponse.Success(result.Data, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching calls.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpGet("call-settings/available-numbers")]
        [Authorize]
        [RequireAuthenticationOnly]
        public async Task<IActionResult> GetAvailableNumbers()
        {
            _logger.LogInformation("Received request to get available numbers for call settings.");

            var result = await _callSettingsService.GetAvailableNumbersAsync();

            if (!result.Success)
            {
                _logger.LogError("Failed to get available numbers: {Message}", result.Message);
                return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
            }
                
            _logger.LogInformation("Successfully retrieved available numbers for call settings.");
            return ApiResponse.Success(result.Data, result.Message);
        }

        [HttpPost("call-settings/buy-number")]
        [Authorize]
        [RequireAuthenticationOnly]
        public async Task<IActionResult> BuyNumber([FromBody] BuyNumberRequest request)
        {
            _logger.LogInformation("Received request to buy number {PhoneNumber} for call settings.", request.PhoneNumber);

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            var result = await _callSettingsService.BuyNumberAsync(companyId, request);

            if (!result.Success)
            {
                _logger.LogError("Failed to buy number {PhoneNumber}: {Message}", request.PhoneNumber, result.Message);
                return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
            }

            _logger.LogInformation("Successfully bought number {PhoneNumber} for call settings.", request.PhoneNumber);
            return ApiResponse.Success(result.Data, result.Message);
        }
    }
}
