using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Dtos
{
    public class CompanyPlanFeaturesDto
    {
        public SubscriptionStatus SubscriptionStatus { get; set; }

        public int MinutesUsed { get; set; }

        public int EstMinute { get; set; }

        public bool IsTakeoverAllowed { get; set; }

        public PlanType PlanType { get; set; }
    }
}
