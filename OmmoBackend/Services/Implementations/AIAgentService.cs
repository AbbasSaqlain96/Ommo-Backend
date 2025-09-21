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
        private readonly IUltravoxAIService _ultravoxAIService;
        private readonly ITwilioService _twilioService;

        public AIAgentService(ICompanyRepository companyRepository, IAIAgentRepository aiAgentRepository, IUltravoxAIService ultravoxAIService, ITwilioService twilioService)
        {
            _companyRepository = companyRepository;
            _aiAgentRepository = aiAgentRepository;
            _ultravoxAIService = ultravoxAIService;
            _twilioService = twilioService;
        }

        public async Task<ServiceResponse<RegisterAIAgentResult>> RegisterAIAgentAsync(RegisterAIAgentRequest request)
        {
            if (request.AgentType != "LoadBoard")
            {
                return ServiceResponse<RegisterAIAgentResult>.ErrorResponse("AgentType not supported", 400);
            }

            var company = await _companyRepository.GetByIdAsync(request.CompanyId);
            if (company == null)
            {
                return ServiceResponse<RegisterAIAgentResult>.ErrorResponse("Company not found", 404);
            }

            // Step 1: Create AI Agent
            var aiAgentConfig = await _ultravoxAIService.CreateLoadBoardAgentAsync(company.name);
            if (aiAgentConfig == null)
                return ServiceResponse<RegisterAIAgentResult>.ErrorResponse("Failed to create AI agent", 500);

            // Step 2: Buy Twilio Number
            var twilioNumber = await _twilioService.BuyNumberAsync();
            if (string.IsNullOrEmpty(twilioNumber))
                return ServiceResponse<RegisterAIAgentResult>.ErrorResponse("Could not provision Twilio number", 500);

            // Step 3: Insert Agent
            var agent = new Agent
            {
                company_id = company.company_id,
                agent_type = request.AgentType
            };

            var savedAgent = await _aiAgentRepository.RegisterAIAgentAsync(agent);

            // Step 4: Update Company with Twilio Number
            company.twilio_number = twilioNumber;
            await _companyRepository.UpdateAsync(company);

            return ServiceResponse<RegisterAIAgentResult>.SuccessResponse(new RegisterAIAgentResult()
            {
                Status = true,
                AgentId = savedAgent.agent_id,
                TwilloNumber = twilioNumber
            });
        }
    }
}
