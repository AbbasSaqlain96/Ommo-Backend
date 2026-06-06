namespace OmmoBackend.Dtos
{
    public class PlanDto
    {
        public int PlanId { get; set; }
        public string PlanType { get; set; } = default!;
        public string PlanName { get; set; } = default!;
        public int Concurrency { get; set; }
        public string Interval { get; set; } = default!;
        public decimal Price { get; set; }
        public int AllowedUsers { get; set; }
        public int EstMinute { get; set; }
        public string PlanStatus { get; set; } = default!;
        public bool IsTranscriptAllowed { get; set; }
        public bool IsTakeoverAllowed { get; set; }
    }
}
