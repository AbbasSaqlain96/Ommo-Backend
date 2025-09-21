using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [Route("api/aiagent")]
    [ApiController]
    public class AIAgentController : ControllerBase
    {
        private readonly ILogger<AIAgentController> _logger;
        private readonly IAIAgentService _aiagentService;
        private readonly ICallTranscriptService _transcriptService;

        public AIAgentController(ILogger<AIAgentController> logger, IAIAgentService aiagentService, ICallTranscriptService transcriptService)
        {
            _logger = logger;
            _aiagentService = aiagentService;
            _transcriptService = transcriptService;
        }

        [HttpPost("register-agent")]
        [Authorize]
        public async Task<IActionResult> RegisterAgent([FromBody] RegisterAIAgentRequest request)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .Select(ms => ms.Value.Errors.First().ErrorMessage)
                    .FirstOrDefault();

                return ApiResponse.Error(firstError, 400);
            }

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                _logger.LogInformation("Registering an AI agent for a company");

                var result = await _aiagentService.RegisterAIAgentAsync(request);

                if (!result.Success)
                {
                    _logger.LogWarning("AI Agent creation failed: {ErrorMessage}", result.ErrorMessage);
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                }

                _logger.LogInformation("AI Agent created successfully");
                return ApiResponse.Success(result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while registering an AI agent for a company.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpGet("getTranscript/{callId}")]
        [Authorize]
        public async Task<IActionResult> GetTranscript(int callId)
        {
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            try
            {
                _logger.LogInformation("Fetch call transcript.");

                var result = await _transcriptService.GetTranscriptAsync(callId, companyId);

                if (!result.Success) 
                {
                    _logger.LogWarning("Fetch call transcript failed: {ErrorMessage}", result.ErrorMessage);
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                }

                _logger.LogInformation("Call transcript fetched successfully");
                return ApiResponse.Success(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching call transcript.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}
