namespace OmmoBackend.Dtos
{
    public class CompanyPaymentProfileDto
    {
        public int? SubscriptionPlan { get; set; }
        public string SubscriptionStatus { get; set; }
        public DateTimeOffset? CurrentPeriodStart { get; set; }
        public DateTimeOffset? CurrentPeriodEnd { get; set; }
        public bool CancelAtPeriodEnd { get; set; }
        public DateTimeOffset? CanceledAt { get; set; }
        public int MinutesUsed { get; set; }
        public int TotalPackageMinutes { get; set; }
        public int MinutesUnused { get; set; }
    }
}
