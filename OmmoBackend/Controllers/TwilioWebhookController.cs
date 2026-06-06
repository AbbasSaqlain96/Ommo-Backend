using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Services.Interfaces;
using System.ComponentModel.Design;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Models;
using OmmoBackend.Services.Implementations;
using OmmoBackend.Services.Interfaces;
using Twilio.TwiML.Voice;
using Microsoft.AspNetCore.SignalR;
using OmmoBackend.Hubs;

namespace OmmoBackend.Controllers
{

    [ApiController]
    [Route("api/webhooks/twilio")]
    public class TwilioWebhookController : ControllerBase
    {
        private readonly ICallService _callService;
        private readonly ILogger<TwilioWebhookController> _logger;
        private readonly IHubContext<UserChatHub> _hubContext;

        public TwilioWebhookController(ICallService callService, ILogger<TwilioWebhookController> logger, IHubContext<UserChatHub> hubContext)
        {
            _callService = callService;
            _logger = logger;
            _hubContext = hubContext;
        }

        [HttpPost("status")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateCallStatus([FromForm] TwilioStatusCallbackRequest request)
        {
            try
            {
                _logger.LogInformation("Twilio webhook received: {@req}", request);

                await _callService.UpdateTwilioCallStatusAsync(request);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Twilio webhook processing failed");
                return StatusCode(500);
            }
        }


    }
}

