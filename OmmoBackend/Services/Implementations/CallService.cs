using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;
using System.Text.Json;
using Twilio;
using Twilio.TwiML;
using Twilio.TwiML.Voice;
using Twilio.Types;


namespace OmmoBackend.Services.Implementations
{
    public class CallService : ICallService
    {

        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CallService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAIAgentRepository _aiAgentRepo;
        private readonly ICallRepository _callRepository;

        public CallService( IWebHostEnvironment environment,
            IConfiguration configuration, ILogger<CallService> logger, IHttpClientFactory httpClientFactory, IAIAgentRepository aiAgentRepo, ICallRepository callRepository)
        {
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _aiAgentRepo = aiAgentRepo;
            _callRepository = callRepository;
            //wilioClient.Init(_configuration["Twilio:AccountSid"], _configuration["Twilio:AuthToken"]);

        }


        /*

        public async Task<OutboundCallResult> CallAsync(
            CompanyDialInfoDto company,
            LoadInfo load,
            ClientInfo client,
            Guid agentId,
            int companyId)
        {
            var settings = await _aiAgentRepo.GetAgentSettingsAsync(agentId);
            if (settings == null)
            {
                _logger.LogWarning("No AgentSettings found for AgentGuid {AgentGuid}. Using fallbacks.", agentId);
            }



            // 2) Derive dynamic bits
            var agentName = settings?.AgentName ?? "Agent";
            var whoWeAre = settings?.WhoWeAre ?? "We provide reliable, on-time freight coverage.";
            var consentOn = settings?.ConsentMode ?? false;
            var offeredRpm = load.LoadRpm;
            var floorRpm = settings?.FloorRpm ?? 1.35m;
            var targetRpm = settings?.TargetRpm ?? 1.65m;
            var walkawayRpm = settings?.WalkawayRpm ?? 1.50m;

            // Voice mapping (your provided codes)
            var voiceCode = (settings?.VoiceGender?.Trim().ToLower()) switch
            {
                "female" => "1769b283-36c6-4883-9c52-17bf75a29bc5",
                "male" => "feccf00b-417e-4e7a-9f89-62f537280334",
                _ => "1769b283-36c6-4883-9c52-17bf75a29bc5" // default female if unknown
            };

            var consentLine = consentOn
            ? "This call may be recorded for quality and booking verification. Do I have your consent to proceed? "
            : string.Empty;

            // Intro + who we are
            var introLine =$"Hello, I’m {agentName} calling from {company.name}. {whoWeAre} ";

            // Confirm load basics
            var detailsLine =
                $"I saw your load from {load.Origin} to {load.Destination}, " +
                $"pickup around {load.FromDate:MMM d, h:mm tt} and delivery around {load.ToDate:MMM d, h:mm tt}. ";

            // Rate confirmation
            var rateLine =
                $"You’re showing ~${offeredRpm:F2} per mile, total ${load.RateTotal:F2} on {load.Mileage} miles. ";

            // Combine
            var openingLine = $"{consentLine}{introLine}{detailsLine}{rateLine}";

            var systemPrompt =
            $@"You are an AI calling agent for B2B freight bookings. Follow this policy strictly:

            1) If consent_mode is true, FIRST speak the consent line and wait for yes/no. If no/unsure → politely end the call.
            2) Confirm origin, destination, pickup and dropoff windows concisely.
            3) Confirm broker's offered RPM and total rate. Use the computed 'offeredRpm'.
            4) Negotiation rules (use USD):
               - If offeredRpm ≥ target_rpm → accept immediately (no further negotiation) and politely proceed to booking steps.
               - If walkaway_rpm ≤ offeredRpm < target_rpm → negotiate toward target_rpm; if broker won’t reach target, accept a reasonable midpoint ≥ walkaway_rpm.
               - If offeredRpm < walkaway_rpm → attempt to bring it above walkaway_rpm; if broker refuses, politely decline and end.
            5) If the broker says they're busy or asks to call later, politely end the call without dragging.
            6) Keep responses concise, professional, and human-like; avoid long monologues. Pause for broker responses.
            7) NEVER commit below floor_rpm. NEVER argue. Be courteous.

            You can use:
            - agent_name = '{agentName}'
            - company_name = '{company.name}'
            - who_we_are = '{whoWeAre}'
            - consent_mode = {(consentOn ? "true" : "false")}
            - floor_rpm = {floorRpm:F2}, walkaway_rpm = {walkawayRpm:F2}, target_rpm = {targetRpm:F2}
            - offered_rpm = {offeredRpm:F2}, total = {load.RateTotal:F2}, miles = {load.Mileage}
            - lane = '{load.Origin}' → '{load.Destination}', pickup = '{load.FromDate:O}', dropoff = '{load.ToDate:O}'
            - agent_id (Ultravox) = '{agentId}'
            Use these values for reasoning and concise responses.";

            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Add("X-API-Key", _configuration["Ultravox:ApiKey"]);
            var compactMetadata = new
            {
                company = new { name = company.name },
                load = new
                {
                    load.Mileage,
                    load.RateTotal,
                    load.LoadRpm,
                    load.Origin,
                    load.Destination,
                    FromDate = load.FromDate.ToString("O"),
                    ToDate = load.ToDate.ToString("O"),
                    OfferedRpm = offeredRpm
                },
                client = new
                {
                    client.ClientPhone,
                    client.ClientEmail,
                    client.ClientCompany
                },
                settings = new
                {
                    agentName,
                    whoWeAre,
                    consentMode = consentOn,
                    floorRpm,
                    walkawayRpm,
                    targetRpm,
                    voiceGender = settings?.VoiceGender
                },
                agentId = agentId.ToString()
            };
            var payload = new
            {
                systemPrompt,
                voice = voiceCode,
                firstSpeakerSettings = new
                {
                    agent = new { text = openingLine, delay = "0s" }
                },
                medium = new { twilio = new { } },
                recordingEnabled = consentOn,
                metadata = new Dictionary<string, string>
                {
                    ["ctx"] = JsonSerializer.Serialize(compactMetadata)
                }
            };
            var uvxResp = await http.PostAsJsonAsync("https://api.ultravox.ai/api/calls", payload);
            var uvxContent = await uvxResp.Content.ReadAsStringAsync();
            if (!uvxResp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Ultravox error {(int)uvxResp.StatusCode}: {uvxContent}");


            // Parse Ultravox response
            using var json = JsonDocument.Parse(uvxContent);
            var uvxCallId = json.RootElement.GetProperty("callId").GetString();
            var joinUrl = json.RootElement.GetProperty("joinUrl").GetString();

            if (string.IsNullOrWhiteSpace(joinUrl))
                throw new InvalidOperationException("Ultravox did not return a joinUrl.");

            // Twilio: To = broker, From = company twilio
            var toNumber = new Twilio.Types.PhoneNumber(client.ClientPhone);
            var fromNumber = new Twilio.Types.PhoneNumber(company.twillo_number!);

            // TwiML URL (multi-tenant safe endpoint you built)
            var baseUrl = _configuration["App:PublicBaseUrl"];
            var twimlUrl = $"{baseUrl}/api/aiagent/twiml/{companyId}?joinUrl={Uri.EscapeDataString(joinUrl)}";
            var vr = new VoiceResponse();
            var connect = new Connect();
            connect.Stream(url: joinUrl);  // joinUrl from Ultravox
            vr.Append(connect);
            // IMPORTANT: use async
            var twilioCall = await Twilio.Rest.Api.V2010.Account.CallResource.CreateAsync(
                to: toNumber,
                from: fromNumber,
                twiml: new Twiml(vr.ToString())
            );
            return new OutboundCallResult(
                UltravoxCallId: uvxCallId!,
                TwilioCallSid: twilioCall.Sid,
                Status: "Status"
            ); 

            //Testing Call on basis of Payload (Next thing)

        }*/
        public async Task<OutboundCallResult> CallAsync(
            CompanyDialInfoDto company,
            LoadInfo load,
            ClientInfo client,
            Guid agentId,
            int companyId)
        {
            // 1) Fetch agent settings
            var settings = await _aiAgentRepo.GetAgentSettingsAsync(agentId);
            if (settings == null)
                _logger.LogWarning("No AgentSettings found for AgentGuid {AgentGuid}. Using fallbacks.", agentId);

            // 2) Derive dynamic bits
            var agentName = settings?.AgentName ?? "Agent";
            var whoWeAre = settings?.WhoWeAre ?? "We provide reliable, on-time freight coverage.";
            var consentOn = settings?.ConsentMode ?? false;
            var offeredRpm = load.LoadRpm;

            var floorRpm = settings?.FloorRpm ?? 1.35m;
            var targetRpm = settings?.TargetRpm ?? 1.65m;
            var walkawayRpm = settings?.WalkawayRpm ?? 1.50m;

            var voiceCode = (settings?.VoiceGender?.Trim().ToLower()) switch
            {
                "female" => "1769b283-36c6-4883-9c52-17bf75a29bc5",
                "male" => "feccf00b-417e-4e7a-9f89-62f537280334",
                _ => "1769b283-36c6-4883-9c52-17bf75a29bc5"
            };

            // First utterance: only consent OR short greeting (no lane/rate here)
            var firstUtterance = consentOn
                ? "This call may be recorded for quality and booking verification. Do I have your consent to proceed?"
                : $"Hello, I’m {agentName} calling from {company.name}. Is this a good time to talk about a load?";

            // 3) System prompt (turn-taking + secrecy + broadened consent synonyms)
            var systemPrompt =
        $@"You are an AI calling agent for B2B freight bookings. Speak naturally, briefly, and one idea at a time.

Turn-taking:
- After you speak, STOP and wait for the broker.
- Keep turns to 1–2 short sentences. Never monologue.

Consent:
- If consent_mode is true, say only the consent line first and WAIT for yes/no.
- Treat “yes”, “yeah”, “yep”, “sure”, “go ahead”, “okay/ok” and similar as consent; on consent, continue immediately.
- Treat “no”, “not now”, “busy” as no-consent; end politely.

Info sequence (each as a separate short turn, waiting after each):
1) Confirm origin → destination.
2) Confirm pickup and dropoff windows.
3) Confirm offered RPM and total (use offered_rpm and total).

Negotiation (internal thresholds; NEVER say them aloud):
- If offered_rpm ≥ target_rpm → accept and proceed to booking.
- If walkaway_rpm ≤ offered_rpm < target_rpm → counter toward target; if needed accept a fair midpoint ≥ walkaway_rpm.
- If offered_rpm < walkaway_rpm → attempt to raise above walkaway; if not possible, politely decline.

Other:
- If broker is busy / asks to call later → end quickly.
- Be courteous; amounts in USD only.

Context:
- agent_name = '{agentName}'
- company_name = '{company.name}'
- who_we_are = '{whoWeAre}'
- consent_mode = {(consentOn ? "true" : "false")}
- floor_rpm = {floorRpm:F2} (internal)
- walkaway_rpm = {walkawayRpm:F2} (internal)
- target_rpm = {targetRpm:F2} (internal)
- offered_rpm = {offeredRpm:F2}, total = {load.RateTotal:F2}, miles = {load.Mileage}
- lane = '{load.Origin}' → '{load.Destination}', pickup = '{load.FromDate:O}', dropoff = '{load.ToDate:O}'
- agent_id = '{agentId}'";

            // 4) Compact metadata (for logging/analytics)
            var compactMetadata = new
            {
                company = new { name = company.name },
                load = new
                {
                    load.Mileage,
                    load.RateTotal,
                    load.LoadRpm,
                    load.Origin,
                    load.Destination,
                    FromDate = load.FromDate.ToString("O"),
                    ToDate = load.ToDate.ToString("O"),
                    OfferedRpm = offeredRpm
                },
                client = new
                {
                    client.ClientPhone,
                    client.ClientEmail,
                    client.ClientCompany
                },
                settings = new
                {
                    agentName,
                    whoWeAre,
                    consentMode = consentOn,
                    floorRpm,
                    walkawayRpm,
                    targetRpm,
                    voiceGender = settings?.VoiceGender
                },
                agentId = agentId.ToString()
            };

            // 5) Ultravox request
            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Remove("X-API-Key");
            http.DefaultRequestHeaders.Add("X-API-Key", _configuration["Ultravox:ApiKey"]);

            var payload = new
            {
                systemPrompt,
                voice = voiceCode,

                // Start with agent; allow barge-in
                firstSpeaker = "FIRST_SPEAKER_AGENT",
                firstSpeakerSettings = new
                {
                    agent = new
                    {
                        text = firstUtterance,
                        delay = "0s"
                        // no "uninterruptible": early "yes" will be heard
                    }
                },

                // Loosened VAD to catch short “yes/yeah”
                vadSettings = new
                {
                    turnEndpointDelay = "0.25s",
                    minimumTurnDuration = "0.18s",
                    minimumInterruptionDuration = "0.15s",
                    frameActivationThreshold = 0.35
                },

                // Gentle nudge then graceful exit (no endBehavior at top level)
                inactivityMessages = new object[]
                {
            new { duration = "4s", message = "If it’s okay, I’ll give a quick overview." },
            new { duration = "9s", message = "I’ll let you go for now—thanks for your time." }
                },

                // Ensure ASR configuration fits the audience
                languageHint = "en-US",

                // Session bounds (Ultravox requires 's' suffix)
                joinTimeout = "30s",
                maxDuration = "600s",
                timeExceededMessage = "I have to hop, but feel free to call me back. Bye for now.",

                // Twilio streaming leg
                medium = new { twilio = new { } },

                recordingEnabled = consentOn,
                transcriptOptional = false,              // force transcripts so consent is captured
                initialOutputMedium = "MESSAGE_MEDIUM_VOICE",

                metadata = new Dictionary<string, string>
                {
                    ["ctx"] = JsonSerializer.Serialize(compactMetadata)
                }
            };

            var uvxResp = await http.PostAsJsonAsync("https://api.ultravox.ai/api/calls", payload);
            var uvxContent = await uvxResp.Content.ReadAsStringAsync();
            if (!uvxResp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Ultravox error {(int)uvxResp.StatusCode}: {uvxContent}");

            // 6) Parse Ultravox response
            using var json = JsonDocument.Parse(uvxContent);
            var uvxCallId = json.RootElement.GetProperty("callId").GetString();
            var joinUrl = json.RootElement.GetProperty("joinUrl").GetString();

            if (string.IsNullOrWhiteSpace(joinUrl))
                throw new InvalidOperationException("Ultravox did not return a joinUrl.");

            // 7) Twilio call
            if (string.IsNullOrWhiteSpace(client.ClientPhone))
                throw new ArgumentException("Client phone number is required.", nameof(client.ClientPhone));
            if (string.IsNullOrWhiteSpace(company.twillo_number))
                throw new ArgumentException("Company Twilio number is required.", nameof(company.twillo_number));

            var toNumber = new Twilio.Types.PhoneNumber(client.ClientPhone);
            var fromNumber = new Twilio.Types.PhoneNumber(company.twillo_number!);

            var vr = new VoiceResponse();
            var connect = new Connect();
            connect.Stream(url: joinUrl);
            vr.Append(connect);

            var twilioCall = await Twilio.Rest.Api.V2010.Account.CallResource.CreateAsync(
                to: toNumber,
                from: fromNumber,
                twiml: new Twiml(vr.ToString())
            );

            return new OutboundCallResult(
                UltravoxCallId: uvxCallId!,
                TwilioCallSid: twilioCall.Sid,
                Status: twilioCall.Status?.ToString() ?? "initiated"
            );
        }

        public async Task<Guid?> FetchAgentIdAsync(int companyId)
        {
            return await _aiAgentRepo.GetAgentGuidByCompanyIdAsync(companyId);
        }

        public async Task<ServiceResponse<List<CalledLoadDto>>> GetCalledLoadsAsync(int companyId)
        {
            try
            {
                var loads = await _callRepository.GetCalledLoadsAsync(companyId);

                if (loads == null || !loads.Any())
                {
                    return ServiceResponse<List<CalledLoadDto>>.SuccessResponse(new List<CalledLoadDto>(), "No called loads found in last 24 hours.");
                }

                return ServiceResponse<List<CalledLoadDto>>.SuccessResponse(loads, "Called loads fetched successfully.");
            }
            catch (Exception)
            {
                return ServiceResponse<List<CalledLoadDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    }
}
