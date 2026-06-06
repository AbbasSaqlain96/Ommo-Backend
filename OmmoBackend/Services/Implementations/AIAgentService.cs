using Microsoft.EntityFrameworkCore;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class AIAgentService : IAIAgentService
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IAIAgentRepository _aiAgentRepository;
        private readonly ILogger<AIAgentService> _logger;

        public AIAgentService(ICompanyRepository companyRepository, IAIAgentRepository aiAgentRepository, ILogger<AIAgentService> logger)
        {
            _companyRepository = companyRepository;
            _aiAgentRepository = aiAgentRepository;
            _logger = logger;
        }

        //public async Task<ServiceResponse<RegisterAIAgentResult>> RegisterAIAgentAsync(RegisterAIAgentRequest request)
        //{
        //    if (request.AgentType != "LoadBoard")
        //    {
        //        return ServiceResponse<RegisterAIAgentResult>.ErrorResponse("AgentType not supported", 400);
        //    }

        //    var company = await _companyRepository.GetByIdAsync(request.CompanyId);
        //    if (company == null)
        //    {
        //        return ServiceResponse<RegisterAIAgentResult>.ErrorResponse("Company not found", 404);
        //    }

        //    // Step 1: Create AI Agent
        //    var aiAgentConfig = await _ultravoxAIService.CreateLoadBoardAgentAsync(company.name);
        //    if (aiAgentConfig == null)
        //        return ServiceResponse<RegisterAIAgentResult>.ErrorResponse("Failed to create AI agent", 500);

        //    // Step 2: Buy Twilio Number
        //    var twilioNumber = await _twilioService.BuyNumberAsync();
        //    if (string.IsNullOrEmpty(twilioNumber))
        //        return ServiceResponse<RegisterAIAgentResult>.ErrorResponse("Could not provision Twilio number", 500);

        //    // Step 3: Insert Agent
        //    var agent = new Agent
        //    {
        //        company_id = company.company_id,
        //        agent_type = request.AgentType
        //    };

        //    var savedAgent = await _aiAgentRepository.RegisterAIAgentAsync(agent);

        //    // Step 4: Update Company with Twilio Number
        //    company.twilio_number = twilioNumber;
        //    await _companyRepository.UpdateAsync(company);

        //    return ServiceResponse<RegisterAIAgentResult>.SuccessResponse(new RegisterAIAgentResult()
        //    {
        //        Status = true,
        //        AgentId = savedAgent.agent_guid,
        //        TwilloNumber = twilioNumber
        //    });
        //}

        public async Task<ServiceResponse<AgentSettingsResponse>> GetAgentSettingsAsync(int companyId)
        {
            try
            {
                var (agent, settings) = await _aiAgentRepository.GetAgentWithSettingsByCompanyIdAsync(companyId);

                if (agent == null || settings == null)
                    return ServiceResponse<AgentSettingsResponse>.ErrorResponse("No AI agent configuration found for your company", 404);

                var response = new AgentSettingsResponse
                {
                    AgentGuid = agent.agent_guid,
                    AgentName = settings.AgentName,
                    WhoWeAre = settings.WhoWeAre,
                    VoiceGender = settings.VoiceGender,
                    FloorRpm = settings.FloorRpm,
                    TargetRpm = settings.TargetRpm,
                    WalkawayRpm = settings.WalkawayRpm,
                    ConsentMode = settings.ConsentMode
                };

                return ServiceResponse<AgentSettingsResponse>.SuccessResponse(response, "AI Agent settings fetched successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching AI Agent Settings for CompanyId: {CompanyId}", companyId);
                return ServiceResponse<AgentSettingsResponse>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<string>> UpdateAgentSettingsAsync(UpdateAgentSettingsRequest request, int companyId)
        {
            try
            {
                // Validate existence
                var agent = await _aiAgentRepository.GetAgentByGuidAsync(request.AgentGuid);

                if (agent == null)
                    return ServiceResponse<string>.ErrorResponse("No Agent found for your Company", 404);

                // Validate ownership
                if (agent.company_id != companyId)
                    return ServiceResponse<string>.ErrorResponse("Agent ID does not belong to your Company", 403);

                // Fetch existing settings
                var settings = await _aiAgentRepository.GetByAgentGuidAsync(request.AgentGuid);
                if (settings == null)
                    return ServiceResponse<string>.ErrorResponse("No Agent Settings found", 404);

                // Apply only non-null updates
                if (!string.IsNullOrWhiteSpace(request.AgentName)) settings.AgentName = request.AgentName;
                if (!string.IsNullOrWhiteSpace(request.WhoWeAre)) settings.WhoWeAre = request.WhoWeAre;
                if (!string.IsNullOrWhiteSpace(request.VoiceGender))
                {
                    var gender = request.VoiceGender.ToLowerInvariant();
                    if (gender != "male" && gender != "female")
                        return ServiceResponse<string>.ErrorResponse("Invalid value for VoiceGender. Allowed values: 'male', 'female'.", 400);

                    settings.VoiceGender = gender;
                }

                if (request.FloorRpm.HasValue) settings.FloorRpm = request.FloorRpm.Value;
                if (request.TargetRpm.HasValue) settings.TargetRpm = request.TargetRpm.Value;
                if (request.WalkawayRpm.HasValue) settings.WalkawayRpm = request.WalkawayRpm.Value;

                // Validate constraint before saving
                if (settings.TargetRpm < settings.WalkawayRpm || settings.WalkawayRpm < settings.FloorRpm)
                {
                    return ServiceResponse<string>.ErrorResponse(
                        "Invalid RPM values: TargetRpm must be >= WalkawayRpm, and WalkawayRpm must be >= FloorRpm.", 400);
                }

                if (request.ConsentMode.HasValue) settings.ConsentMode = request.ConsentMode.Value;

                settings.UpdatedAt = DateTime.UtcNow;

                // Save changes
                await _aiAgentRepository.UpdateAsync(settings);

                return ServiceResponse<string>.SuccessResponse(null, "AI Agent Settings updated successfully.");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while updating AI Agent Settings for Company ID: {CompanyId}", companyId);
                return ServiceResponse<string>.ErrorResponse($"Database update failed", 500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while updating AI Agent Settings.");
                return ServiceResponse<string>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<string>> AddAgentAsync(Guid agentGuid, int companyId)
        {
            try
            {
                _logger.LogInformation("Adding AI Agent for CompanyId {CompanyId}", companyId);

                Agent agent = new Agent
                {
                    agent_guid = agentGuid,
                    company_id = companyId,
                    agent_type = "LoadBoard"
                };

                await _aiAgentRepository.AddAgentAsync(agent);

                _logger.LogInformation("AI Agent added for CompanyId {CompanyId}", companyId);
                return ServiceResponse<string>.SuccessResponse(null, "AI Agent added successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to add AI Agent. CompanyId: {CompanyId}, AgentGuid: {AgentGuid}",
                    companyId,
                    agentGuid);

                throw;
            }
        }
    }
}
