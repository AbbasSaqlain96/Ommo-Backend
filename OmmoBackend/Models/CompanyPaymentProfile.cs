using OmmoBackend.Helpers.Enums;
using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class CompanyPaymentProfile
    {
        [Key]
        public int company_id { get; set; }
        public string? stripe_customer_id { get; set; }
        public string? stripe_subscription_id { get; set; }
        public int? subscription_plan { get; set; }

        [Required]
        public SubscriptionStatus subscription_status { get; set; }
        public DateTimeOffset? trial_started_at { get; set; }
        public DateTimeOffset? trial_ends_at { get; set; }
        public DateTimeOffset? current_period_start { get; set; }
        public DateTimeOffset? current_period_end { get; set; }

        [Required]
        public bool cancel_at_period_end { get; set; }
        public DateTimeOffset? canceled_at { get; set; }

        [Required]
        public int minutes_used { get; set; }

        [Required]
        public bool payment_failed { get; set; }
        public DateTimeOffset? payment_failed_at { get; set; }

        [Required]
        public int payment_retry_count { get; set; }

        [Required]
        public DateTimeOffset updated_at { get; set; }
    }
}
