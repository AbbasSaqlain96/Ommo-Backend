using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/support")]
    public class SupportController : ControllerBase
    {
        private readonly ISupportService _supportService;
        private readonly ILogger<SupportController> _logger;
        public SupportController(ISupportService supportService, ILogger<SupportController> logger)
        {
            _supportService = supportService;
            _logger = logger;
        }

        [HttpPost("request")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateSupportRequest([FromBody] SupportRequestDto request)
        {
            _logger.LogInformation("Received support request: {Subject} from {ContactEmail}", request.Subject, request.ContactEmail);

            var response = await _supportService.CreateSupportRequestAsync(request);

            if (!response.Success)
            {
                _logger.LogError("Failed to create support request: {ErrorMessage}", response.ErrorMessage);
                return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
            }

            _logger.LogInformation("Support request created successfully: {Message}", response.Message);
            return ApiResponse.Success("", response.Message);
        }
    }
}
