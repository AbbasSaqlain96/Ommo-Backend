using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class AIAgentSettingService : IAIAgentSettingService
    {
        private readonly IAIAgentSettingRepository _aiAgentSettingRepository;
        private readonly ILogger<AIAgentSettingService> _logger;

        public AIAgentSettingService(ILogger<AIAgentSettingService> logger, IAIAgentSettingRepository aiAgentSettingRepository)
        {
            _logger = logger;
            _aiAgentSettingRepository = aiAgentSettingRepository;
        }

        public async Task<ServiceResponse<string>> AddAgentSettingAsync(Guid agentGuid)
        {
            try
            {
                _logger.LogInformation("Adding agent settings for AgentGuid {AgentGuid}", agentGuid);

                AgentSettings agentSettings = new AgentSettings();
                agentSettings.AgentGuid = agentGuid;
                agentSettings.AgentName = "Dana";
                agentSettings.WhoWeAre = "Dispatch for Truck company";
                agentSettings.VoiceGender = "male";
                agentSettings.FloorRpm = (decimal)2.000;
                agentSettings.TargetRpm = (decimal)3.000;
                agentSettings.WalkawayRpm = (decimal)2.100;
                agentSettings.ConsentMode = false;

                await _aiAgentSettingRepository.AddAgentSettingAsync(agentSettings);

                _logger.LogInformation("Agent settings added for AgentGuid {AgentGuid}", agentGuid);

                return ServiceResponse<string>.SuccessResponse("Agent setting added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add agent settings for AgentGuid {AgentGuid}", agentGuid);
                throw;
            }
        }
    }
}