namespace OmmoBackend.Dtos
{
    public class CallInsightsResult
    {
        public Guid CallId { get; set; }
        public ConfirmDataDto ConfirmData { get; set; }   // << Wrap your ConfirmDataDto here

        public string Sentiment { get; set; } = "neutral";

        public List<string> SummaryBullets { get; set; } = new();
    }
}
