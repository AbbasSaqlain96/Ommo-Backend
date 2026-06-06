using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using OmmoBackend.Dtos;
using OmmoBackend.Services.Interfaces;
using OmmoBackend.Models;
using OmmoBackend.Data;

namespace OmmoBackend.Services.Implementations
{
    public class AiInsightsService : IAiInsightsService
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _cfg;
        private readonly ILogger<AiInsightsService> _log;
        private readonly IServiceScopeFactory _scopeFactory;

        public AiInsightsService(
            IHttpClientFactory http,
            IConfiguration cfg,
            ILogger<AiInsightsService> log)
        {
            _http = http;
            _cfg = cfg;
            _log = log;
        }

        public async Task<CallInsightsResult> ExtractInsightsAsync(Guid callId, string transcript, CancellationToken ct)
        {
            var client = _http.CreateClient();

            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_cfg["AI:ApiKey"]}");
            var today = DateTime.UtcNow;
            var prompt = BuildPrompt(callId,transcript,today);

            var body = new
            {
                model = _cfg["AI:Model"],
                messages = new[]
                {
            new { role = "system", content = "You are a strict JSON extraction engine for logistics call insights." },
            new { role = "user", content = prompt }
        },
                response_format = new { type = "json_object" },
                max_tokens = 1000,
                temperature = 0.0 // 🔥 required for stable structured data
            };

            var json = JsonSerializer.Serialize(body);

            var response = await client.PostAsync(
                _cfg["AI:Endpoint"],
                new StringContent(json, Encoding.UTF8, "application/json"),
                ct
            );

            var responseText = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _log.LogError("AI failed: {Status} {Body}", response.StatusCode, responseText);
                return new CallInsightsResult();
            }

            using var doc = JsonDocument.Parse(responseText);

            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                _log.LogWarning("AI returned empty insights content.");
                return new CallInsightsResult();
            }

            return ParseAIResponse(content, callId);
        }

        private string BuildPrompt(Guid callId, string transcript, DateTime today)
        {
            return $@"You are an AI insights processor for a freight broker phone call.

            ====================================================
            CORE CONTEXT (STRICT)
            ====================================================

            - call_id = ""{callId}""
            - Each call is independent.
            - This extractor runs repeatedly as the transcript grows.
            - NEVER mix information from different calls.
            - The database layer merges values across iterations.

            ====================================================
            NULL & INCREMENTAL UPDATE RULE (CRITICAL)
            ====================================================

            For EACH field:
            A value is considered PRESENT if ANY of the following is true:
            - The broker states it directly
            - The agent states it AND the broker confirms with agreement words
            - The value can be confidently normalized from ASR noise
            - The value can be deterministically derived using allowed rules

            In these cases, you MUST return the value (not null).

            - If the value has NOT appeared yet in the transcript, return null.
            - Returning null is correct and expected.
            - Do NOT guess.
            - Do NOT invent placeholder values.
            - Do NOT downgrade previously known values.

            ====================================================
            EXTRACTION IMPERATIVE (CRITICAL)
            ====================================================

            If a value appears EVEN ONCE in the transcript
            (agent or broker),
            and the intent is clear,
            you MUST extract and return it.

            ====================================================
            BROKER DIRECT SPEECH RULE
            ====================================================

            Any factual information spoken directly by the broker
            (route, rate, equipment, commodity, dates, weight)
            is valid without needing confirmation.

            ====================================================
            CONFIRMATION RULES
            ====================================================

            Agent-stated facts are considered CONFIRMED if the broker responds with:
            ""yes"", ""yeah"", ""yep"", ""right"", ""correct"", ""ok"", ""okay"", ""sure"".

            ====================================================
            NORMALIZATION (ASR ERROR HANDLING)
            ====================================================

            Correct obvious speech-to-text errors using freight domain knowledge.

            Equipment Type normalization examples:
            - ""van"", ""rain"", ""dry van"", ""dry ven"" → ""Dry Van""
            - ""reefer"", ""refer"", ""reafar"" → ""Reefer""
            - ""flat"", ""flat bad"", ""flatbed"" → ""Flatbed""
            - ""step deck"", ""stepdeck"" → ""Step Deck""
            - ""power only"" → ""Power Only""

            Load Type normalization:
            - ""full"", ""full load"" → ""Full""
            - ""partial"", ""ltl"" → ""Partial""

            Commodity normalization examples:
            - ""glass"", ""class"", ""gras"" → ""Glass""
            - ""steel"", ""steal"" → ""Steel""
            - ""food"", ""foods"", ""produce"" → ""Food""
            - ""paper"", ""papers"" → ""Paper""
            - ""electronics"", ""electronic"" → ""Electronics""

            Weight normalization:
            - ""45k"", ""45 thousand"" → 45000

            If intent is unclear → return null.

            ====================================================
            DERIVED VALUES (MANDATORY)
            ====================================================

            TripMiles:
            - MUST be derived when BOTH Origin and Destination are known
            - Use reasonable U.S. highway distance estimates
            - If Origin or Destination is missing → TripMiles MUST be null

            RatePerMile:
            - MUST be calculated when BOTH FinalRate AND TripMiles are known
            - RPM = FinalRate / TripMiles
            - Round to 2 decimals
            - If FinalRate is missing → RatePerMile MUST be null


            ====================================================
            FIELDS YOU MUST RETURN
            ====================================================

            confirmData:
            - broker_name
            - PickupTime
            - DeliveryTime
            - TripMiles
            - RatePerMile
            - FinalRate
            - Origin
            - Destination
            - equipment_type
            - load_type
            - commodity
            - weight
            - load_size

            ====================================================
            BROKER NAME RULES
            ====================================================

            - Extract ONLY the broker's name.
            - Valid triggers:
              ""My name is ___""
              ""This is ___""
              ""You're speaking with ___""
            - Ignore agent names.
            - If unclear or disputed → return null.

            ====================================================
            SENTIMENT
            ====================================================

            - Sentiment reflects ONLY the broker's tone.
            - Values: positive | neutral | negative
            - Keep sentiment stable unless tone clearly changes.

            ====================================================
            SUMMARY BULLETS — STATE SNAPSHOT (FINAL)
            ====================================================

            Summary bullets provide a concise, human-readable snapshot of the CURRENT KNOWN STATE of the call.

            They are NOT a repetition of confirmData fields.
            They should read like short status notes a human would write.

            RULES:
            - Bullets may be generated as soon as information becomes available
            - Bullets SHOULD be textual and descriptive, not just field=value
            - Do NOT restate the same fact multiple times in different wording
            - Do NOT narrate conversation progress
            - Avoid robotic or repetitive phrasing

            Bullets should evolve only when new meaningful information is learned.
            If no meaningful information is known yet, returning an empty array is acceptable.

            ====================================================
            SUGGESTED BULLET THEMES (MAX ONE EACH)
            ====================================================

            1) Broker identity or engagement
               Example:
               ""Broker Sarwaich Raza discussed the load details.""

            2) Route overview
               Example:
               ""The load runs from Boise, Idaho to Creve Coeur, Illinois.""

            3) Schedule understanding
               Example:
               ""Pickup is planned in December with delivery expected by mid-February.""

            4) Equipment & load characteristics
               Example:
               ""The shipment requires a Dry Van and is a full load.""

            5) Commodity & weight
               Example:
               ""The load consists of glass weighing approximately 42,000 lbs.""

            6) Pricing discussion
               Example:
               ""A total rate of $1,500 was mentioned, with pricing still under review.""
            ====================================================
            SUMMARY BULLETS — CATEGORY LOCK (CRITICAL)
            ====================================================

            Each summary bullet belongs to ONE category.

            Once a bullet for a category has been generated,
            you MUST NOT generate another bullet for the same category,
            even if the wording would be different.

            If new information refines an existing category
            (e.g. route changes from Houston to Phoenix):
            - Replace the previous understanding internally
            - But DO NOT generate a new bullet

            Allowed categories (ONE BULLET MAX EACH):
            - Broker
            - Route
            - Schedule
            - Equipment & Load
            - Commodity & Weight
            - Pricing

            ====================================================
            DATE RESOLUTION RULE (CRITICAL)
            ====================================================

            Assume TODAY'S DATE is: {today} (UTC).

            When a broker mentions a date WITHOUT a year:
            - Assume the date refers to the NEXT FUTURE occurrence
            - Never assign a past date

            PickupTime and DeliveryTime must always be in the future
            relative to today's date unless explicitly stated otherwise.

            ====================================================
            OUTPUT FORMAT (STRICT JSON ONLY)
            ====================================================

            {{
              ""callId"": ""{callId}"",
              ""confirmData"": {{
                ""broker_name"": ""string or null"",
                ""PickupTime"": ""ISO-8601 string or null"",
                ""DeliveryTime"": ""ISO-8601 string or null"",
                ""TripMiles"": number or null,
                ""RatePerMile"": number or null,
                ""FinalRate"": number or null,
                ""Origin"": ""string or null"",
                ""Destination"": ""string or null"",
                ""equipment_type"": ""string or null"",
                ""load_type"": ""string or null"",
                ""commodity"": ""string or null"",
                ""weight"": number or null,
                ""load_size"": number or null
              }},
              ""sentiment"": ""positive | neutral | negative"",
              ""bullets"": []
            }}

            ====================================================
            TRANSCRIPT
            ====================================================

            {transcript}
            ";
        }






        private CallInsightsResult ParseAIResponse(string content, Guid callId)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                return new CallInsightsResult
                {
                    CallId = callId,  // <-- direct assignment (no risk)

                    ConfirmData = root.TryGetProperty("confirmData", out var cd)
                        ? JsonSerializer.Deserialize<ConfirmDataDto>(cd.GetRawText())
                        : null,

                    Sentiment = root.TryGetProperty("sentiment", out var s)
                        ? s.GetString()
                        : "neutral",

                    SummaryBullets = root.TryGetProperty("bullets", out var b)
                        ? b.EnumerateArray()
                            .Select(x => x.GetString() ?? "")
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToList()
                        : new List<string>()
                };
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to parse AI insights: {Content}", content);
                return new CallInsightsResult { CallId = callId };
            }
        }
        /*
        private static readonly string[] _goodbyeKeywords =
        {
            "bye",
            "goodbye",
            "thanks bye",
            "thank you bye",
            "see you",
            "bye bye",
            "<<end_call>>"
        };


        public bool ShouldEndCall_Regex(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var lower = text.ToLower();

            return _goodbyeKeywords.Any(k => lower.Contains(k));
        }

        public async Task EndCallAsync(Call call)
        {
            await Twilio.Rest.Api.V2010.Account.CallResource.UpdateAsync(
                pathSid: call.twilio_call_sid,
                status: Twilio.Rest.Api.V2010.Account.CallResource.UpdateStatusEnum.Completed
            );

        }

        public async Task<bool> DetectGoodbyeAsync(string message, CancellationToken ct)
        {
            var prompt = $@"
            Classify whether this message indicates the END of a phone call.

            Message: ""{message}""

            Rules:
            - If the message contains 'bye', 'goodbye', 'i'm done', 'not booking', 
              'disconnect', 'not happy', or any clear call-ending intent → answer YES.
            - If the agent says '<<END_CALL>>' then answer YES.
            - Otherwise → answer NO.

            Answer YES or NO only.";

            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_cfg["AI:ApiKey"]}");


            var body = new
            {
                model = _cfg["AI:Model"],
                messages = new[] {
            new { role = "user", content = prompt }
        }
            };

            var reqJson = JsonSerializer.Serialize(body);
            var resp = await client.PostAsync(
                _cfg["AI:Endpoint"],
                new StringContent(reqJson, Encoding.UTF8, "application/json"),
                ct
            );

            var json = await resp.Content.ReadAsStringAsync(ct);
            using var root = JsonDocument.Parse(json);

            var content = root.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
                ?.Trim()
                ?.ToLower();

            return content == "yes";
        }

        public async Task<bool> ShouldEndCallAsync(string transcriptText, CancellationToken ct)
        {
            // Fast path — 99% of cases
            if (ShouldEndCall_Regex(transcriptText))
                return true;

            // Only classify last message via AI
          //  var lastLine = transcriptText
            //    .Split('\n')
              //  .LastOrDefault()?
                //.Trim() ?? "";

            return await DetectGoodbyeAsync(transcriptText, ct);
        }

        */


    }
}
