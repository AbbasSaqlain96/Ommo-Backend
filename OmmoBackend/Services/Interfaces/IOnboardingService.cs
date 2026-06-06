using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IOnboardingService
    {
        Task<ServiceResponse<SignupCompanyResponse>> SignupCompanyAsync(SignupCompanyRequest request);
        Task<OnboardingAuthDto> GetOnboardingDataAsync(int companyId);
        Task<ServiceResponse<string>> CompleteQuestionnaireAsync(int companyId, List<QuestionnaireAnswerRequest> request);
        Task<ServiceResponse<string>> AdvanceToPaymentStepAsync(int companyId);
    }
}
