namespace OmmoBackend.Dtos
{
    public class BillingHistoryRecordDto
    {
        public string PackageName { get; set; }
        public decimal Amount { get; set; }
        public int MinutesUsed { get; set; }
        public int MinuteUnused { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public string Interval { get; set; }
    }
}
