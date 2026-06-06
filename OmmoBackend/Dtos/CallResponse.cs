namespace OmmoBackend.Dtos
{
    public class CallResponse
    {
        public Guid CallId { get; set; }
        public string? BrokerNumber { get; set; }
        public string StatusOfCall { get; set; } = "";
        public DateTime CallTimestamp { get; set; }
        public string? BrokerCompany { get; set; }
        public int CallDuration { get; set; }

        public bool IsTranscriptComplete { get; set; }
        public bool IsAIProcessingComplete { get; set; }

        public string? BrokerName { get; set; }
        public string? Sentiment { get; set; }

        public List<string>? SummaryBullets { get; set; }
    }

}
