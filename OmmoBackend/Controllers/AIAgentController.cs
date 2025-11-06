using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Helpers.Utilities;
using OmmoBackend.Services.Implementations;
using OmmoBackend.Services.Interfaces;
using Twilio.TwiML.Voice;

namespace OmmoBackend.Controllers
{
    [Route("api/aiagent")]
    [ApiController]
    public class AIAgentController : ControllerBase
    {
        private readonly ILogger<AIAgentController> _logger;
        //private readonly IAIAgentService _aiagentService;
        //private readonly ICallTranscriptService _transcriptService;
        private readonly ICallService _callservice;
        private readonly ICompanyService _companyService;
        private readonly IConfiguration _configuration;
        public AIAgentController(IConfiguration configuration, ILogger<AIAgentController> logger, ICompanyService companyService, ICallService callservice)
        {
            _logger = logger;
          //  _aiagentService = aiagentService;
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
                        // You can add <Parameter> tags if you want Twilio to send
                        // metadata to the media stream vendor (if supported).
                        // new XElement("Parameter", new XAttribute("name","companyId"), new XAttribute("value", companyId))
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
            try
            {

                if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                    return error;
                var userId = TokenHelper.GetUserIdFromClaims(User);


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

                var companyDial = new CompanyDialInfoDto(company.name, company.twillo_number);
                var load = new LoadInfo(
                    request.Mileage,
                    request.RateTotal,
                    request.LoadRpm,
                    request.Origin,
                    request.Destination,
                    request.FromDate,
                    request.ToDate
                );

                var client = new ClientInfo(
                    request.ClientPhone,
                    request.ClientEmail,
                    request.ClientCompany
                );

                // Make the call through service
                await _callservice.CallAsync(companyDial, load, client, agentId.Value,companyId);

                //Insertion into calls table so return would be 
                //user_id fetched through token above
                //broker_number request.ClientPhone
                //is_broker_already_registered (default false)
                //status_of_call (response from Call Function)
                //call_timestamp (time now)
                //load_id 0 by default
                //caller_id ? (UltravoxCallId: uvxCallId!, from call function(ultravox_call_id))
                //company_id fetched above
                //match_id
                //truckstop_id |
                //loadboard_type 
                //broker_company
                //Twillo.Sid ( response from call function)

                //add attribute Twillo.sid

                //Here i wanted to Trigger Something maybe Webhook . (Service)
                /*
                 * 
                 * 
                 * it will Fetch Transcript through Transcript Api (Continous approach untill Communication ends)
                 * https://api.ultravox.ai/api/calls/{ultravox_call_id}/messages
                 * here is reference of other Ultravox documentation https://docs.ultravox.ai/api-reference/introduction
                 * sample of transcript
                 * {
    "next": null,
    "previous": null,
    "total": 25,
    "results": [
        {
            "role": "MESSAGE_ROLE_AGENT",
            "text": "This call may be recorded for",
            "medium": "MESSAGE_MEDIUM_TEXT",
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "0.348s",
                "end": "2.185s"
            },
            "callStageMessageIndex": 0
        },
        {
            "role": "MESSAGE_ROLE_USER",
            "text": "Mail. The person you're trying to reach is not available. At the tone, please record your message. When you have finished recording, you may hang up.",
            "medium": "MESSAGE_MEDIUM_VOICE",
            "callStageMessageIndex": 1,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "0s",
                "end": "9.088s"
            }
        },
        {
            "role": "MESSAGE_ROLE_AGENT",
            "text": "I'm Ava from Ommotech, and I need your consent to discuss a potential freight booking, may I proceed?",
            "medium": "MESSAGE_MEDIUM_TEXT",
            "callStageMessageIndex": 2,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "9.765s",
                "end": "14.585s"
            }
        },
        {
            "role": "MESSAGE_ROLE_USER",
            "text": "Hello?",
            "medium": "MESSAGE_MEDIUM_VOICE",
            "callStageMessageIndex": 3,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "14.464s",
                "end": "16.416s"
            }
        },
        {
            "role": "MESSAGE_ROLE_AGENT",
            "text": "I'm Ava from Ommotech, we're a freight carrier negotiating loads on our behalf, may I proceed with discussing a potential booking?",
            "medium": "MESSAGE_MEDIUM_TEXT",
            "callStageMessageIndex": 4,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "17.305s",
                "end": "24.185s"
            }
        },
        {
            "role": "MESSAGE_ROLE_USER",
            "text": "Oh, social.",
            "medium": "MESSAGE_MEDIUM_VOICE",
            "callStageMessageIndex": 5,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "23.968s",
                "end": "26.272s"
            }
        },
        {
            "role": "MESSAGE_ROLE_AGENT",
            "text": "I'd like to confirm the load details: we're looking at a shipment from Phoenix, AZ to Dallas, TX, is that correct?",
            "medium": "MESSAGE_MEDIUM_TEXT",
            "callStageMessageIndex": 6,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "26.885s",
                "end": "33.305s"
            }
        },
        {
            "role": "MESSAGE_ROLE_USER",
            "text": "Yes. It's correct. But it's not actually Dallas, but it is more about Austin.",
            "medium": "MESSAGE_MEDIUM_VOICE",
            "callStageMessageIndex": 7,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "33.152s",
                "end": "40.128s"
            }
        },
        {
            "role": "MESSAGE_ROLE_AGENT",
            "text": "So the destination is actually Austin, TX, not Dallas, I'll make sure to update that, what are the pickup and dropoff windows for this shipment?",
            "medium": "MESSAGE_MEDIUM_TEXT",
            "callStageMessageIndex": 8,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "40.825s",
                "end": "48.065s"
            }
        },
        {
            "role": "MESSAGE_ROLE_USER",
            "text": "What was in the load detail?",
            "medium": "MESSAGE_MEDIUM_VOICE",
            "callStageMessageIndex": 9,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "48.032s",
                "end": "52.352s"
            }
        },
        {
            "role": "MESSAGE_ROLE_AGENT",
            "text": "The load details I had were: origin Phoenix, AZ, destination Dallas, TX, pickup on 2025-09-15, and dropoff on 2025-09-16, but you corrected the destination to Austin, TX.",
            "medium": "MESSAGE_MEDIUM_TEXT",
            "callStageMessageIndex": 10,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "53.325s",
                "end": "66.425s"
            }
        },
        {
            "role": "MESSAGE_ROLE_USER",
            "text": "Yeah. Correct. And the drop off is, like, before set in October.",
            "medium": "MESSAGE_MEDIUM_VOICE",
            "callStageMessageIndex": 11,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "65.664s",
                "end": "71.488s"
            }
        },
        {
            "role": "MESSAGE_ROLE_AGENT",
            "text": "So to confirm, the pickup window is still on September 15th, but the dropoff window is now before 6 PM on September 16th, is that correct?",
            "medium": "MESSAGE_MEDIUM_TEXT",
            "callStageMessageIndex": 12,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "72.405s",
                "end": "80.585s"
            }
        },
        {
            "role": "MESSAGE_ROLE_USER",
            "text": "No. No. The drop off is first October before first October.",
            "medium": "MESSAGE_MEDIUM_VOICE",
            "callStageMessageIndex": 13,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "80.288s",
                "end": "86.592s"
            }
        },
        {
            "role": "MESSAGE_ROLE_AGENT",
            "text": "So the pickup is on September 15th, and the dropoff is before October 1st, got it, and what's the offered rate per mile and total for this shipment?",
            "medium": "MESSAGE_MEDIUM_TEXT",
            "callStageMessageIndex": 14,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "87.305s",
                "end": "96.325s"
            }
        },
        {
            "role": "MESSAGE_ROLE_USER",
            "text": "What was in the load?",
            "medium": "MESSAGE_MEDIUM_VOICE",
            "callStageMessageIndex": 15,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "96.352s",
                "end": "98.688s"
            }
        },
        {
            "role": "MESSAGE_ROLE_AGENT",
            "text": "The load was 1200 miles, from Phoenix, AZ to Austin, TX, with a pickup on September 15th and a dropoff before October 1st, and the offered rate was $2.00 per mile, totaling $2400.00.",
            "medium": "MESSAGE_MEDIUM_TEXT",
            "callStageMessageIndex": 16,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "99.685s",
                "end": "111.605s"
            }
        },
        {
            "role": "MESSAGE_ROLE_USER",
            "text": "No. No. No. I can offer maybe 1.5.",
            "medium": "MESSAGE_MEDIUM_VOICE",
            "callStageMessageIndex": 17,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "112.288s",
                "end": "117.920s"
            }
        },
        {
            "role": "MESSAGE_ROLE_AGENT",
            "text": "You're offering $1.50 per mile, which would be a total of $1800.00 for the 1200 miles, I'll need to discuss this with my team, can I counteroffer or should I accept/reject this rate?",
            "medium": "MESSAGE_MEDIUM_TEXT",
            "callStageMessageIndex": 18,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "118.945s",
                "end": "129.405s"
            }
        },
        {
            "role": "MESSAGE_ROLE_USER",
            "text": "I cannot go about this. This is the final from me.",
            "medium": "MESSAGE_MEDIUM_VOICE",
            "callStageMessageIndex": 19,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "130.240s",
                "end": "134.976s"
            }
        },
        {
            "role": "MESSAGE_ROLE_AGENT",
            "text": "Since $1.50 is below our target rate, I'm afraid we'll have to decline the offer, thank you for your time and consideration, have a great day!",
            "medium": "MESSAGE_MEDIUM_TEXT",
            "callStageMessageIndex": 20,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "135.625s",
                "end": "142.865s"
            }
        },
        {
            "role": "MESSAGE_ROLE_AGENT",
            "text": "If it’s okay, I’ll give a quick overview.",
            "medium": "MESSAGE_MEDIUM_TEXT",
            "callStageMessageIndex": 21,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "147.085s",
                "end": "148.945s"
            }
        },
        {
            "role": "MESSAGE_ROLE_USER",
            "text": "No quick call on you. Thank you. Bye.",
            "medium": "MESSAGE_MEDIUM_VOICE",
            "callStageMessageIndex": 22,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "148.768s",
                "end": "152.256s"
            }
        },
        {
            "role": "MESSAGE_ROLE_AGENT",
            "text": "You're welcome, it was a pleasure speaking with you, goodbye!",
            "medium": "MESSAGE_MEDIUM_TEXT",
            "callStageMessageIndex": 23,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "153.285s",
                "end": "155.925s"
            }
        },
        {
            "role": "MESSAGE_ROLE_AGENT",
            "text": "If it’s okay, I’ll give a quick overview.",
            "medium": "MESSAGE_MEDIUM_TEXT",
            "callStageMessageIndex": 24,
            "callStageId": "5b99bb11-8dde-4e1b-9f46-58d5256e4d35",
            "timespan": {
                "start": "160.125s",
                "end": "161.885s"
            }
        }
    ]
}
                 so it will continous fetch this transcript and update table 
                    Column     |            Type             | Collation | Nullable |      Default
---------------+-----------------------------+-----------+----------+-------------------
 transcript_id | uuid                        |           | not null | gen_random_uuid()
 call_id       | uuid                        |           |          |
 speaker       | character varying(50)       |           |          |
 text          | text                        |           |          |
 timestamp     | timestamp without time zone |           |          | CURRENT_TIMESTAMP
                 */
                //it will Fetch and Update Table ( after confirming Data)
                /* ( call id we got after successfull insertion to call table above . so we will pass it to webhook/service)
                 * next it will update for same call id all these tables below ( can fetch this info through transcript) 
 call_confirm_data
                            Table "public.call_confirm_data"
    Column     |            Type             | Collation | Nullable |      Default
---------------+-----------------------------+-----------+----------+-------------------
 c_c_d_id      | uuid                        |           | not null | gen_random_uuid()
 call_id       | uuid                        |           |          |
 pickup_time   | timestamp without time zone |           |          |
 delivery_time | timestamp without time zone |           |          |
 trip_mile     | numeric(10,2)               |           |          |
 rate_per_mile | numeric(10,2)               |           |          |
 final_rate    | numeric(10,2)               |           |          |
 origin        | character varying(255)      |           |          |
 destination   | character varying(255)      |           |          |
                                         Table "public.call_sentiment"
  Column   |         Type          | Collation | Nullable |      Default
-----------+-----------------------+-----------+----------+-------------------
 c_s_id    | uuid                  |           | not null | gen_random_uuid()
 call_id   | uuid                  |           |          |
 sentiment | character varying(50) |           |          |
Indexes:
                                        Table "public.call_summary_bullets"
  Column   |            Type             | Collation | Nullable |      Default
-----------+-----------------------------+-----------+----------+-------------------
 bullet_id | uuid                        |           | not null | gen_random_uuid()
 call_id   | uuid                        |           |          |
 timestamp | timestamp without time zone |           |          | CURRENT_TIMESTAMP
 text      | text                        |           |          |

                (this would be continous proccess untill call ends )
      //finally we can use same webhook to end call ( as done above)
                // finally it should update status of call as well for example if we keep 2 status in_proccess end
                intially when call intiated status will be in_process as end it updates end
                 */

                return Ok(new
                {
                    message = "Call initiated successfully",
                    data = Array.Empty<object>() // always []
                });
            }
            catch (Exception ex)
            {
                // Log exception here if needed (Serilog/NLog/etc.)
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
    }
}
