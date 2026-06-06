using Microsoft.AspNetCore.SignalR;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Hubs;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Implementations;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;
using System.Text.Json;
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
        private readonly IHubContext<CallTranscriptHub> _hubContext;
        private readonly ICompanyPaymentProfileRepository _companyPaymentProfileRepository;
        private readonly IPackagePlanRepository _packagePlanRepository;
        private readonly IBillingRepository _billingRepository;

        public CallService(IWebHostEnvironment environment,
            IConfiguration configuration, ILogger<CallService> logger, IHttpClientFactory httpClientFactory, IAIAgentRepository aiAgentRepo, ICallRepository callRepository, IHubContext<CallTranscriptHub> hubContext, ICompanyPaymentProfileRepository companyPaymentProfileRepository, IPackagePlanRepository packagePlanRepository, IBillingRepository billingRepository)
        {
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _aiAgentRepo = aiAgentRepo;
            _callRepository = callRepository;
            _hubContext = hubContext;
            _companyPaymentProfileRepository = companyPaymentProfileRepository;
            _packagePlanRepository = packagePlanRepository;
            _billingRepository = billingRepository;
        }


        public async Task<Guid> LogCallAsync(Call call, CancellationToken ct = default)
        {
            // Insert using repository
            var id = await _callRepository.InsertAsync(call, ct);
            return id;
        }



        public async System.Threading.Tasks.Task UpdateTwilioCallStatusAsync(TwilioStatusCallbackRequest request)
        {
            string status = request.CallStatus.ToLower();
            string statusOfCall;
            string callResult;

            switch (status)
            {
                case "queued":
                case "ringing":
                    statusOfCall = "ringing";
                    callResult = null;
                    break;

                case "in-progress":
                    statusOfCall = "live";
                    callResult = null;
                    break;

                case "completed":
                    statusOfCall = "ended";
                    callResult = null;
                    break;

                case "busy":
                case "no-answer":
                    statusOfCall = "no-answer";
                    callResult = "no-answer";
                    break;

                case "canceled":
                case "failed":
                    statusOfCall = "failed";
                    callResult = "call-failed";
                    break;

                default:
                    statusOfCall = "failed";
                    callResult = null;
                    break;
            }

            await _callRepository.UpdateStatusByTwilioSidAsync(
                request.CallSid,
                statusOfCall,
                callResult
            );
            var callId = await _callRepository.GetCallIdByTwilioSidAsync(request.CallSid);
            if (callId == null)
                return; // safety exit

            // 🔹 Broadcast via SignalR
            await _hubContext.Clients
                .Group(callId.ToString())
                .SendAsync("CallStatusUpdated", new
                {
                    callId = callId,
                    statusOfCall = statusOfCall,
                    callResult = callResult
                });
        }

        public async System.Threading.Tasks.Task UpdateCallAfterDialAsync(
        Guid callId,
        OutboundCallResult callResult)
        {
            if (callResult == null)
                throw new ArgumentNullException(nameof(callResult));

            await _callRepository.UpdateAfterDialAsync(
                callId,
                callResult
            );
        }



        public async Task<OutboundCallResult> CallAsync(
            CompanyDialInfoDto company,
            LoadInfo load,
            ClientInfo client,
            Guid agentId,
            int companyId,
            Guid call_id,
            int userid)
        {
            var concurrencyCheck = await ValidateConcurrencyLimitAsync(companyId);

            if (!concurrencyCheck.Success)
                throw new InvalidOperationException(concurrencyCheck.ErrorMessage);

            // ============================
            // Minute Limit Check
            // ============================
            var limitCheck = await CheckMinuteLimitAsync(companyId);

            if (!limitCheck.Success)
                throw new InvalidOperationException(limitCheck.ErrorMessage);

            // 1) Fetch agent settings
            var settings = await _aiAgentRepo.GetAgentSettingsAsync(agentId);
            if (settings == null)
                _logger.LogWarning("No AgentSettings found for AgentGuid {AgentGuid}. Using fallbacks.", agentId);

            // 2) Derive dynamic bits
            var agentName = settings?.AgentName ?? "Agent";
            var whoWeAre = settings?.WhoWeAre ?? "We provide reliable, on-time freight coverage.";
            var consentOn = settings?.ConsentMode ?? false;
            var offeredRpm = load.LoadRpm;
            var orgin = NormalizeLocation(load.Origin);
            var destination = NormalizeLocation(load.Destination);


            var floorRpm = settings?.FloorRpm ?? 1.35m;
            var targetRpm = settings?.TargetRpm ?? 1.65m;
            var walkawayRpm = settings?.WalkawayRpm ?? 1.50m;


            var weight = load.wieght;
            var length = load.length;
            var equipment = load.equipment_type;
            var commodity = load.commodity;

            var weightText = weight > 0 ? $"{weight} lbs" : "unknown weight";
            var lengthText = length > 0 ? $"{length} ft" : "unknown length";
            var equipmentText = string.IsNullOrWhiteSpace(equipment) ? "unspecified equipment" : equipment;
            var commodityText = string.IsNullOrWhiteSpace(commodity) ? "general freight" : commodity;

            var voiceCode = (settings?.VoiceGender?.Trim().ToLower()) switch
            {
                "female" => "1769b283-36c6-4883-9c52-17bf75a29bc5",
                "male" => "feccf00b-417e-4e7a-9f89-62f537280334",
                _ => "1769b283-36c6-4883-9c52-17bf75a29bc5"
            };

            var firstUtterance = consentOn
                ? "This call may be recorded for quality assurance"
                : $"Hi, I’m calling regarding a load from {orgin} to {destination}. Is this one still available?";


            var systemPrompt =
                    $@"You are an AI calling agent for B2B freight bookings.
                Speak naturally, briefly, and one idea at a time.

                ================================================
                TURN-TAKING RULES (VERY STRICT)
                ================================================

                Speak 1–2 short sentences per turn.
                After every response, STOP and wait for the broker.
                Never interrupt or talk over the broker.
                Be a good listener, not an abrupt speaker.
                Do NOT ask questions while the broker is speaking.
                Only respond after the broker finishes completely.

                ================================================
                INTRODUCTION
                ================================================

                After consent is granted (or immediately if consent_mode = false):

                Turn 1:
                “Hi, I’m calling regarding a load from {orgin} to {destination}. Is this one still available?”
                WAIT for broker response (DO NOT SPEAK, DO NOT HANG UP)

                ================================================
                AVAILABILITY CHECK
                ================================================
                ❌ If broker explicitly says NOT AVAILABLE

                Examples:

                “It’s already booked”

                “Not available”

                “Covered already”

                “It’s gone”

                Then (single turn):

                “No worries, thank you for your time. Have a great day.”

                ✅ Invoke HangUp AFTER speaking

                🚫 Do NOT ask anything else
                🚫 Do NOT wait further
                ✅ If broker says AVAILABLE

                Examples:

                “Yes”

                “Yeah it is”

                “Still open”

                “Available”

                Then (ONE sentence only):

                “Great, may I have your name please?”

                ⬇️ WAIT for response
                🚫 Do NOT hang up
                🚫 Do NOT continue until name is received
                ===============================================
                NAME CONFIRMATION RULE
                ===============================================

                When the broker provides their name:

                - Do NOT repeat it unless it sounds clearly uncertain
                - If unclear or unusual, confirm ONCE:

                “Sorry, did I catch your name correctly as {{Name}}?”

                If confirmed → proceed
                If not → store corrected name
                Never ask again

                ===============================================
                CONTEXT RE-ANCHOR (MANDATORY)
                ===============================================

                After the broker provides their name, say ONE sentence:

                “Thanks {{Name}} — I’m calling regarding that load posting, just need to confirm a couple details.”

                Then STOP and wait.

                Do NOT ask checklist questions until this confirmation is complete.

                ================================================
                INFORMATION SHARING RULES
                ================================================
                ❗ VERY IMPORTANT

                DO NOT disclose or share any information unless the broker asks
                Only answer exactly what is asked
                Do not volunteer extra details
                ✅ Sharable Information (ONLY IF ASKED)

                Company MC Number

                Reference ID

                Pickup Origin

                Drop-off Location

                Company Name

                Mileage

                Total Rate

                RPM (LoadRpm)

                ================================================
                HANDLING BROKER QUESTIONS
                ================================================

                If broker asks MC number or Reference → Answer directly
                If broker asks about load or company info → Answer clearly
                Do NOT repeat or expand beyond the asked question
                ================================================
                EXPECTED INFO FROM BROKER
                ================================================

                The broker may voluntarily provide load details.
                When the broker is speaking, you MUST remain silent and listen carefully.

                Expected information (may be given in any order):
                - Origin and destination (complete city and state)
                - Pickup and drop-off date & time windows
                - Equipment type
                - Load type (full or partial)
                - Commodity
                - Weight
                - Total rate
                - Load size

                ================================================
                LISTENING RULES (STRICT)
                ================================================

                - Do NOT interrupt while the broker is speaking
                - Do NOT ask follow-up questions mid-sentence
                - Do NOT rush or cut off the broker
                - Be a patient, attentive listener

                Silence while the broker is talking is REQUIRED.

                ================================================
                SPEECH COMPLETION DEFINITION (CRITICAL)
                ================================================

                The broker is considered to have finished speaking ONLY IF:

                - The broker has stopped speaking
                AND
                - There has been at least 6 seconds of silence

                Pauses, fillers, or thinking sounds such as:
                “uh”, “um”, “what”, “let me see”, short silence,
                DO NOT mean the broker is finished.

                ================================================
                AGENT BEHAVIOR RULES
                ================================================

                - While the broker is speaking OR pausing briefly, remain completely silent
                - Do NOT speak until the broker has been silent for 6 seconds
                - Silence during broker speech is REQUIRED
                - Never compete for turn-taking

                ================================================
                CLARIFICATION COOLDOWN (HARD RULE)
                ================================================

                Even if required details appear missing:

                - DO NOT ask any clarification immediately after the broker finishes speaking
                - Wait silently for an additional 2 seconds
                - If the broker resumes speaking during this time:
                  → CANCEL clarification
                  → Continue listening


                ================================================
                MISSING / UNCLEAR INFORMATION — CLARIFICATION
                ================================================

                After the broker finishes speaking, internally evaluate the information received.

                REQUIRED DETAILS CHECKLIST (ALL MUST BE PRESENT):
                - Origin AND destination (complete city + state)
                - Pickup AND drop-off date & time windows
                - Equipment type
                - Load type (full or partial)
                - Commodity
                - Weight
                - Total rate
                - Load size

                ================================================
                MANDATORY LOOPING RULE (CRITICAL)
                ================================================

                You MUST NOT proceed to call closing unless EVERY item in the checklist is clearly known.

                This is a HARD RULE.

                If ANY single required detail is missing, unclear, or assumed:
                - You MUST ask a clarification question
                - You MUST continue clarifying
                - You MUST NOT close the call

                ================================================
                CLARIFICATION BEHAVIOR
                ================================================

                If ONE OR MORE details are missing or unclear:

                - Ask ONLY ONE concise clarification question
                - The question MUST target the MOST IMPORTANT missing or unclear item
                - NEVER ask multiple questions in the same turn

                After asking:
                - STOP speaking
                - Wait for the broker’s response
                - Do NOT interrupt

                After the broker responds:
                - Re-evaluate the checklist
                - If ANY required item is still missing:
                  → Ask the NEXT most important clarification question
                - Repeat this loop UNTIL the checklist is fully complete

                ================================================
                CLARIFICATION PRIORITY ORDER (INTERNAL ONLY)
                ================================================

                1) Origin or destination missing or unclear
                   → Confirm location using internal lane first (not generic questioning)

                2) Pickup or drop-off date/time missing or unclear
                   → Ask about schedule

                3) Equipment type or load type missing or unclear
                   → Ask about equipment

                4) Load size or weight missing or unclear
                   → Ask about size/weight (combined)

                5) Commodity missing or unclear
                   → Ask about commodity

                6) Total rate missing or unclear
                   → Ask about rate
                ================================================
                EXAMPLES (ONE QUESTION ONLY)
                ================================================

                • Location confirmation (preferred if known internally):
                  “Just to confirm, pickup is {orgin} and delivery is {destination}, right?”

                • Schedule missing:
                  “Could you confirm the pickup and drop-off schedule?”

                • Equipment/load type missing:
                  “I’m showing {equipmentText} on this — is that correct? Full or partial?”

                • Size/weight missing:
                  “Showing roughly {weightText} and {lengthText}, correct?”

                • Commodity missing:
                  “What commodity is being hauled?”

                • Rate missing:
                  “What total rate are you offering for this load?”


                ===============================================
                BROKER CONFUSION HANDLING (CRITICAL)
                ===============================================

                If the broker responds with confusion signals such as:
                - “About what?”
                - “What for?”
                - “How far?”
                - “I didn’t give you anything”
                - “What are you asking?”

                Then IMMEDIATELY pause checklist flow and say:

                “Sorry about that — I’m calling regarding a load from {orgin} to {destination}. I just need to confirm a few details.”

                Then STOP and wait.

                If confusion repeats a second time:
                - Politely disengage and Invoke HangUp

                ===============================================
                INTERRUPT GUARD (CRITICAL)
                ===============================================

                Never ask a checklist question immediately after:
                - A short broker response
                - A confused response
                - A clarification response


                Always wait for:
                - A natural conversational pause
                - OR a continuation cue from the broker such as:
                  “yes”, “yeah”, “yep”, “yup”, “okay”, “ok”, “alright”,
                  “go ahead”, “ask”, “shoot”, “mm-hmm”, “right”, “correct”

                These should be treated as permission to continue the conversation.

                ================================================
                ABSOLUTE CONSTRAINTS
                ================================================

                🚫 Do NOT interrupt under any circumstance  
                🚫 Do NOT assume missing details  
                🚫 Do NOT proceed based on partial information  
                🚫 Do NOT summarize and close unless checklist = COMPLETE

                ================================================
                CALL CLOSING (DATA COLLECTION MODE)
                ================================================

                Before closing, internally verify:

                All required checklist items are collected

                No item is missing or unclear

                If the checklist is 100% complete:

                Say exactly one sentence:

                “Thank you for sharing all the details. My manager will review this and connect with you shortly.”

                Then:

                Do NOT wait for a goodbye

                Do NOT respond to any broker reply (e.g. “okay”, “sounds good”, “bye”, “fine”)

                Immediately invoke HangUp

                ================================================
                CALL ENDING
                ================================================

                Always end politely:
                “Thank you for your time. Have a great day.”
                Immediately invoke HangUp

                ================================================
                VOICEMAIL RULES
                ================================================
                If:

                Call is not received,
                Voicemail is detected
                AI-calling system is detected

                Then:

                Leave voicemail (if allowed)

                Do NOT talk to another AI

                Do NOT continue conversation

                End call immediately after voicemail

                ================================================
                FORCE CALL TERMINATION
                ================================================

                If broker wants to end the call

                If broker is not interested

                If broker becomes unresponsive

                If broker say bye

                Say:

                “Understood, thank you. Goodbye.”

                Immediately invoke HangUp

                ===============================================
                NON-COOPERATIVE EXIT RULE
                ===============================================

                If the broker gives 2 or more unclear, evasive, or non-responsive answers
                after clarification attempts:

                Say:

                “I understand — this may not be a good time. I’ll let you go. Thank you for your time.”

                Then Immediately invoke HangUp.

                Do NOT continue questioning.

                ================================================
                CONTEXT (DO NOT REVEAL)
                ================================================
                consent_mode = {consentOn}
                lane = '{orgin}' → '{destination}'
                origin = '{orgin}'
                destination = '{destination}'
                equipment_type = '{equipmentText}'
                weight = '{weightText}'
                length = '{lengthText}'
                commodity = '{commodityText}'
                rpm = '{offeredRpm}'
                Reference_id = '{load.Reference_ID}'
                mc_number = '{company.mc_number}'
                company_name = '{company.name}'
                agent_id = '{agentId}'
                user_id = '{userid}'
                call_id = '{call_id}'


";
            // 4) Compact metadata (for logging/analytics)
            var compactMetadata = new
            {
                company = new { name = company.name, mc = company.mc_number },
                call_id = call_id,
                user_id = userid,
                load = new
                {
                    orgin,
                    destination,
                    FromDate = load.FromDate.ToString("O"),
                    ToDate = load.ToDate.ToString("O"),
                    load.Reference_ID
                },
                client = new
                {
                    client.ClientPhone,
                    client.ClientEmail,
                    client.ClientCompany
                },
                settings = new
                {
                    consentMode = consentOn,
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
                selectedTools = new[]
                {
                    new { toolName = "leaveVoicemail" },
                    new { toolName = "hangUp" }
                },
                vadSettings = new
                {
                    turnEndpointDelay = "0.7s",
                    minimumTurnDuration = "0s",
                    minimumInterruptionDuration = "0.1s",
                    frameActivationThreshold = 0.12  // Best for low-volume telephony speech
                },

            inactivityMessages = new object[]
                    {
                        // First nudge
                        new
                        {
                            duration = "5s",
                            message = "Sorry, that cut out a bit — can you repeat that?"
                        }
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
            var baseUrl = _configuration["App:PublicBaseUrl"];

            if (string.IsNullOrEmpty(baseUrl))
                throw new InvalidOperationException("App:PublicBaseUrl is not configured.");

            var statusCallbackUrl = new Uri($"{baseUrl}/api/webhooks/twilio/status");

            var twilioCall = await Twilio.Rest.Api.V2010.Account.CallResource.CreateAsync(
               to: toNumber,
               from: fromNumber,
               twiml: new Twiml(vr.ToString()),
               statusCallback: statusCallbackUrl,
               statusCallbackEvent: new List<string>
               {
                "queued",
                "ringing",
                "answered",
                "in-progress",
                "completed",
                "busy",
                "failed",
                "no-answer",
                "canceled"
               }
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
        
        public async Task<ServiceResponse<List<CallResponse>>> GetCallsAsync(int companyId, string? statusFilter)
        {
            try
            {
                // Validate status if provided
                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    var normalized = statusFilter.Trim().ToLowerInvariant();

                    // Load all statuses from DB
                    var allowedStatuses = await _callRepository.GetDistinctCallStatusesAsync();

                    if (!allowedStatuses.Contains(normalized))
                    {
                        return ServiceResponse<List<CallResponse>>.ErrorResponse("Invalid status filter. Allowed values: " + string.Join(", ", allowedStatuses), 400);
                    }

                    statusFilter = normalized;
                }
                var response = await _callRepository.GetCallsAsync(companyId, statusFilter);

                return ServiceResponse<List<CallResponse>>.SuccessResponse(
                    response,
                    "Calls fetched successfully"
                );

            }
            catch (Exception ex)
            {
                return ServiceResponse<List<CallResponse>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }




        public async System.Threading.Tasks.Task TakeoverCallAsync(Guid callId, int companyId, int userId, string takeovernumber)
        {
            var takeoverValidation = await ValidateTakeoverFeatureAsync(companyId);

            if (!takeoverValidation.Success)
                throw new InvalidOperationException(takeoverValidation.ErrorMessage);

            // ============================
            // Minute Limit Check
            // ============================
            var limitCheck = await CheckMinuteLimitAsync(companyId);

            if (!limitCheck.Success)
                throw new InvalidOperationException(limitCheck.ErrorMessage);

            // 1️⃣ Fetch call info from DB
            var call = await _callRepository.GetCallForTakeoverAsync(callId);

            if (call == null)
                throw new InvalidOperationException("Call not found.");

            // 2️⃣ Company ownership check
            if (call.CompanyId != companyId)
                throw new UnauthorizedAccessException("Call does not belong to your company.");

            if (string.IsNullOrWhiteSpace(call.TwilioCallSid))
                throw new InvalidOperationException("Twilio Call SID is missing.");


            if (string.IsNullOrWhiteSpace(takeovernumber))
                throw new InvalidOperationException("No number found for this User");

            //only this is remaining 
            //var takeoverNumber = "+923212694374";

            var twilioCall = await Twilio.Rest.Api.V2010.Account
            .CallResource.FetchAsync(call.TwilioCallSid);

            if (twilioCall.Status != Twilio.Rest.Api.V2010.Account
                    .CallResource.StatusEnum.InProgress)
            {
                throw new InvalidOperationException(
                    $"Call is not live. Current status: {twilioCall.Status}");
            }

            // 5️⃣ Build takeover TwiML URL
            var baseUrl = _configuration["App:PublicBaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Public base URL not configured.");

            var takeoverUrl =
                $"{baseUrl}/api/aiagent/takeover-twiml?number={Uri.EscapeDataString(takeovernumber)}";

            // 6️⃣ Redirect call to takeover TwiML
            await Twilio.Rest.Api.V2010.Account.CallResource.UpdateAsync(
                pathSid: call.TwilioCallSid,
                url: new Uri(takeoverUrl),
                method: Twilio.Http.HttpMethod.Get
            );
        }

        static readonly Dictionary<string, string> StateMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AL"] = "Alabama",
            ["AK"] = "Alaska",
            ["AZ"] = "Arizona",
            ["AR"] = "Arkansas",
            ["CA"] = "California",
            ["CO"] = "Colorado",
            ["CT"] = "Connecticut",
            ["DE"] = "Delaware",
            ["FL"] = "Florida",
            ["GA"] = "Georgia",
            ["HI"] = "Hawaii",
            ["ID"] = "Idaho",
            ["IL"] = "Illinois",
            ["IN"] = "Indiana",
            ["IA"] = "Iowa",
            ["KS"] = "Kansas",
            ["KY"] = "Kentucky",
            ["LA"] = "Louisiana",
            ["ME"] = "Maine",
            ["MD"] = "Maryland",
            ["MA"] = "Massachusetts",
            ["MI"] = "Michigan",
            ["MN"] = "Minnesota",
            ["MS"] = "Mississippi",
            ["MO"] = "Missouri",
            ["MT"] = "Montana",
            ["NE"] = "Nebraska",
            ["NV"] = "Nevada",
            ["NH"] = "New Hampshire",
            ["NJ"] = "New Jersey",
            ["NM"] = "New Mexico",
            ["NY"] = "New York",
            ["NC"] = "North Carolina",
            ["ND"] = "North Dakota",
            ["OH"] = "Ohio",
            ["OK"] = "Oklahoma",
            ["OR"] = "Oregon",
            ["PA"] = "Pennsylvania",
            ["RI"] = "Rhode Island",
            ["SC"] = "South Carolina",
            ["SD"] = "South Dakota",
            ["TN"] = "Tennessee",
            ["TX"] = "Texas",
            ["UT"] = "Utah",
            ["VT"] = "Vermont",
            ["VA"] = "Virginia",
            ["WA"] = "Washington",
            ["WV"] = "West Virginia",
            ["WI"] = "Wisconsin",
            ["WY"] = "Wyoming"
        };

        string NormalizeLocation(string location)
        {
            // Phoenix,AZ  OR  Phoenix, AZ
            var parts = location.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return location;

            var city = parts[0].Trim();
            var state = parts[1].Trim();

            if (StateMap.TryGetValue(state, out var fullState))
                return $"{city}, {fullState}";

            return location;
        }


        private async Task<ServiceResponse<bool>> CheckMinuteLimitAsync(int companyId)
        {
            try
            {
                var profile = await _companyPaymentProfileRepository.GetByCompanyIdAsync(companyId);

                if (profile == null)
                {
                    return ServiceResponse<bool>.ErrorResponse("Payment profile not found.", 404);
                }

                int limit = 0;

                // Trial logic (from config)
                if (profile.subscription_status == SubscriptionStatus.trial)
                {
                    var trialLimit = _configuration["Trial:AllowedMinutes"];

                    if (!int.TryParse(trialLimit, out limit))
                    {
                        _logger.LogError("Invalid Trial:AllowedMinutes config value.");
                        return ServiceResponse<bool>.ErrorResponse("Configuration error.", 500);
                    }
                }
                // Active plan logic
                else if (profile.subscription_status == SubscriptionStatus.active)
                {
                    if (!profile.subscription_plan.HasValue)
                    {
                        return ServiceResponse<bool>.ErrorResponse("Subscription plan not found.", 400);
                    }

                    var plan = await _packagePlanRepository.GetByIdAsync(profile.subscription_plan.Value);

                    if (plan == null)
                    {
                        return ServiceResponse<bool>.ErrorResponse("Plan not found.", 404);
                    }

                    limit = plan.est_minute;
                }
                else
                {
                    // Handle cancelled, expired etc.
                    return ServiceResponse<bool>.ErrorResponse("Invalid subscription state.", 403);
                }

                // Final comparison
                if (profile.minutes_used >= limit)
                {
                    return ServiceResponse<bool>.ErrorResponse(
                        "You have already consumed your minute limits.", 403);
                }

                return ServiceResponse<bool>.SuccessResponse(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking minute limit for CompanyId: {CompanyId}", companyId);
                return ServiceResponse<bool>.ErrorResponse("Server is temporarily unavailable.", 503);
            }
        }

        private async Task<ServiceResponse<bool>> ValidateTakeoverFeatureAsync(int companyId)
        {
            try
            {
                var data = await _billingRepository.GetCompanyPlanFeaturesAsync(companyId);

                if (data == null)
                {
                    return ServiceResponse<bool>.ErrorResponse(
                        "Subscription plan not found.",
                        404);
                }

                // Allow custom plans automatically
                if (data.PlanType == PlanType.custom)
                {
                    return ServiceResponse<bool>.SuccessResponse(true);
                }

                // Allow plans with takeover enabled
                if (data.IsTakeoverAllowed)
                {
                    return ServiceResponse<bool>.SuccessResponse(true);
                }

                return ServiceResponse<bool>.ErrorResponse(
                    "Your package does not support this feature. Upgrade your plan to access it.",
                    403);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error validating takeover feature for CompanyId: {CompanyId}",
                    companyId);

                return ServiceResponse<bool>.ErrorResponse(
                    "Server is temporarily unavailable. Please try again later.",
                    503);
            }
        }
    
        private async Task<ServiceResponse<bool>> ValidateConcurrencyLimitAsync(int companyId)
        {
            try
            {
                var data = await _billingRepository.GetConcurrencyDataAsync(companyId);

                if (data == null)
                {
                    return ServiceResponse<bool>.ErrorResponse(
                        "Subscription plan not found.",
                        404);
                }

                if (data.ActiveCalls >= data.AllowedConcurrency)
                {
                    return ServiceResponse<bool>.ErrorResponse(
                        "You have reached the maximum concurrent call limit for your plan.",
                        403);
                }

                return ServiceResponse<bool>.SuccessResponse(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error validating concurrency limit for CompanyId: {CompanyId}",
                    companyId);

                return ServiceResponse<bool>.ErrorResponse(
                    "Server is temporarily unavailable. Please try again later.",
                    503);
            }
        }
    }
}

