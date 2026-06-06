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
using Org.BouncyCastle.Asn1.Ocsp;
using Twilio.TwiML.Voice;

namespace OmmoBackend.Controllers
{
    [Route("api/aiagent")]
    [ApiController]
    public class AIAgentController : ControllerBase
    {
        private readonly ILogger<AIAgentController> _logger;
        private readonly IAIAgentService _aiagentService;
        //private readonly ICallTranscriptService _transcriptService;
        private readonly ICallService _callservice;
        private readonly ICompanyService _companyService;
        private readonly IConfiguration _configuration;
        public AIAgentController(IConfiguration configuration, ILogger<AIAgentController> logger, IAIAgentService aiagentService, ICompanyService companyService, ICallService callservice)
        {
            _logger = logger;
            _aiagentService = aiagentService;
          //  _transcriptService = transcriptService;
            _companyService = companyService;
            _configuration = configuration;
            _callservice = callservice;
        }

        /*    [HttpPost("register-agent")]
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



            [HttpPost("twiml")]
            public IActionResult TwiML([FromQuery] string joinUrl)
            {
                var twiml = $@"
                    <Response>
                      <Connect>
                        <Stream url=""{joinUrl}"" />
                      </Connect>
                    </Response>";
                return Content(twiml, "text/xml");
            }
       */   
        [HttpGet("takeover-twiml")]
        [AllowAnonymous]
        public IActionResult TakeoverTwiML(string number)
        {
            var response = new XDocument(
                new XElement("Response",
                    new XElement("Say", "Please hold while I connect you to my Manager."),
                    new XElement("Dial",
                        new XElement("Number", number) // ✅ STATIC NUMBER
                    )
                )
            );

            Response.Headers["Cache-Control"] = "no-store";

            return Content(
                response.ToString(SaveOptions.DisableFormatting),
                "text/xml"
            );
        }

        [HttpPost("twiml/{companyId:int}")]
        [HttpGet("twiml/{companyId:int}")]
        public IActionResult TwiML(int companyId, [FromQuery] string joinUrl)
        {
            // 1) Basic validation
            if (string.IsNullOrWhiteSpace(joinUrl))
                return BadRequest("Missing joinUrl.");

            if (!Uri.TryCreate(joinUrl, UriKind.Absolute, out var uri))
                return BadRequest("Invalid joinUrl.");

            if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return BadRequest("joinUrl must be https.");

            // 2) Host allow-list (configure in appsettings: Ultravox:AllowedHosts: [ "api.ultravox.ai" ])
            var allowedHosts = _configuration.GetSection("Ultravox:AllowedHosts").Get<string[]>()
                               ?? new[] { "api.ultravox.ai" };
            if (!allowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
                return BadRequest("joinUrl host not allowed.");

            // 3) Optional: authorize tenant/company here
            //    e.g., compare companyId from route vs. companyId in JWT

            // 4) Build TwiML safely (no string concat)
            var response = new XDocument(
                new XElement("Response",
                    new XElement("Connect",
                        new XElement("Stream",
                            new XAttribute("url", uri.ToString())
                        )
                    )
                )
            );

            Response.Headers["Cache-Control"] = "no-store";
            return Content(response.ToString(SaveOptions.DisableFormatting), "text/xml");
        }


        [HttpPost("outbound")]
        [Authorize]
        //[AllowAnonymous]
        public async Task<IActionResult> OutboundCall([FromBody] OutboundCallRequest request)
        {
            Guid callId = Guid.Empty;
            OutboundCallResult? callresult = null;
            try
            {

                if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                    return error;
                var userId = TokenHelper.GetUserIdFromClaims(User);
                if (string.IsNullOrWhiteSpace(request.ClientPhone))
                {
                    return BadRequest(new
                    {
                        message = "Cannot make a call Client Phone is not available",
                        data = Array.Empty<object>()
                    });
                }

                string clientnum;
                if(userId==312)
                {
                    clientnum = "+923212694374";
                }
                 else if (userId == 370)
                {
                  clientnum = "+14803405340";
                }
                else if (userId == 375)
                {
                    clientnum = "+923212694374"; // replace this number with your number
                        //This Account belong to Saqlain 
                }
                else if(userId == 376)
                {
                    clientnum = request.ClientPhone;
                }

                else
                {
                    return BadRequest(new
                    {
                        message = "This User is not allowed for this operation.",
                        data = Array.Empty<object>()
                    });
                }


                var company = await _companyService.GetCompanyDialInfoAsync(companyId);
                if (company is null)
                    return NotFound(new { message = "Company not found.", data = Array.Empty<object>() });

                if (string.IsNullOrWhiteSpace(company.twillo_number))
                    return BadRequest(new { message = "Twilio number is required. Please get it updated in Company Profile.", data = Array.Empty<object>() });

                var agentId = await _callservice.FetchAgentIdAsync(companyId);
                if (agentId is null)
                {
                    return BadRequest(new
                    {
                        message = "No AI agent found for this company.",
                        data = Array.Empty<object>()
                    });
                }
                // Add MC in Company Dial
                var companyDial = new CompanyDialInfoDto(company.name, company.twillo_number, company.mc_number);
                var load = new LoadInfo(
                    request.Mileage,
                    request.RateTotal,
                    request.LoadRpm,
                    request.Origin,
                    request.Destination,
                    request.Reference_ID,
                    request.FromDate,
                    request.ToDate,
                    request.wieght,
                    request.length,
                    request.Commodity,
                    request.Equipment_Type
                );

                var client = new ClientInfo(
                    clientnum,
                    request.ClientEmail,
                    request.ClientCompany
                );

                var call = new Call
                {
                    user_id = userId,
                    company_id = companyId,
                    broker_number = request.ClientPhone,
                    broker_company = request.ClientCompany ?? string.Empty,
                    is_broker_already_registered = false,
                    status_of_call = "initiated",
                    call_result = "none",
                    call_timestamp = DateTime.UtcNow,
                    load_id = 0,
                    caller_id = null,
                    twilio_call_sid = null,
                    reference_id = request.Reference_ID,
                    loadboard_type = request.LoadboardType
                };

                callId = await _callservice.LogCallAsync(call);


                callresult = await _callservice.CallAsync(companyDial, load, client, agentId.Value, companyId, callId, userId);
                await _callservice.UpdateCallAfterDialAsync(callId, callresult);

                return Ok(new
                {
                    message = "Call initiated successfully",
                    data = new
                    {
                        call_id = callId
                    }
                });
            }
            catch (Exception ex)
            {

                // Ensure call state is updated even on failure
                callresult ??= new OutboundCallResult(
                    UltravoxCallId: null,
                    TwilioCallSid: null,
                    Status: "failed"
                );

                await _callservice.UpdateCallAfterDialAsync(callId, callresult);
                return StatusCode(500, new
                {
                    message = "An error occurred while initiating the call",
                    data = Array.Empty<object>(),
                    error = ex.Message
                });
            }
        }




        /*    [HttpGet("getTranscript/{callId}")]
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
            }*/


        [HttpGet("settings")]
        [Authorize]
        public async Task<IActionResult> GetAgentSettings()
        {
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            var result = await _aiagentService.GetAgentSettingsAsync(companyId);

            if (!result.Success)
                return ApiResponse.Error(result.ErrorMessage, result.StatusCode);

            return ApiResponse.Success(result.Data, result.Message);
        }

        [HttpPut("update-settings")]
        [Authorize]
        public async Task<IActionResult> UpdateAgentSettings([FromBody] UpdateAgentSettingsRequest request)
        {
            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            var result = await _aiagentService.UpdateAgentSettingsAsync(request, companyId);

            if (!result.Success)
                return ApiResponse.Error(result.ErrorMessage, result.StatusCode);

            return ApiResponse.Success(null, result.Message);
        }
    }
}
