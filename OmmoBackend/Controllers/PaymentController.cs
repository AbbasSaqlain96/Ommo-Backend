using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Handlers;
using OmmoBackend.Services.Interfaces;
using Serilog.Core;
using Stripe;

namespace OmmoBackend.Controllers
{
    [Route("api/payment")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IEnumerable<IStripeEventHandler> _handlers;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IEnumerable<IStripeEventHandler> handlers, IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _handlers = handlers;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("webhook")]
        public async Task<IActionResult> Handle()
        {
            var json = await new StreamReader(Request.Body).ReadToEndAsync();

            var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrWhiteSpace(stripeSignature))
                return BadRequest("Missing Stripe signature");

            if (_handlers == null)
                throw new InvalidOperationException("Stripe handlers not injected");

            var stripeEvent = EventUtility.ConstructEvent(
                json,
                stripeSignature,
                _configuration["Stripe:WebhookSecret"]);

            if (stripeEvent == null)
                return BadRequest("Invalid Stripe event");

            var handler = _handlers
                .FirstOrDefault(x =>
                    x != null &&
                    x.EventType == stripeEvent.Type);

            if (handler == null)
            {
                return Ok(); // ignore unsupported event
            }

            await handler.HandleAsync(stripeEvent);

            return Ok();
        }
    }
}