using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Dtos
{
    public class SignupCompanyResponse
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public OnboardingDto OnboardingDto { get; set; }
    }

    public class OnboardingDto 
    {
        public bool IsCompleted { get; set; }
        public OnboardingStep CurrentStep { get; set; }
    }
}
