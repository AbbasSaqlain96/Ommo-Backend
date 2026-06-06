using OmmoBackend.Helpers.Enums;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class CompanyOnboarding
    {
        [Key]
        public int company_onboarding_id { get; set; }

        [Required]
        public int company_id { get; set; }

        [Required]
        public OnboardingStep current_step { get; set; } = OnboardingStep.verification;

        [Required]
        public bool is_completed { get; set; } = false;

        public DateTimeOffset? questionnaire_completed_at { get; set; }
        public DateTimeOffset? integration_completed_at { get; set; }
        public DateTimeOffset? payment_completed_at { get; set; }
        public DateTimeOffset? call_settings_completed_at { get; set; }

        [Required]
        public DateTimeOffset created_at { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTimeOffset updated_at { get; set; } = DateTime.UtcNow;

        public DateTime? verification_completed_at { get; set; }
    }
}
