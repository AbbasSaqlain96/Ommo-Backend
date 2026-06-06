namespace OmmoBackend.Dtos
{
    public record AuthResult
    {
        public string? Token { get; set; }
        public string RefreshToken { get; set; }
        public OnboardingAuthDto OnboardingAuthDto { get; set; }
    }

    public class OnboardingAuthDto
    {
        public bool? IsCompleted { get; set; }
        public string? CurrentStep { get; set; }
        public string? SubscriptionStatus { get; set; }
    }
}