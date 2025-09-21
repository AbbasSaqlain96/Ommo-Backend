using OmmoBackend.Dtos;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class UltravoxAIService : IUltravoxAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<UltravoxAIService> _logger;

        public UltravoxAIService(HttpClient httpClient, IConfiguration config, ILogger<UltravoxAIService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<AgentAIConfig> CreateLoadBoardAgentAsync(string companyName)
        {
            var requestPayload = new
            {
                role = "Professional Load Booking Assistant",
                persona = "Friendly, assertive, quick negotiator",
                voice = "Natural, neutral tone",
                prompt = $"You are a load booking assistant for {companyName}. You help negotiate rates, match routes, and answer queries.",
                contextAware = true
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("https://ultravox.ai/api/agents", requestPayload);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Ultravox AI creation failed with status {Status}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadFromJsonAsync<AgentAIConfig>();
                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating AI agent with Ultravox.");
                return null;
            }
        }
    }
}
