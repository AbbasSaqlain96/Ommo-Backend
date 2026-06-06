namespace OmmoBackend.Dtos
{
    public class ConfirmDataDto
    {
        public DateTime? PickupTime { get; set; }
        public DateTime? DeliveryTime { get; set; }
        public decimal? TripMiles { get; set; }
        public decimal? RatePerMile { get; set; }
        public decimal? FinalRate { get; set; }
        public string? Origin { get; set; }
        public string? broker_name { get; set; }
        public string? Destination { get; set; }
        public string? equipment_type { get; set; }     // e.g. Dry Van, Reefer
        public string? load_type { get; set; }          // Full / Partial
        public string? commodity { get; set; }

        public decimal? weight { get; set; }

        public decimal? load_size { get; set; }
    }
    public class SentimentDto
    {
        public string Sentiment { get; set; }
    }

    public class SummaryBulletDto
    {
        public string Text { get; set; }
    }
}
