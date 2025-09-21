using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IOnboardingService
    {
        Task<ServiceResponse<SignupCompanyResponse>> SignupCompanyAsync(SignupCompanyRequest request);
    }
}
