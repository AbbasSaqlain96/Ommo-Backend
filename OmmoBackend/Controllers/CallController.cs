using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [Route("api/call")]
    [ApiController]
    public class CallController : ControllerBase
    {
        private readonly ICallService _callService;
        private readonly ILogger<CallController> _logger;
        public CallController(ICallService callService, ILogger<CallController> logger)
        {
            _callService = callService;
            _logger = logger;
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
    }
}
