using OmmoBackend.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace OmmoBackend.Services.Implementations
{
    public class UltravoxService : IUltravoxService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UltravoxService> _logger;

        public UltravoxService(HttpClient httpClient, IConfiguration configuration, ILogger<UltravoxService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        //public async Task<AgentAIConfig> CreateLoadBoardAgentAsync(string companyName)
        //{
        //    var requestPayload = new
        //    {
        //        role = "Professional Load Booking Assistant",
        //        persona = "Friendly, assertive, quick negotiator",
        //        voice = "Natural, neutral tone",
        //        prompt = $"You are a load booking assistant for {companyName}. You help negotiate rates, match routes, and answer queries.",
        //        contextAware = true
        //    };

        //    try
        //    {
        //        var response = await _httpClient.PostAsJsonAsync("https://ultravox.ai/api/agents", requestPayload);
        //        if (!response.IsSuccessStatusCode)
        //        {
        //            _logger.LogError("Ultravox AI creation failed with status {Status}", response.StatusCode);
        //            return null;
        //        }

        //        var json = await response.Content.ReadFromJsonAsync<AgentAIConfig>();
        //        return json;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error creating AI agent with Ultravox.");
        //        return null;
        //    }
        //}


        public async Task<Guid> CreateAgentAsync(int companyId)
        {
            var apiKey = _configuration["Ultravox:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("Ultravox API key is missing");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ultravox.ai/api/agents");

            request.Headers.Add("X-API-Key", apiKey);

            var payload = new
            {
                name = $"Agent-{companyId}",

                callTemplate = new
                {
                    systemPrompt = @"You are an AI dispatcher for a trucking company.
                                    You handle load bookings, driver communication, and logistics coordination.
                                    Be concise, professional, and accurate.",

                    model = "ultravox-v0.7",

                    temperature = 0.3,

                    firstSpeakerSettings = new
                    {
                        agent = new
                        {
                            text = "Hello, this is your dispatch assistant. How can I help you today?"
                        }
                    },

                    medium = new
                    {
                        webRtc = new
                        {
                            dataMessages = new
                            {
                                transcript = true
                            }
                        }
                    }
                }
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                _logger.LogInformation("Calling Ultravox API to create agent for CompanyId {CompanyId}", companyId);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

                var response = await _httpClient.SendAsync(request, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    _logger.LogError("Ultravox API failed. Status: {Status}, Response: {Response}",
                        response.StatusCode,
                        errorContent);

                    throw new HttpRequestException($"Ultravox API failed with status {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseContent);

                string? agentGuidString = doc.RootElement.TryGetProperty("agentId", out var guidProp)
                    ? guidProp.GetString()
                    : doc.RootElement.GetProperty("id").GetString();

                if (!Guid.TryParse(agentGuidString, out var agentGuid))
                {
                    _logger.LogError("Invalid GUID returned from Ultravox: {Response}", responseContent);
                    throw new Exception("Invalid agent GUID");
                }

                _logger.LogInformation("Ultravox agent created: {AgentGuid}", agentGuid);

                return agentGuid;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Ultravox API timeout for CompanyId {CompanyId}", companyId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating Ultravox agent for CompanyId {CompanyId}", companyId);
                throw;
            }
        }
    }
}
